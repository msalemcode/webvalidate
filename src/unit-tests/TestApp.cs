using System;
using System.Threading.Tasks;
using WebValidation;
using WebValidationApp;
using Xunit;

namespace UnitTests
{
    public class TestApp
    {
        [Fact]
        public async Task RunAppTest()
        {
            string[] args = new string[] 
            { 
                "--host", "froyo", 
                "--files", "dotnet.json", "baseline.json", "foreach.json", 
                "--sleep", "10"};

            // test RunOnce
            Assert.Equal(0, await App.Main(args).ConfigureAwait(false));

            // test RunLoop
            args = new string[] { "--host", "froyo", "--files", "benchmark.json", "TestFiles/baseline.json", "--duration", "10", "--runloop", "--sleep", "0", "--random" };
            Assert.Equal(0, await App.Main(args).ConfigureAwait(false));

        }

        [Fact]
        public async Task CommandArgsTest()
        {
            // no params displays usage
            Assert.Equal(0, await App.Main(null).ConfigureAwait(false));

            // test remaining valid parameters
            using App app = new App();
            string[] args = new string[] { "--random", "--verbose", "--telemetry", "testApp", "testKey" };
            Assert.True(app.ProcessCommandArgs(args));

            // test bad param
            args = new string[] { "foo" };
            Assert.False(app.ProcessCommandArgs(args));

            // test bad param with good param
            args = new string[] { "--host", "froyo", "foo" };
            Assert.False(app.ProcessCommandArgs(args));
            app.Dispose();
        }

        [Fact]
        public async Task ValidateAllJsonFilesTest()
        {
            // test all files
            using Config cfg = new Config
            {
                Host = "http://localhost"
            };

            cfg.FileList.Add("TestFiles/actorById.json");
            cfg.FileList.Add("TestFiles/bad.json");
            cfg.FileList.Add("TestFiles/baseline.json");
            cfg.FileList.Add("TestFiles/benchmark.json");
            cfg.FileList.Add("TestFiles/dotnet.json");
            cfg.FileList.Add("TestFiles/featured.json");
            cfg.FileList.Add("TestFiles/foreach.json");
            cfg.FileList.Add("TestFiles/genres.json");
            cfg.FileList.Add("TestFiles/java.json");
            cfg.FileList.Add("TestFiles/movieById.json");
            cfg.FileList.Add("TestFiles/moviesByActorId.json");
            cfg.FileList.Add("TestFiles/msft.json");
            cfg.FileList.Add("TestFiles/node.json");
            cfg.FileList.Add("TestFiles/rating.json");
            cfg.FileList.Add("TestFiles/search.json");
            cfg.FileList.Add("TestFiles/year.json");

            // load and validate all of our test files
            using var wv = new WebV(cfg);

            // file not found test
            Assert.Null(wv.ReadJson("foo"));

            // test with null config
            Assert.False(await wv.RunOnce(null).ConfigureAwait(false));

            cfg.Dispose();
        }

