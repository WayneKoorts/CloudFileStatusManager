# Cloud File Status Manager

## Description

This is a .NET library and CLI utility for reading & managing the hydration and pin status of files on cloud drives like iCloud Drive or OneDrive.

### Packages

- [CloudFileStatusManager](https://www.nuget.org/packages/com.wkoorts.CloudFileStatusManager)
  - Contains the `ICloudFileStatusManager` interface, and enums for the hydration and pin status of files. This is a cross-platform package and required in all cases.
- [CloudFileStatusManager.Windows](https://www.nuget.org/packages/com.wkoorts.CloudFileStatusManager.Windows)
  - Contains the Windows implementation of the `ICloudFileStatusManager` interface.  You also need this one in all cases at the moment because Windows is the only supported platform, but there will be a macOS version in the future.

## Features

- Read the hydration status (whether or not the file has been downloaded locally) of files on cloud drives.
- Manage the hydration status of files on cloud drives (hydrate or dehydrate files).

## Limitations

- Tested only with iCloud Drive and OneDrive, but may work for other providers.  I suspect that any cloud drive which is implemented with the Windows [Cloud Filter API](https://learn.microsoft.com/en-us/windows/win32/cfapi/cloud-filter-reference) will probably work.
- Initially only supports Windows, but I need a Mac implementation for a project I'm working on, so I'll probably add that soon if it makes sense under the same interfaces.

## Project & Package Structure

- CloudFileStatusManager project
  - Produces the CloudFileStatusManager package (`com.wkoorts.CloudFileStatusManager`).
  - Contains the `ICloudFileStatusManager` interface, and enums for the hydration and pin status of files.
- CloudFileStatusManager.Windows project
  - Produces the CloudFileStatusManager.Windows package (`com.wkoorts.CloudFileStatusManager.Windows`).
  - Contains the Windows implementation of the `ICloudFileStatusManager` interface.
- CloudFileStatusManager.CLI.Windows project
  - This is a command-line interface for `CloudFileStatusManager.Windows`.
  - Produces no package.

## Using the CLI

Binaries for the CLI are available to download from the [Releases page](https://github.com/WayneKoorts/CloudFileStatusManager/releases).

```shell
$ ./cfsm.exe --help
Description:
  Cloud File Status Manager CLI for Windows

Usage:
  cfsm [command] [options]

Options:
  -v, --verbose   Show verbose output
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  is-on-cloud-storage <file>   Check if a file is on cloud storage
  get-hydration-status <file>  Get the hydration status of a file
  get-pin-status <file>        Get the pin status of a file
  hydrate <file>               Hydrate a file
  dehydrate <file>             Dehydrate a file
```

## Acknowledgements

Thank you to [Hunter Ratliff](https://hratliff.com/), who explained how file attributes in Windows represent the various states of files on cloud drives in [this blog post](https://hratliff.com/posts/icloud-onedrive-syncing-in-cmd/).

## Donate

<a href="https://www.buymeacoffee.com/waynekoorts" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-yellow.png" alt="Buy Me A Coffee" height="26" width="113"></a>
