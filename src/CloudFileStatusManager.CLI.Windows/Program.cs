using System.CommandLine;
using CloudFileStatusManager;
using CloudFileStatusManager.Windows;

ICloudFileStatusManager cloudFileStatusManager = new WindowsCloudFileStatusManager();

var rootCommand = new RootCommand("Cloud File Status Manager CLI for Windows");

var fileArgument = new Argument<string>("file", "The path of the file to manage");

// get-hydration-status command
var getHydrationStatusCommand = new Command("get-hydration-status", "Get the hydration status of a file")
{
    fileArgument
};
getHydrationStatusCommand.SetHandler(filePath =>
{
    var status = cloudFileStatusManager.GetHydrationStatus(filePath);
    Console.WriteLine(status);
}, fileArgument);

rootCommand.AddCommand(getHydrationStatusCommand);

// get-pin-status command
var getPinStatusCommand = new Command("get-pin-status", "Get the pin status of a file")
{
    fileArgument
};
getPinStatusCommand.SetHandler(filePath =>
{
    var status = cloudFileStatusManager.GetPinStatus(filePath);
    Console.WriteLine(status);
}, fileArgument);

rootCommand.AddCommand(getPinStatusCommand);

await rootCommand.InvokeAsync(args);
