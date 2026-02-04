#!/bin/bash
set -euo pipefail

# Move to the directory where this script resides so relative paths work when double-clicked.
cd "$(dirname "$0")"

# Build and pack the Maude.Native NuGet package.
dotnet build Maude.Native/Maude.Native.csproj -c Release
dotnet pack Maude.Native/Maude.Native.csproj -c Release -o ./artifacts/nuget
