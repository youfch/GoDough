VERSION=$1

xmlstarlet edit \
  --inplace \
  --update "/Project/PropertyGroup/AssemblyVersion" \
  --value  "$VERSION" \
  src/GoDough.csproj