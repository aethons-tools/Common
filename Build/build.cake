#tool "OpenCover"
#tool "NUnit.ConsoleRunner"
#tool "ReportGenerator"
#tool "GitVersion.CommandLine"

// Get command line arguments 
var target = Argument("target", "Default");
var configuration = Argument("config", "Release");

// Set up paths
const string CommonRoot = "..";
const string RepoRoot = "../..";
const string BuildFolder = RepoRoot + "/build";
const string ArtifactsFolder = BuildFolder + "/artifacts";
const string TestingFolder = BuildFolder + "/testing";

// find out where we are
var segments = Directory(Environment.CurrentDirectory).Path.Segments;
var repoName = segments[segments.Length - 3];

var ProjectUrl = $"https://github.com/aethons-tools/{repoName}";
var LicenseUrl = $"https://github.com/aethons-tools/{repoName}/blob/master/LICENSE.md";
const string IconUrl = "https://github.com/aethons-tools/Common/Images/nuget-icon.png";

// share the git version with everyone
string NuGetVersion;

Task("Clean")
	.Does(() =>
	{
		CleanDirectory(BuildFolder);
	});
	
Task("Version")
	.Does(() =>
	{
		var version = GitVersion(new GitVersionSettings
		{
			RepositoryPath = RepoRoot
		});
		
		NuGetVersion = $"{version.NuGetVersionV2}-build{int.Parse(version.BuildMetaData).ToString("D4")}";
		
		CreateAssemblyInfo($"{CommonRoot}/AssemblyInfo.Version.cs", new AssemblyInfoSettings {
			Version = version.AssemblySemVer,
			FileVersion = $"{version.MajorMinorPatch}.{version.BuildMetaData}",
			InformationalVersion = NuGetVersion
		});
	});
	
Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.Does(() =>
	{
		var solutionName = $"{RepoRoot}/{repoName}.sln";
		
		if (!FileExists(solutionName))
			throw new Exception($"Could not find {solutionName}");
			
		MSBuild(solutionName, new MSBuildSettings
		{
			Configuration = configuration
		});
	});


Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{		
		CreateDirectory(TestingFolder);

		var testAssemblies = GetFiles($"{RepoRoot}/test/**/*.csproj").Select(tp =>
		{
			var path = tp.ToString();
			var name = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".dll");
			var newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), $"bin/{configuration}/{name}");
			return MakeAbsolute(File(newPath));
		}).ToArray();
		
		var openCoverSettings = GetFiles($"{RepoRoot}/src/**/*.csproj")
			.Aggregate(new OpenCoverSettings(), (a, p) => a.WithFilter($"+[{p.GetFilenameWithoutExtension()}]*"));
		OpenCover(t => 
			t.NUnit3(testAssemblies, new NUnit3Settings {Results = $"{TestingFolder}/testresults.xml" }),
		 	File($"{TestingFolder}/coverresults.xml"),
		 	openCoverSettings
		 );
		 
		 ReportGenerator($"{TestingFolder}/coverresults.xml", $"{TestingFolder}/coverage");
	});

Task("Package")
	.IsDependentOn("Test")
	.Does(() =>
	{
		CreateDirectory(ArtifactsFolder);

		var properties = new Dictionary<string, string>();
		properties["Configuration"] = configuration;
		
		NuGetPack(GetFiles($"{RepoRoot}/src/**/*.csproj"), new NuGetPackSettings
		{
			OutputDirectory = ArtifactsFolder,
			Version = NuGetVersion,
			ProjectUrl = new Uri(ProjectUrl),
			IconUrl = new Uri(IconUrl),
			LicenseUrl = new Uri(LicenseUrl),
			IncludeReferencedProjects = true,
			Properties = properties
		});
	});
	
Task("PublishLocal")
	.IsDependentOn("Package")
	.Does(() =>
	{
		var path = EnvironmentVariable("local_nuget_repo");
		if (path != null)
		{
			CopyFiles($"{ArtifactsFolder}/*.nupkg", path);
			Information("Packges copied to '{0}'", path);
		}
		else
		{
			Information("Packages not copied to local repo; to use a local repo, set the 'local_nuget_repo' environment variable to the path.");
		}
	});
	
Task("Default")
	.IsDependentOn("PublishLocal")
	.Does(() => {});
	
Task("Init")
	.IsDependentOn("Version")
	.Does(() => {});
	
RunTarget(target);