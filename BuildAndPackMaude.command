#!/bin/bash
set -euo pipefail

# Move to the directory where this script resides so relative paths work when double-clicked.
cd "$(dirname "$0")"

# Build and pack the Maude NuGet package.
dotnet build Maude/Maude.csproj -c Release
dotnet pack Maude/Maude.csproj -c Release -o ./artifacts/nuget
