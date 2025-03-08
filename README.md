# Cloud File Status Manager

## Description

The com.wkoorts.CloudFileStatusManager package provides a means to read & manage the status of files on cloud drives like iCloud Drive or OneDrive.

## Features

- Read the hydration status (whether or not the file has been downloaded locally) of files on cloud drives.
- Manage the hydration status of files on cloud drives (hydrate or dehydrate files).

### Limitations

- Tested only with iCloud Drive and OneDrive, but may work for other providers.  I suspect that any cloud drive which is implemented with the Windows [Cloud Filter API](https://learn.microsoft.com/en-us/windows/win32/cfapi/cloud-filter-reference) will probably work.
- Initially only supports Windows, but I need a Mac implementation for a project I'm working on, so I'll probably add that soon if it makes sense under the same interfaces.

## Acknowledgements

Thank you to [Hunter Ratliff](https://hratliff.com/), who explained how file attributes in Windows represent the various states of files on cloud drives in [this blog post](https://hratliff.com/posts/icloud-onedrive-syncing-in-cmd/).
