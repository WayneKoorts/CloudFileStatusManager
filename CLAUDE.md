# CLAUDE.md

This file provides guidance for Claude Code when working on this repository.

## Project Overview

CloudFileStatusManager is a .NET library and CLI tool for managing the hydration status of files on cloud storage providers (iCloud Drive, OneDrive). It provides functionality to:

- Check if a file is on cloud storage
- Get/set hydration status (downloaded vs cloud-only)
- Get/set pin status (keep downloaded)

## Project Structure

```
src/
├── CloudFileStatusManager/              # Core library (interfaces, enums)
├── CloudFileStatusManager.Windows/      # Windows implementation
├── CloudFileStatusManager.macOS/        # macOS implementation
├── CloudFileStatusManager.CLI.Common/   # Shared CLI code
├── CloudFileStatusManager.CLI.Windows/  # Windows CLI (cfsm.exe)
├── CloudFileStatusManager.CLI.macOS/    # macOS CLI (cfsm)
└── CloudFileStatusManager.macOS.Tests/  # macOS tests
```

## Build Commands

```bash
# Build entire solution
dotnet build src/CloudFileStatusManager.sln

# Run tests
dotnet test src/CloudFileStatusManager.sln
```

## Creating Releases

### macOS CLI Release

1. **Build for both architectures:**
   ```bash
   cd src/CloudFileStatusManager.CLI.macOS
   dotnet publish -c Release -r osx-arm64 --self-contained true -o ../../publish/osx-arm64-cli
   dotnet publish -c Release -r osx-x64 --self-contained true -o ../../publish/osx-x64-cli
   ```

2. **Create tarballs:**
   ```bash
   cd publish
   tar -czvf cfsm-macos-arm64-VERSION.tar.gz -C osx-arm64-cli .
   tar -czvf cfsm-macos-x64-VERSION.tar.gz -C osx-x64-cli .
   ```

3. **Create GitHub release:**
   ```bash
   gh release create vVERSION-macos \
     --title "CLI for macOS VERSION" \
     --notes "Release notes here" \
     publish/cfsm-macos-arm64-VERSION.tar.gz \
     publish/cfsm-macos-x64-VERSION.tar.gz
   ```

### Windows CLI Release

1. **Build for both architectures:**
   ```bash
   cd src/CloudFileStatusManager.CLI.Windows
   dotnet publish -c Release -r win-x64 --self-contained true -o ../../publish/win-x64-cli
   dotnet publish -c Release -r win-x86 --self-contained true -o ../../publish/win-x86-cli
   ```

2. **Create zip files:**
   ```bash
   cd publish
   zip -r CloudFileStatusManager-Windows-CLI-VERSION.zip win-x64-cli/
   ```

3. **Create GitHub release:**
   ```bash
   gh release create vVERSION \
     --title "CLI for Windows VERSION" \
     --notes "Release notes here" \
     publish/CloudFileStatusManager-Windows-CLI-VERSION.zip
   ```

### Managing Releases

```bash
# List all releases
gh release list

# View release details
gh release view TAG

# Delete a release
gh release delete TAG --yes
```

## Cloud Provider Support

| Provider | Windows | macOS |
|----------|---------|-------|
| OneDrive | Full support | Detection only |
| iCloud Drive | N/A | Full support |
