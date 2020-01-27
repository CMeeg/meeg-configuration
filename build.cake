//////////////////////////////////////////////////////////////////////
// Directives
//////////////////////////////////////////////////////////////////////

#tool nuget:?package=GitVersion.CommandLine&version=5.1.3
#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0
#addin "nuget:?package=Cake.Incubator&version=5.1.0"

//////////////////////////////////////////////////////////////////////
// Arguments
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// Context
//////////////////////////////////////////////////////////////////////

public class BuildData
{
    public string ProjectName { get; }
    public string Configuration { get; }
    public ConvertableDirectoryPath SourceDirectoryPath { get; }
    public ConvertableDirectoryPath DistDirectoryPath { get; }
    public ConvertableFilePath SolutionFilePath { get; }
    public ConvertableDirectoryPath ProjectDirectoryPath { get; }
    public ConvertableDirectoryPath BinDirectoryPath { get; }
    public GitVersion Version { get; set; }

    public BuildData(ICakeContext context, string projectName, string configuration)
    {
        ProjectName = projectName;
        Configuration = configuration;

        SourceDirectoryPath = context.Directory("./src");
        DistDirectoryPath = context.Directory("./.dist");

        SolutionFilePath = SourceDirectoryPath + context.File($"{projectName}.sln");
        ProjectDirectoryPath = SourceDirectoryPath + context.Directory(projectName);
        BinDirectoryPath = ProjectDirectoryPath + context.Directory("bin") + context.Directory(configuration);
    }
}

//////////////////////////////////////////////////////////////////////
// Setup
//////////////////////////////////////////////////////////////////////

Setup<BuildData>(setupContext => {
    return new BuildData(
        setupContext,
        "Meeg.Configuration",
        configuration
    );
});

//////////////////////////////////////////////////////////////////////
// Tasks
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does<BuildData>(data =>
{
    Information("Cleaning " + data.BinDirectoryPath);
    CleanDirectory(data.BinDirectoryPath);

    Information("Cleaning " + data.DistDirectoryPath);
    CleanDirectory(data.DistDirectoryPath);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does<BuildData>(data =>
{
    NuGetRestore(data.SolutionFilePath);
});

Task("Version")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does<BuildData>(data =>
{
    data.Version = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    });

    Information(data.Version.Dump());
});

Task("Build")
    .IsDependentOn("Version")
    .Does<BuildData>(data =>
{
    MSBuild(
        data.SolutionFilePath,
        settings => settings
            .SetConfiguration(data.Configuration)
            .SetVerbosity(Verbosity.Minimal)
    );
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does<BuildData>(data =>
{
    NUnit3("./src/**/bin/" + data.Configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
    });
});

Task("NuGet-Pack")
    .IsDependentOn("Run-Unit-Tests")
    .Does<BuildData>(data =>
{
    // Get assembly info, which we will use to populate package metadata

    FilePath assemblyInfoFilePath = data.ProjectDirectoryPath + Directory("Properties") + File("AssemblyInfo.cs");
    AssemblyInfoParseResult assemblyInfo = ParseAssemblyInfo(assemblyInfoFilePath);

    // Create the NuGet package

    var settings = new NuGetPackSettings {
        OutputDirectory = data.DistDirectoryPath,
        Properties = new Dictionary<string, string> {
            { "id", assemblyInfo.Title },
            { "version", data.Version.FullSemVer },
            { "description", assemblyInfo.Description },
            { "author", assemblyInfo.Company },
            { "copyright", assemblyInfo.Copyright },
            { "configuration", data.Configuration }
        },
        Symbols = true,
        ArgumentCustomization = args => args.Append("-SymbolPackageFormat snupkg")
    };

    NuGetPack(data.ProjectDirectoryPath + File($"{data.ProjectName}.nuspec"), settings);
});

Task("NuGet-Publish")
    .IsDependentOn("NuGet-Pack")
    .Does<BuildData>(data =>
{
    if (!BuildSystem.IsLocalBuild)
    {
        Error("This script will only publish to a local NuGet feed.");

        return;
    }

    string nuGetFeedPath = EnvironmentVariable("NUGET_LOCAL_FEED_PATH");

    if (string.IsNullOrEmpty(nuGetFeedPath))
    {
        Error("NUGET_LOCAL_FEED_PATH environment variable not set.");

        return;
    }

    NuGetInit(
        data.DistDirectoryPath,
        nuGetFeedPath
    );
});

//////////////////////////////////////////////////////////////////////
// Targets
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

Task("Package")
    .IsDependentOn("NuGet-Pack");

Task("Publish")
    .IsDependentOn("NuGet-Publish");

//////////////////////////////////////////////////////////////////////
// Execution
//////////////////////////////////////////////////////////////////////

RunTarget(target);
