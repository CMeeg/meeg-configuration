//////////////////////////////////////////////////////////////////////
// Directives
//////////////////////////////////////////////////////////////////////

#tool nuget:?package=GitVersion.CommandLine&version=5.1.3
#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0
#addin "nuget:?package=Cake.Incubator&version=5.1.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

string projectName = "Meeg.Configuration";
GitVersion version;

var solutionPath = File($"./src/{projectName}.sln");
var projectDir = Directory($"./src/{projectName}");
var binDir = projectDir + Directory("bin") + Directory(configuration);
var distDir = Directory("./.dist");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    Information("Cleaning " + binDir);
    CleanDirectory(binDir);

    Information("Cleaning " + distDir);
    CleanDirectory(distDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(solutionPath);
});

Task("Version")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    version = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    });

    Information(version.Dump());
});

Task("Build")
    .IsDependentOn("Version")
    .Does(() =>
{
    MSBuild(
        solutionPath,
        settings => settings
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
    );
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
    });
});

Task("NuGet-Pack")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    // Get assembly info, which we will use to populate package metadata

    FilePath assemblyInfoPath = projectDir + Directory("Properties") + File("AssemblyInfo.cs");
    AssemblyInfoParseResult assemblyInfo = ParseAssemblyInfo(assemblyInfoPath);

    // Create the NuGet package

    var settings = new NuGetPackSettings {
        // BasePath = binDir,
        Symbols = true,
        OutputDirectory = distDir,
        Properties = new Dictionary<string, string> {
            { "id", assemblyInfo.Title },
            { "version", version.NuGetVersion },
            { "description", assemblyInfo.Description },
            { "author", assemblyInfo.Company },
            { "copyright", assemblyInfo.Copyright },
            { "configuration", configuration }
        }
    };

    NuGetPack(projectDir + File($"{projectName}.nuspec"), settings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

Task("Package")
    .IsDependentOn("NuGet-Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
