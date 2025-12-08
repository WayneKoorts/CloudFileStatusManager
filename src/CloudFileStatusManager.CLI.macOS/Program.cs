using CloudFileStatusManager;
using CloudFileStatusManager.CLI.Common;
using CloudFileStatusManager.macOS;

ICloudFileStatusManager cloudFileStatusManager = new MacOSCloudFileStatusManager();
return await CliRunner.RunAsync(cloudFileStatusManager, "macOS", args);
