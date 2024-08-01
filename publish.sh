read -p "Enter package version: " version
read -p "Enter NuGet API Key: " apiKey

dotnet pack
dotnet nuget push bin/Release/N35T.Distributed.$version.nupkg --api-key $apiKey --source https://api.nuget.org/v3/index.json