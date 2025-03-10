using System.CommandLine;
using CloudFileStatusManager;
using CloudFileStatusManager.Enums;
using CloudFileStatusManager.Windows;

ICloudFileStatusManager cloudFileStatusManager = new WindowsCloudFileStatusManager();

var rootCommand = new RootCommand("Cloud File Status Manager CLI for Windows");

var fileArgument = new Argument<string>("file", "The path of the file to manage");
var verboseOption = new Option<bool>(["--verbose", "-v"], "Show verbose output");
rootCommand.AddOption(verboseOption);

// is-on-cloud-storage command
var isOnCloudStorageCommand = new Command("is-on-cloud-storage", "Check if a file is on cloud storage")
{
    fileArgument,
    verboseOption
};
isOnCloudStorageCommand.SetHandler((filePath, verbose) =>
{
    var isOnCloudStorage = cloudFileStatusManager.IsOnCloudStorage(filePath, verbose);
    Console.Write($"File is likely{(!isOnCloudStorage ? " not" : "")} on cloud storage.");
}, fileArgument, verboseOption);
rootCommand.AddCommand(isOnCloudStorageCommand);

// get-hydration-status command
var getHydrationStatusCommand = new Command("get-hydration-status", "Get the hydration status of a file")
{
    fileArgument,
    verboseOption
};
getHydrationStatusCommand.SetHandler((filePath, verbose) =>
{
    var status = cloudFileStatusManager.GetHydrationStatus(filePath, verbose);
    Console.WriteLine(status);
}, fileArgument, verboseOption);

rootCommand.AddCommand(getHydrationStatusCommand);

// get-pin-status command
var getPinStatusCommand = new Command("get-pin-status", "Get the pin status of a file")
{
    fileArgument,
    verboseOption
};
getPinStatusCommand.SetHandler((filePath, verbose) =>
{
    var status = cloudFileStatusManager.GetPinStatus(filePath, verbose);
    Console.WriteLine(status);
}, fileArgument, verboseOption);
rootCommand.AddCommand(getPinStatusCommand);

// hydrate command
var hydrateCommand = new Command("hydrate", "Hydrate a file")
{
    fileArgument,
    verboseOption
};
hydrateCommand.SetHandler((filePath, verbose) =>
{
    var status = cloudFileStatusManager.GetHydrationStatus(filePath, verbose);
    if (status == FileHydrationStatus.Hydrated)
    {
        Console.WriteLine("File is already hydrated");
    }
    else
    {
        Console.WriteLine("Hydrating file (this may take some time depending on the size of the file)...");
        cloudFileStatusManager.HydrateFile(filePath, verbose);
        Console.WriteLine("File hydrated");
    }
}, fileArgument, verboseOption);
rootCommand.AddCommand(hydrateCommand);

// dehydrate command
var dehydrateCommand = new Command("dehydrate", "Dehydrate a file")
{
    fileArgument,
    verboseOption
};
dehydrateCommand.SetHandler((filePath, verbose) =>
{
    var status = cloudFileStatusManager.GetHydrationStatus(filePath, verbose);
    if (status == FileHydrationStatus.Dehydrated)
    {
        Console.WriteLine("File is already dehydrated");
    }
    else
    {
        cloudFileStatusManager.DehydrateFile(filePath, verbose);
        Console.WriteLine("File dehydrated");
    }
}, fileArgument, verboseOption);
rootCommand.AddCommand(dehydrateCommand);

await rootCommand.InvokeAsync(args);
