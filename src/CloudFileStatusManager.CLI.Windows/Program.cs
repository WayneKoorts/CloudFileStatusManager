using CloudFileStatusManager;
using CloudFileStatusManager.CLI.Common;
using CloudFileStatusManager.Windows;

ICloudFileStatusManager cloudFileStatusManager = new WindowsCloudFileStatusManager();
return await CliRunner.RunAsync(cloudFileStatusManager, "Windows", args);
