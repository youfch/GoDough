VERSION=$1

dotnet pack src/GoDough.sln --configuration Release
dotnet nuget push \
  src/.godot/mono/temp/bin/Release/GoDough.${VERSION}.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json