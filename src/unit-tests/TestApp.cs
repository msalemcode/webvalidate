using System.Threading.Tasks;
using Xunit;

namespace CSE.WebValidate.Tests.Unit
{
    public class TestApp
    {
        [Fact]
        public async Task CommandArgsTest()
        {
            // no params displays usage
            Assert.Equal(1, await App.Main(null).ConfigureAwait(false));

            // test remaining valid parameters
            string[] args = new string[] { "--random", "--verbose", "--telemetry", "testApp", "testKey" };
            Assert.Equal(1, await App.Main(args).ConfigureAwait(false));

            // test bad param
            args = new string[] { "foo" };
            Assert.Equal(1, await App.Main(args).ConfigureAwait(false));

            // test bad param with good param
            args = new string[] { "-s", "froyo", "foo" };
            Assert.Equal(1, await App.Main(args).ConfigureAwait(false));
        }

        [Fact]
        public async Task ValidateAllJsonFilesTest()
        {
            // test all files
            using Config cfg = new Config
            {
                Server = "http://localhost",
                Timeout = 30,
                MaxConcurrentRequests = 100
            };

            cfg.FileList.Add("actorById.json");
            cfg.FileList.Add("bad.json");
            cfg.FileList.Add("baseline.json");
            cfg.FileList.Add("benchmark.json");
            cfg.FileList.Add("dotnet.json");
            cfg.FileList.Add("featured.json");
            cfg.FileList.Add("foreach.json");
            cfg.FileList.Add("genres.json");
            cfg.FileList.Add("java.json");
            cfg.FileList.Add("movieById.json");
            cfg.FileList.Add("moviesByActorId.json");
            cfg.FileList.Add("msft.json");
            cfg.FileList.Add("node.json");
            cfg.FileList.Add("rating.json");
            cfg.FileList.Add("search.json");
            cfg.FileList.Add("year.json");

            // load and validate all of our test files
            using var wv = new WebV(cfg);

            // file not found test
            Assert.Null(wv.ReadJson("foo"));

            // test with null config
            Assert.NotEqual(0, await wv.RunOnce(null, new System.Threading.CancellationToken()).ConfigureAwait(false));

            cfg.Dispose();
        }

        [Fact]
        public void EnvironmentVariableTest()
        {
            // set all env vars
            System.Environment.SetEnvironmentVariable(EnvKeys.Duration, "30");
            System.Environment.SetEnvironmentVariable(EnvKeys.Files, "baseline.json dotnet.json");
            System.Environment.SetEnvironmentVariable(EnvKeys.Server, "froyo");
            System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, "100");
            System.Environment.SetEnvironmentVariable(EnvKeys.Random, "false");
            System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, "30");
            System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, "false");
            System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, "1000");
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryName, "testApp");
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryKey, "testKey");
            System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, "false");

            var cmd = App.MergeEnvVarIntoCommandArgs(null);

            // validate
            Assert.Equal(23, cmd.Count);

            // clear env vars
            System.Environment.SetEnvironmentVariable(EnvKeys.Duration, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Files, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Server, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Random, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryName, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.TelemetryKey, null);
            System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, null);

            // isnullempty fails
            Assert.False(App.CheckFileExists(string.Empty));

            // isnullempty fails
            Assert.False(App.CheckFileExists("testFileNotFound"));
        }

        [Fact]
        public void VersionTest()
        {
            Assert.NotNull(CSE.WebValidate.Version.AssemblyVersion);
        }

        [Fact]
        public void CommandLineTest()
        {
            //string[] args;
            //using App app = new App();

            //// empty array is valid
            //args = Array.Empty<string>();
            //Assert.True(app.ProcessCommandArgs(args));

            //// valid
            //args = new string[] { "--maxconcurrent", "100" };
            //Assert.True(app.ProcessCommandArgs(args));

            //// valid
            //args = new string[] { "--timeout", "30" };
            //Assert.True(app.ProcessCommandArgs(args));

            //// valid
            //args = new string[] { "--help" };
            //Assert.True(app.ProcessCommandArgs(args));

            //// non-numeric
            //args = new string[] { "--sleep", "foo" };
            //Assert.False(app.ProcessCommandArgs(args));

            //// non-numeric
            //args = new string[] { "--maxconcurrent", "foo" };
            //Assert.False(app.ProcessCommandArgs(args));

            //// non-numeric
            //args = new string[] { "--timeout", "foo" };
            //Assert.False(app.ProcessCommandArgs(args));

            //// non-numeric
            //args = new string[] { "--duration", "foo" };
            //Assert.False(app.ProcessCommandArgs(args));

            //args = new string[] { "--host", "froyo", "--files", "badFileName.json" };
            //Assert.True(app.ProcessCommandArgs(args));

            //app.Dispose();
        }

        [Fact]
        public void HostOnlyCommandLineTest()
        {
            //using App app = new App();
            //string[] args = new string[] { "-s", "froyo" };

            //// valid
            //Assert.True(app.ProcessCommandArgs(args));
            //Assert.True(app.ValidateParameters());

            //Assert.Equal("https://froyo.azurewebsites.net", app.Config.Host);

            //// check default values
            //Assert.NotNull(app.Config.FileList);
            //Assert.Single(app.Config.FileList);
            //Assert.Equal("baseline.json", app.Config.FileList[0]);
            //Assert.Equal(0, app.Config.Duration);
            //Assert.Equal(100, app.Config.MaxConcurrentRequests);
            //Assert.Equal(10, app.Config.MaxErrors);
            //Assert.Null(app.Config.Metrics);
            //Assert.False(app.Config.Random);
            //Assert.Equal(30, app.Config.RequestTimeout);
            //Assert.False(app.Config.RunLoop);
            //Assert.Equal(0, app.Config.SleepMs);
            //Assert.Null(app.Config.TelemetryApp);
            //Assert.Null(app.Config.TelemetryKey);
            //Assert.True(app.Config.Verbose);

            //app.Dispose();
        }
    }
}
