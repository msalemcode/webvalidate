using System;
using WebValidation;
using WebValidationApp;
using Xunit;

namespace UnitTests
{
    public class TestApp
    {
        [Fact]
        public void NoCommandLine()
        {
            using App app = new App();
            string[] args = Array.Empty<string>();

            Assert.True(app.ProcessEnvironmentVariables());
            Assert.True(app.ProcessCommandArgs(args));
        }

        [Fact]
        public void Version()
        {
            Assert.NotNull(WebValidationApp.Version.AssemblyVersion);
            Assert.StartsWith("0.5.", WebValidationApp.Version.AssemblyVersion, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void RunOnce()
        {
            string[] args = new string[] { "--host", "froyo" };

            Assert.Equal(0, App.Main(args));
        }

        [Fact]
        public void HostOnlyCommandLine()
        {
            using App app = new App();

            string[] args = new string[] { "--host", "froyo" };

            Assert.True(app.ProcessCommandArgs(args));
            Assert.True(app.ValidateParameters());

            Assert.Equal("https://froyo.azurewebsites.net", app.Config.Host);
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
        }

        [Fact]
        public void ThreeFilesCommandLine()
        {
            using App app = new App();

            string[] args = new string[] { "--host", "froyo", "--files", "dotnet.json", "genres.json", "year.json" };

            Assert.True(app.ProcessCommandArgs(args));
            Assert.True(app.ValidateParameters());

            Assert.Equal("https://froyo.azurewebsites.net", app.Config.Host);
            Assert.NotNull(app.Config.FileList);
            Assert.Equal(3, app.Config.FileList.Count);
            Assert.Equal("TestFiles/dotnet.json", app.Config.FileList[0]);
            Assert.Equal("TestFiles/genres.json", app.Config.FileList[1]);
            Assert.Equal("TestFiles/year.json", app.Config.FileList[2]);
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
        }

        [Fact]
        public void BadFileCommandLine()
        {
            using App app = new App();

            string[] args = new string[] { "--host", "froyo", "--files", "badFileName.json" };

            Assert.True(app.ProcessCommandArgs(args));
            Assert.False(app.ValidateParameters());
        }
    }
}
