﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ShadowCopyAnalyzerAssemblyLoader : DefaultAnalyzerAssemblyLoader
    {
        /// <summary>
        /// The base directory for shadow copies. Each instance of
        /// <see cref="ShadowCopyAnalyzerAssemblyLoader"/> gets its own
        /// subdirectory under this directory. This is also the starting point
        /// for scavenge operations.
        /// </summary>
        private readonly string _baseDirectory;

        internal readonly Task DeleteLeftoverDirectoriesTask;

        /// <summary>
        /// The directory where this instance of <see cref="ShadowCopyAnalyzerAssemblyLoader"/>
        /// will shadow-copy assemblies, and the mutex created to mark that the owner of it is still active.
        /// </summary>
        private readonly Lazy<(string directory, Mutex)> _shadowCopyDirectoryAndMutex;

        /// <summary>
        /// Used to generate unique names for per-assembly directories. Should be updated with <see cref="Interlocked.Increment(ref int)"/>.
        /// </summary>
        private int _assemblyDirectoryId;

        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory = null)
        {
            if (baseDirectory != null)
            {
                _baseDirectory = baseDirectory;
            }
            else
            {
                _baseDirectory = Path.Combine(Path.GetTempPath(), "CodeAnalysis", "AnalyzerShadowCopies");
            }

            _shadowCopyDirectoryAndMutex = new Lazy<(string directory, Mutex)>(
                () => CreateUniqueDirectoryForProcess(), LazyThreadSafetyMode.ExecutionAndPublication);

            DeleteLeftoverDirectoriesTask = Task.Run(DeleteLeftoverDirectories);
        }

        private void DeleteLeftoverDirectories()
        {
            // Avoid first chance exception
            if (!Directory.Exists(_baseDirectory))
                return;

            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(_baseDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var subDirectory in subDirectories)
            {
                string name = Path.GetFileName(subDirectory).ToLowerInvariant();
                Mutex mutex = null;
                try
                {
                    // We only want to try deleting the directory if no-one else is currently
                    // using it. That is, if there is no corresponding mutex.
                    if (!Mutex.TryOpenExisting(name, out mutex))
                    {
                        ClearReadOnlyFlagOnFiles(subDirectory);
                        Directory.Delete(subDirectory, recursive: true);
                    }
                }
                catch
                {
                    // If something goes wrong we will leave it to the next run to clean up.
                    // Just swallow the exception and move on.
                }
                finally
                {
                    if (mutex != null)
                    {
                        mutex.Dispose();
                    }
                }
            }
        }

#nullable enable
        protected override string GetPathToLoad(string fullPath)
        {
            string assemblyDirectory = CreateUniqueDirectoryForAssembly();
            string shadowCopyPath = CopyFileAndResources(fullPath, assemblyDirectory);
            return shadowCopyPath;
        }
#nullable disable

        private static string CopyFileAndResources(string fullPath, string assemblyDirectory)
        {
            string fileNameWithExtension = Path.GetFileName(fullPath);
            string shadowCopyPath = Path.Combine(assemblyDirectory, fileNameWithExtension);

            CopyFile(fullPath, shadowCopyPath);

            string originalDirectory = Path.GetDirectoryName(fullPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
            string resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
            string resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

            foreach (var directory in Directory.EnumerateDirectories(originalDirectory))
            {
                string directoryName = Path.GetFileName(directory);

                string resourcesPath = Path.Combine(directory, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    string resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }

                resourcesPath = Path.Combine(directory, resourcesNameWithoutExtension, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    string resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithoutExtension, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }
            }

            return shadowCopyPath;
        }

        private static void CopyFile(string originalPath, string shadowCopyPath)
        {
            var directory = Path.GetDirectoryName(shadowCopyPath);
            Directory.CreateDirectory(directory);

            File.Copy(originalPath, shadowCopyPath);

            ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
        }

        private static void ClearReadOnlyFlagOnFiles(string directoryPath)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            foreach (var file in directory.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories))
            {
                ClearReadOnlyFlagOnFile(file);
            }
        }

        private static void ClearReadOnlyFlagOnFile(FileInfo fileInfo)
        {
            try
            {
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }
            catch
            {
                // There are many reasons this could fail. Ignore it and keep going.
            }
        }

        private string CreateUniqueDirectoryForAssembly()
        {
            int directoryId = Interlocked.Increment(ref _assemblyDirectoryId);

            string directory = Path.Combine(_shadowCopyDirectoryAndMutex.Value.directory, directoryId.ToString());

            Directory.CreateDirectory(directory);
            return directory;
        }

        private (string directory, Mutex mutex) CreateUniqueDirectoryForProcess()
        {
            string guid = Guid.NewGuid().ToString("N").ToLowerInvariant();
            string directory = Path.Combine(_baseDirectory, guid);

            var mutex = new Mutex(initiallyOwned: false, name: guid);

            Directory.CreateDirectory(directory);

            return (directory, mutex);
        }
    }
}