        [Fact]
        public void EnvironmentVariableTest()
        {
            // set all env vars
            System.Environment.SetEnvironmentVariable(EnvKeys.AppService, "test");
            System.Environment.SetEnvironmentVariable(EnvKeys.Duration, "30");
            System.Environment.SetEnvironmentVariable(EnvKeys.Files, "baseline.json dotnet.json");
            System.Environment.SetEnvironmentVariable(EnvKeys.Host, "froyo");
            System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, "100");
            System.Environment.SetEnvironmentVariable(EnvKeys.Random, "false");
            System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, "30");
            System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, "false");
            System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, "1000");
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryAppName, "testApp");
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryKey, "testKey");
            System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, "false");

            // validate
            using App app = new App();
            Assert.True(app.ProcessEnvironmentVariables());

            // clear env vars
            System.Environment.SetEnvironmentVariable(EnvKeys.AppService, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Duration, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Files, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Host, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Random, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryAppName, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryKey, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, null);

            // order matters
            // both telemetryapp and telemetry key must be specified
            app.Config.TelemetryApp = string.Empty;
            Assert.False(app.ValidateSharedParameters());
            
            // must be > 0
            app.Config.RequestTimeout = 0;
            Assert.False(app.ValidateSharedParameters());

            // requires RunLoop = true
            app.Config.Random = true;
            Assert.False(app.ValidateRunOnceParameters());

            // requires RunLoop = true
            app.Config.Duration = 60;
            Assert.False(app.ValidateRunOnceParameters());

            app.Config.RunLoop = true;

            // resets to 1
            app.Config.SleepMs = -1;

            // must be >= -1
            app.Config.Duration = -2;
            Assert.False(app.ValidateRunLoopParameters());

            // must be > -1
            app.Config.SleepMs = -2;
            Assert.False(app.ValidateRunLoopParameters());

            // must be > -1
            app.Config.MaxConcurrentRequests = -2;
            Assert.False(app.ValidateRunLoopParameters());

            app.Config.Duration = -2;
            Assert.False(app.ValidateRunLoopParameters());

            // null fails
            Assert.False(app.ProcessCommandArgs(null));

            // isnullempty fails
            Assert.False(App.TestFileExists(string.Empty, out string discard));

            // isnullempty fails
            Assert.False(App.TestFileExists("testFileNotFound", out discard));

            app.Dispose();
        }

        [Fact]
        public void VersionTest()
        {
            // version format is 1.0.0+MMdd.hhmm
            // will need to be updated when version number changes
            Assert.NotNull(WebValidationApp.Version.AssemblyVersion);
            Assert.StartsWith("1.0.0+", WebValidationApp.Version.AssemblyVersion, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void CommandLineTest()
        {
            string[] args;
            using App app = new App();

            // empty array is valid
            args = Array.Empty<string>();
            Assert.True(app.ProcessCommandArgs(args));

            // valid
            args = new string[] { "--maxconcurrent", "100" };
            Assert.True(app.ProcessCommandArgs(args));

            // valid
            args = new string[] { "--timeout", "30" };
            Assert.True(app.ProcessCommandArgs(args));

            // valid
            args = new string[] { "--help" };
            Assert.True(app.ProcessCommandArgs(args));

            // non-numeric
            args = new string[] { "--sleep", "foo" };
            Assert.False(app.ProcessCommandArgs(args));

            // non-numeric
            args = new string[] { "--maxconcurrent", "foo" };
            Assert.False(app.ProcessCommandArgs(args));

            // non-numeric
            args = new string[] { "--timeout", "foo" };
            Assert.False(app.ProcessCommandArgs(args));

            // non-numeric
            args = new string[] { "--duration", "foo" };
            Assert.False(app.ProcessCommandArgs(args));

            args = new string[] { "--host", "froyo", "--files", "badFileName.json" };
            Assert.True(app.ProcessCommandArgs(args));

            // file not found
            Assert.False(app.ValidateParameters());

            app.Dispose();
        }

        [Fact]
        public void HostOnlyCommandLineTest()
        {
            using App app = new App();
            string[] args = new string[] { "--host", "froyo" };

            // valid
            Assert.True(app.ProcessCommandArgs(args));
            Assert.True(app.ValidateParameters());

            Assert.Equal("https://froyo.azurewebsites.net", app.Config.Host);

            // check default values
            Assert.NotNull(app.Config.FileList);
            Assert.Single(app.Config.FileList);
            Assert.Equal("TestFiles/baseline.json", app.Config.FileList[0]);
            Assert.Equal(0, app.Config.Duration);
            Assert.Equal(100, app.Config.MaxConcurrentRequests);
            Assert.Equal(10, app.Config.MaxErrors);
            Assert.Null(app.Config.Metrics);
            Assert.False(app.Config.Random);
            Assert.Equal(30, app.Config.RequestTimeout);
            Assert.False(app.Config.RunLoop);
            Assert.Equal(0, app.Config.SleepMs);
            Assert.Null(app.Config.TelemetryApp);
            Assert.Null(app.Config.TelemetryKey);
            Assert.True(app.Config.Verbose);

            app.Dispose();
        }
    }
}
