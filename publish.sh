read -p "Enter package version: " version
read -p "Enter the version message: " message
read -p "Enter NuGet API Key: " apiKey

git tag -a $version -m "$message"
git push origin $version

dotnet pack
dotnet nuget push bin/Release/N35T.Distributed.$version.nupkg --api-key $apiKey --source https://api.nuget.org/v3/index.json