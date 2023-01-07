REM Expects .NET Core SDK listed in global.json to be installed (other versions might work as well, but it was tested with this one)
dotnet pack -c Debug .\src\Compilers\Core\Portable\Microsoft.CodeAnalysis.csproj -p:OfficialBuild=true -p:StrongNameKeyId=Open --no-restore --include-symbols 

dotnet nuget push .\artifacts\packages\Debug\Shipping\Aquila.Microsoft.CodeAnalysis.4.2.2.symbols.nupkg -k 4267837e-9316-452e-9c3c-e26055f4cdba --source https://www.myget.org/F/aquila/api/v2/package