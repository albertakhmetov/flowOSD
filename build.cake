#addin nuget:?package=Cake.VersionReader&version=5.1.0

var target = Argument("target", "Setup");
var configuration = Argument("configuration", "Release");

var publishOutputFolder = "./output/publish";
var setupOutputFolder = "./output/setup";

Task("Clean")
    .Does(() => {
        CleanDirectory(publishOutputFolder);
        CleanDirectory(setupOutputFolder);
    });

Task("Publish")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetPublish("./src", new DotNetPublishSettings {
            OutputDirectory = publishOutputFolder,
            SelfContained = true,
        });
    });

Task("Setup")
    .IsDependentOn("Publish")
    .Does(() => {
        InnoSetup("./installer/installer.iss", new InnoSetupSettings {
            OutputDirectory = setupOutputFolder,
            Defines = new Dictionary<string, string> { 
                { "AppVersion", GetVersionNumber(System.IO.Path.Combine(publishOutputFolder, "flowOSD.exe")) } },
        });
    });

RunTarget(target);