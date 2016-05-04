#tool "OpenCover"
#tool "NUnit.ConsoleRunner"
#tool "ReportGenerator"
#tool "GitVersion.CommandLine"

var target = Argument("target", "Default");
var configuration = Argument("config", "Release");

const string ArtifactsFolder = "./artifacts";
const string TestingFolder = "./testing";

GitVersion version;

Task("Clean")
	.Does(() =>
	{
		CleanDirectory(ArtifactsFolder);
		CleanDirectory(TestingFolder);
	});
	
Task("Version")
	.Does(() =>
	{
		version = GitVersion(new GitVersionSettings
		{
			UpdateAssemblyInfo = true,
			UpdateAssemblyInfoFilePath = "../src/VersionAssemblyInfo.cs",
			ArgumentCustomization = args => args.Append("-ensureassemblyinfo")
		});
	});
	
Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.Does(() =>
	{
		MSBuild(solutionToBuild, new MSBuildSettings
		{
			Configuration = configuration
		});
	});


Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{		
		var dir = Directory(TestingFolder);
		
		var testAssemblies = testProjects.Select(tp =>
		{
			var path = tp.ToString();
			var name = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".dll");
			var newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), $"bin/{configuration}/{name}");
			return MakeAbsolute(File(newPath));
		}).ToArray();
		
		OpenCover(t => 
			t.NUnit3(testAssemblies, new NUnit3Settings {Results = $"{TestingFolder}/testresults.xml" }),
		 	File($"{TestingFolder}/coverresults.xml"),
		 	new OpenCoverSettings()
		 		.WithFilter("+[AethonsTools.*]*")// TODO
		 );
		 
		 ReportGenerator($"{TestingFolder}/coverresults.xml", $"{TestingFolder}/coverage");
	});

Task("Package")
	.IsDependentOn("Test")
	.Does(() =>
	{
		CreateDirectory(ArtifactsFolder);
		NuGetPack(projectsToPackage, new NuGetPackSettings
		{
			OutputDirectory = ArtifactsFolder,
			Version = version.NuGetVersionV2
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
	.Does(() =>
	{
		
	});
	
RunTarget(target);