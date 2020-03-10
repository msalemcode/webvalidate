using System;
using System.Threading.Tasks;
using WebValidation;
using WebValidationApp;
using Xunit;

namespace UnitTests
{
    public class TestEndToEnd
    {
        [Fact]
        public async Task FroyoTest()
        {
            using Config cfg = new Config
            {
                Host = "https://froyo.azurewebsites.net"
            };

            cfg.FileList.Add("TestFiles/dotnet.json");
            cfg.FileList.Add("TestFiles/baseline.json");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }

        [Fact]
        public async Task SherbertTest()
        {
            using Config cfg = new Config
            {
                Host = "https://sherbert.azurewebsites.net"
            };

            // not working yet - cfg.FileList.Add("TestFiles/bad.json");
            cfg.FileList.Add("TestFiles/baseline.json");
            cfg.FileList.Add("TestFiles/node.json");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }

        [Fact]
        public async Task BluebellTest()
        {
            using Config cfg = new Config
            {
                Host = "https://bluebell.azurewebsites.net"
            };

            cfg.FileList.Add("TestFiles/baseline.json");
            cfg.FileList.Add("TestFiles/dotnet.json");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }
    }
}
