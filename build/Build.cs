using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CliWrap;
using System.Text;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using CliWrap.Buffered;
using Kinderworx.Utilities.BuildUtilities;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using System.Threading.Tasks;

class Build : NukeBuild
{
    /// <summary>
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    /// </summary>

    /// <summary>
    /// Nerdbank gitversioning tool.
    /// </summary>
    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;

    /// <summary>
    ///  Git repository object.
    /// </summary>
    [GitRepository]
    readonly GitRepository Repository;

    /// <summary>
    ///  Visual Studio solution object.
    /// </summary>
    [Solution]
    readonly Solution Solution;

    #region Project variables

    /// <summary>
    /// Path to the util csproj.
    /// </summary>
    public string utilsProjectPath { get; private set; }

    /// <summary>
    /// Utils project directory.
    /// </summary>
    public string utilsProjectDir { get; set;  }

    public string utilsProjectName { get; private set; }

    #endregion

    public string OctopusVersion { get; private set; }

    public string CloudBuildNo { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string CodeCoverage = "true";



    public string Space = " ";



    #region NetCoreBuild

    string SelfContained = string.Empty;
    string runtime = "win-x64";

    #endregion

    /// <summary>
    ///
    /// </summary>
    string CoverletFile = string.Empty;

    #region Secrets

    /// <summary>
    ///  ProGet server key.
    /// </summary>
    [Parameter][Secret]    readonly string NuGetApiKey;

    /// <summary>
    /// Codecov test token.
    /// </summary>
    [Secret] [Parameter] readonly string CODECOV_SECRET;

    /// <summary>
    /// Sonarqube API Key.
    /// </summary>
    [Secret]  [Parameter] readonly string SonarKey;

    /// <summary>
    /// GitHub PAT token.
    /// </summary>
    [Secret] [Parameter] readonly string GitHubToken;

    /// <summary>
    /// Sny API Token.
    /// </summary>
    [Secret] [Parameter] readonly string SNYK_TOKEN;

    /// <summary>
    ///     License key for Report Generator.
    /// </summary>
    [Parameter]  [Secret] readonly string ReportGeneratorLicense;

    /// <summary>
    ///     Dependency Track API Key.
    /// </summary>
    [Parameter] [Secret] readonly string DTrackApiKey2;

    #endregion


    #region Paths

    /// <summary>
    ///
    /// </summary>
    readonly AbsolutePath UtilsDir = RootDirectory / "Utilities";

    /// <summary>
    /// s.
    /// </summary>
    readonly AbsolutePath BuildDir = RootDirectory / "Nuke" / "Output" / "Build";

    /// <summary>
    /// Codecov config file.
    /// </summary>
    readonly AbsolutePath CodecovYml = RootDirectory / "codecov.yml";

    /// <summary>
    /// Artifacts directory.
    /// </summary>
    readonly AbsolutePath Artifacts = RootDirectory / "Nuke" / "Artifacts";

    /// <summary>
    ///  Output of coverlet code  coverage report.
    /// </summary>
    readonly AbsolutePath QodanaOut = RootDirectory / "Nuke" / "Qodana" / "Results";

    /// <summary>
    ///  Output of coverlet code  coverage report.
    /// </summary>
    readonly AbsolutePath QodanaReport = RootDirectory / "Nuke" / "Qodana" / "Report";

    /// <summary>
    ///  Output of coverlet code  coverage report.
    /// </summary>
    readonly AbsolutePath QodanaCache = @"D:\QodanaCache";

    /// <summary>
    ///  Output of coverlet code  coverage report.
    /// </summary>
    readonly AbsolutePath CoverletOutput = RootDirectory / "Nuke" / "Output" / "Coverlet";

    /// <summary>
    /// NDependOutput folder.
    /// </summary>
    readonly AbsolutePath NukeOut = RootDirectory / "Nuke";

    /// <summary>
    ///  NDependOutput folder.
    /// </summary>
    readonly AbsolutePath NDependOutput = RootDirectory / "Nuke" / "Output" / "NDependOut";

    /// <summary>
    /// GGShield config file.
    /// </summary>
    readonly AbsolutePath GgConfig = RootDirectory / "gitguardian.yml";

    /// <summary>
    /// Dotnet publish output directory
    /// </summary>
    readonly AbsolutePath PublishFolder = RootDirectory / "Nuke" / "Output" / "Publish";

    /// <summary>
    /// PVS Studio log output folder.
    /// </summary>
    readonly AbsolutePath PvsStudio = RootDirectory / "Nuke" / "Output" / "PVS";

    /// <summary>
    ///     Path to nupkg file from the project
    /// </summary>
    readonly AbsolutePath NupkgPath = RootDirectory / "Nuke" / "Output" / "Nuget";

    /// <summary>
    /// Coverlet report folder.
    /// </summary>
    readonly AbsolutePath ReportOut = RootDirectory / "Nuke" / "Output" / "Coverlet" / "Report";

    /// <summary>
    ///  Output directory of the SBOM file from CycloneDX
    /// </summary>
    readonly AbsolutePath Sbom = RootDirectory / "Nuke" / "Output" / "SBOM";


    /// <summary>
    /// Filename of changelog file.
    /// </summary>
    string ChangeLogFile => RootDirectory / "changelog.md";

    /// <summary>
    ///  Docfx folder.
    /// </summary>

    AbsolutePath DocFxLibrary => RootDirectory / "docfx_project";

    /// <summary>
    /// Directory of MSTests project.
    /// </summary>
    AbsolutePath TestsDirectory => RootDirectory / "Tests";

    /// <summary>
    /// Directory of MSTests project.
    /// </summary>
    AbsolutePath CoverletSrc => RootDirectory / "Tests" / "unittestresults";


    /// <summary>
    /// Target path.
    /// </summary>
    readonly AbsolutePath TargetPath = RootDirectory / "Nuke" / "Output" / "Coverlet" / "Report";

    #endregion

    #region Remote Services

    readonly string ProgetUrl = "";

    /// <summary>
    ///     DependencyTrack application URL.
    /// </summary>
    readonly string DependencyTrackUrl = "http://10.0.0.47:8081";

    /// <summary>
    ///     Sonarqube URL.
    /// </summary>
    readonly string SonarqubeUrl = "http://10.1.0.11:9000";

    /// <summary>
    /// Teamscale server URL.
    /// </summary>
    readonly string TeamscaleUrl = "";

    #endregion'

    #region Tools

    /// <summary>
    /// Auto change log cmd for changelog creation.
    /// </summary>
    [PathVariable("auto-changelog")] readonly Tool AutoChangelogTool;

    /// <summary>
    ///     Octopus CLI.
    /// </summary>
    [PathVariable("dotnet-octo")]
    readonly Tool DotnetOcto;

    /// <summary>
    ///  Dotnet-sonarscanner cli tool.
    /// </summary>
    [PathVariable("dotnet-sonarscanner")]
    readonly Tool SonarscannerTool;

    /// <summary>
    /// TrojanSource Finder.
    /// </summary>
    [PathVariable("tsfinder")] readonly Tool TsFinderTool;

    /// <summary>
    /// NDepend Console exe.
    /// </summary>
    [PathVariable(@"NDepend.Console.exe")] readonly Tool NDependConsoleTool;

    /// <summary>
    /// PVS Studio Cmd.
    /// </summary>
    [PathVariable(@"PVS-Studio_Cmd.exe")] readonly Tool PvsStudioTool;

    //    /// <summary>
    //    /// PlogConverter tool from PVS-Studio.
    //    /// </summary>
    [PathVariable(@"PlogConverter.exe")] readonly Tool PlogConverter;

    //    /// <summary>
    //    /// Dotnet Reactor Console exe.
    //    /// </summary>
    [PathVariable(@"dotNET_Reactor.Console.exe")] readonly Tool Eziriz;

    //    /// <summary>
    //    /// Go cli.
    //    /// </summary>
    [PathVariable(@"go.exe")] readonly Tool Go;

    //    /// <summary>
    //    /// DependencyTrack-audit CLI tool.
    //    /// </summary>
    [PathVariable(@"dtrack-audit")] readonly Tool DTrackAudit;

    /// <summary>
    ///     GitHub cli.
    /// </summary>
    [PathVariable("gh")]
    readonly Tool GitHubCli;

    /// <summary>
    ///     Qodana CLI.
    /// </summary>
    [PathVariable("qodana")]
    readonly Tool Qodana;


    // <summary>
    /// <summary>
    /// Docfx cli tool.
    /// </summary>
    /// DocFX CLI.
    /// </summary>
    [PathVariable("docfx")]
    readonly Tool DocFx;

    /// <summary>
    /// Dotnet cli.
    /// </summary>
    [PathVariable("dotnet")]
    readonly Tool DotNet;

    /// <summary>
    ///     Dotnet-format dotnet tool.
    /// </summary>
    [PathVariable("dotnet-format")]
    readonly Tool DotnetFormatTool;


    /// <summary>
    ///     Git cli.
    /// </summary>
    [PathVariable("nuget")]
    readonly Tool NugetCli;

    /// <summary>
    ///  GGShield CLI for detecting secrets.
    /// </summary>
    [PathVariable("ggshield")]
    readonly Tool GgShield;

    /// <summary>
    ///     ReportGenerator tool.
    /// </summary>
    [PathVariable("reportgenerator")]
    readonly Tool ReportGenerator;

    //    /// <summary>
    //    /// Codecov CLI.
    //    /// </summary>
    [PathVariable("codecov")] readonly Tool Codecov;

    /// <summary>
    /// GGCli for scanning secrets.
    /// </summary>
    [PathVariable("ggshield")] readonly Tool GGShield;

    /// <summary>
    /// NDepend CLI.
    /// </summary>
    [PathVariable("NDepend.console.exe")] readonly Tool NDepend;

    // <summary>
    // Snyk cli.
    // </summary>
    [PathVariable("snyk")] readonly Tool SnykTool;

    #endregion


    public static int Main () => Execute<Build>(x => x.PublishGitHubRelease);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    /// <summary>
    /// Set and create build paths.
    /// </summary>
    Target SetPathsTarget => _ => _
        .Executes(() =>
        {
            Directory.CreateDirectory(NupkgPath.ToString());
            Directory.CreateDirectory(PublishFolder.ToString());
            Directory.CreateDirectory(NDependOutput.ToString());
            Directory.CreateDirectory(BuildDir.ToString());
            Directory.CreateDirectory(PublishFolder.ToString());
            Directory.CreateDirectory(PvsStudio.ToString());
            Directory.CreateDirectory(CoverletOutput.ToString());
            Directory.CreateDirectory(ReportOut.ToString());
            Directory.CreateDirectory(TargetPath.ToString());
            Directory.CreateDirectory(Sbom.ToString());
            Directory.CreateDirectory(Artifacts.ToString());
            Directory.CreateDirectory(QodanaOut.ToString());
            Directory.CreateDirectory(QodanaReport.ToString());
        });

    /// <summary>
    /// Set build variables.
    /// </summary>
    Target SetVariablesTarget => _ => _
        .After(SetPathsTarget)
        .DependsOn(SetPathsTarget)
        .Executes(() =>
        {

            utilsProjectName = "Kinderworx.Utilities.BuildUtilities";
            utilsProjectPath = Solution.GetProject(utilsProjectName).Path;

            Log.Information(utilsProjectName);
            Log.Information(utilsProjectPath);
            Log.Information(Solution.GetProject(utilsProjectName).Path);
        });

    /// <summary>
    /// Creates documentation.
    /// </summary>
    Target CreateDocFx => _ => _
        .DependsOn(SetVariablesTarget)
        .AssuredAfterFailure()
        .Executes(async () =>
        {

            DocFx("build docfx.json", DocFxLibrary);
        });

    /// <summary>
    ///  Authenticate to Synk service.
    /// </summary>
    Target SnykAuth => _ => _
        .DependsOn(CreateDocFx)
        .Description("Authenticate to Snyk.")
        .AssuredAfterFailure()
        .Executes(() =>
        {

            SnykTool($"auth {SNYK_TOKEN}");
        });

    Target SnykScan => _ => _
        .DependsOn(SnykAuth)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SnykTool($"code test", RootDirectory);
        });

    /// <summary>
    /// Run dotnet format to format code.
    /// </summary>
    Target RunDotnetFormatTarget => _ => _
        .DependsOn(SnykScan)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DotnetFormatTool(utilsProjectPath + " -v diagnostic");
        });


    /// <summary>
    ///  Scan code for hardcoded secrets.
    /// </summary>
    Target SecretScan => _ => _
        .DependsOn(RunDotnetFormatTarget)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (IsLocalBuild)
            {
                GgShield($"--config-path {GgConfig} secret scan commit-range HEAD~1");
            }
        });

    /// <summary>
    ///  Runs dotnet outdated against Nuget packages.
    /// </summary>
    Target RunDotnetOutdated => _ => _
        .DependsOn(SecretScan)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DotNet($"outdated {RootDirectory}");
        });

    /// <summary>
    ///     Runs dotnet outdated against Nuget packages.
    /// </summary>
    Target RunQodanaScan => _ => _
        .DependsOn(RunDotnetOutdated)
        .Description("Runs Qodana linter")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            Qodana($"scan --ide QDNET --results-dir {QodanaOut} --report-dir {QodanaReport} --cache-dir {QodanaCache}");
        });

    /// <summary>
    /// Executes NDepend Analysis.
    /// </summary>
    Target RunNDepend => _ => _
        .DependsOn(RunQodanaScan)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            var nDependProj = RootDirectory.GlobFiles("*.ndproj").FirstOrDefault();
            string ndependPath = Path.Combine(Artifacts, "NDepend.zip");
            NDependConsoleTool(string.Format(nDependProj + Space + "/" + "OutDir {0}", NDependOutput));

            if (IsServerBuild)
            {
                BuildUtils.ZipDirectory(NDependOutput, ndependPath);
            }
        });

    /// <summary>
    ///  Create sbom json using CycloneDX.
    /// </summary>
    Target CycloneDx => _ => _
        .DependsOn(RunNDepend)
        .Produces(Sbom / "bom.json")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DotNet(@$"cyclonedx {utilsProjectName} -o {Sbom} -j -dgl");
        });


    /// <summary>
    /// Versions the project using Nerdbank.
    /// </summary>
    Target SetVersionTarget => _ => _
        .DependsOn(CycloneDx)
        .AssuredAfterFailure()
        .Executes(async () =>
        {
            if (IsLocalBuild || (IsServerBuild && !Repository.IsOnMainOrMasterBranch()))
            {

                var dbDailyTasks = await Cli.Wrap("powershell")
                  .WithArguments(new string[] { "nbgv get-version | ConvertTo-JSON" })
                  .ExecuteBufferedAsync();

                OctopusVersion = BuildUtils.ExtractVersion(dbDailyTasks.StandardOutput);

                Log.Information(OctopusVersion);
            }

            // When the code is merged.
            if (IsServerBuild)
            {
                var c =
                    new NerdbankGitVersioningCloudSettings();

                c.SetProcessWorkingDirectory(RootDirectory);

                NerdbankGitVersioningTasks.NerdbankGitVersioningCloud(c);

                CloudBuildNo = NerdbankVersioning.CloudBuildNumber;

            }
        });

    /// <summary>
    ///     Push to Dependency-Track.
    /// </summary>
    Target PushToDTrack => _ => _
        .DependsOn(SetVersionTarget)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            var sbomPath = Sbom / "bom.json";

                DTrackAudit(
                    @$"-a -k {DTrackApiKey2} -n {BuildUtils.Helper(utilsProjectName)} -u {DependencyTrackUrl} -v {OctopusVersion} -i {sbomPath}");
        });

    /// <summary>
    /// Run PVS Studio static analysis.
    /// </summary>
    Target RunPvsStudio => _ => _
        .DependsOn(PushToDTrack)
         .AssuredAfterFailure()
        .Executes(() =>
        {
            var pvsfile = string.Empty;
            var plogFile = PvsStudio / "pvs-studio.plog";
            pvsfile = plogFile.ToString();


            string pvsPath = Path.Combine(Artifacts, "PVSReport.zip");

            string sln = Solution.Path;
            PvsStudioTool($@"-t {sln} -o {pvsfile}");
            PlogConverter($@"-t Html -o {PvsStudio} -n PVS-Log {pvsfile}");



                if (IsServerBuild)
                {
                    BuildUtils.ZipDirectory(NDependOutput, pvsPath);
                }
        });

    /// <summary>
    ///     Starts Sonarqube scanner.
    /// </summary>
    Target StartSonarscan => _ => _
        .DependsOn(RunPvsStudio)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SonarscannerTool(
                @$"begin /k:{BuildUtils.Helper(utilsProjectName)}  /d:sonar.host.url={SonarqubeUrl} /d:sonar.token={SonarKey} /d:sonar.verbose=true");
        });

    /// <summary>
    /// Clean working dir.
    /// </summary>
    Target Clean => _ => _
        .DependsOn(StartSonarscan)
         .AssuredAfterFailure()
        .Executes(() =>
        {
            RootDirectory
                .GlobDirectories("**/{obj,bin}")
                .DeleteFiles();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
         .AssuredAfterFailure()
        .Executes(() =>
        {
            DotNet($"restore {utilsProjectPath}");
        });

    Target Compile => _ => _
        .DependsOn(Restore)
       .AssuredAfterFailure()
        .Executes(() =>
        {
                DotNet($"build {utilsProjectPath} -f {framework} --self-contained {SelfContained} --output {PublishFolder}");
        });


    /// <summary>
    ///  Obfuscate build dll.
    /// </summary>
    Target RunTests => _ => _
        .DependsOn(Compile)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            var testProj = TestsDirectory.GlobFiles("*.Tests.csproj").FirstOrDefault();

            Log.Information(testProj.Name);

            // Execute dotnet test to run the unit tests.
            DotNet($@"test {testProj.ToString()} /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./unittestresults/");

            // Coverage xml file.
            var sourceFile = Path.Combine(CoverletSrc.ToString(), "coverage.opencover.xml");
            CoverletFile = Path.Combine(CoverletOutput.ToString(), "coverage.opencover.xml");

            File.Copy(sourceFile, CoverletFile, true);

            if (File.Exists(CoverletFile)) File.Delete(sourceFile);
        });


    /// <summary>
    ///  Creates Report of test coverage.
    /// </summary>
    Target RunReportGeneratorTarget => _ => _
        .DependsOn(RunTests)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            string zipPath = Path.Combine(Artifacts, "testreports.zip");

            // targetPath is the folder for report.
            // so this will be ApiPath sub folder of coverletOutput.
            ReportGenerator(
                $"-reports:{CoverletFile} -targetdir:{ReportOut} -reporttypes:Html;TeamCitySummary;PngChart;Badges --license:{ReportGeneratorLicense}");

            if (IsServerBuild)
            {
                BuildUtils.ZipDirectory(ReportOut, zipPath);
                Console.WriteLine($"##teamcity[publishArtifacts '{zipPath}']");
            }
        });

    /// <summary>
    ///  Upload code coverage results to codecov
    /// </summary>
    Target UploadToCodeCov => _ => _
        .DependsOn(RunReportGeneratorTarget)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (IsLocalBuild)
            {
                Log.Information(CodecovYml);

                Codecov($"--codecov-yml-path {CodecovYml} create-commit -t {CODECOV_SECRET} ", RootDirectory.ToString());
                Codecov($"--codecov-yml-path {CodecovYml}  create-report -t {CODECOV_SECRET} ", RootDirectory.ToString());
                Codecov($"--codecov-yml-path {CodecovYml}  do-upload -t {CODECOV_SECRET} ", RootDirectory.ToString());
            }

            // This runs on Teamcity, using env vars.
            if (IsServerBuild) {
                Codecov($"-f {CoverletFile} -t {CODECOV_SECRET}", CoverletOutput.ToString());
            }
        });


    /// <summary>
    ///     Ends Sonarqube analysis.
    /// </summary>
    Target EndSonarscanTarget => _ => _
        .DependsOn(UploadToCodeCov)
        .Description("End SonarQube scan")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SonarscannerTool($"end /d:sonar.login=\"{SonarKey}\"");
        });



    /// <summary>
    ///  Set changelog file.
    /// </summary>
    Target AmendChangelogTarget => _ => _
        .DependsOn(EndSonarscanTarget)
        .Description("Creates a changelog of the current commit.")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (IsLocalBuild)
                AutoChangelogTool($"-v  {OctopusVersion} -o {ChangeLogFile}",
                    RootDirectory.ToString()); // Use .autochangelog settings in file.
        });


    Target PushToGitHub => _ => _
        .DependsOn(AmendChangelogTarget)
        .Description("Push formatted code and changelog.md to GitHub repo.")
        .AssuredAfterFailure()
        .Executes(async () =>
        {
            if (IsLocalBuild)
            {
                var gitRepoName = await Cli.Wrap("powershell")
                    .WithArguments(new[] { "Split-Path -Leaf (git remote get-url origin)" })
                    .ExecuteBufferedAsync();

                var repoName = gitRepoName.StandardOutput.TrimEnd();

                var gitCommand = "git";
                var gitAddArgument = @"add -A";
                var gitCommitArgument = @"commit -m ""chore(ci): checking in changed code from local ci""";
                var gitPushArgument =
                    $@"push https://{GitHubToken}@github.com/{Repository.GetGitHubOwner()}/{repoName}";

                try
                {
                    Process.Start(gitCommand, gitAddArgument).WaitForExit();
                    Process.Start(gitCommand, gitCommitArgument).WaitForExit();
                    Process.Start(gitCommand, gitPushArgument).WaitForExit();
                }

                catch (Exception ex) {   }
               }
        });

    Target BuildNupkg => _ => _
    .DependsOn(PushToGitHub)
    .Description("Creates Nuget package, using dotnet-octo")
    .AssuredAfterFailure()
    .Executes(() =>
    {
        if (IsLocalBuild)
            DotnetOcto(
                $"pack ./ --id {utilsProjectName}  --version {OctopusVersion} --outFolder {NupkgPath}  --overwrite",
                PublishFolder);

        if (IsServerBuild)
            DotnetOcto($"pack ./ --id {utilsProjectName}  --version {CloudBuildNo} --outFolder {NupkgPath}  --overwrite",
                PublishFolder);
    });

    /// <summary>
    ///     Push the package to ProGet.
    /// </summary>
    Target PushToNuGet => _ => _
        .Requires(() => NuGetApiKey != null)
        .Description("Push to ProGet")
        .AssuredAfterFailure()
        .DependsOn(BuildNupkg)
        .Executes(() =>
        {
            if (IsLocalBuild) Log.Information("This is a local build, no Nuget push.");

            if (IsServerBuild)
                NugetCli(
                    $"push {NupkgPath / $"{utilsProjectName}.{CloudBuildNo}.nupkg"} {NuGetApiKey} -src {ProgetUrl}");
        });

    /// <summary>
    ///     Publish GitHub release, for main / master releases.
    /// </summary>
    Target PublishGitHubRelease => _ => _
        .DependsOn(PushToNuGet)
        .AssuredAfterFailure()
        .Description("Create a release in GitHub")
        .Requires(() => GitHubToken)
        .OnlyWhenStatic(() => Repository.Equals("master") || Repository.Branch.Equals("main"))
        .Executes<Task>(async () =>
        {
            var releaseTag = $"v{CloudBuildNo}";
            var nuGetPackage = NupkgPath.GlobFiles("*.nupkg").First().ToString();
            GitHubCli($@"release create {releaseTag} -F {ChangeLogFile} {nuGetPackage}");
        });

    protected virtual void OnBuildFinished(string target)
    {
        Directory.Delete(NukeOut);
    }

    public string framework = "net8.0";
}
