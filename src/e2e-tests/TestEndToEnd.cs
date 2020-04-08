using System.Threading.Tasks;
using WebValidation;
using Xunit;

namespace UnitTests
{
    public class TestEndToEnd
    {
        Config BuildConfig(string server)
        {
            Config cfg = new Config
            {
                Server = "https://" + server + ".azurewebsites.net"
            };
            cfg.Timeout = 30;
            cfg.MaxConcurrentRequests = 100;
            cfg.MaxErrors = 10;

            cfg.FileList.Add("dotnet.json");
            cfg.FileList.Add("baseline.json");
            cfg.FileList.Add("bad.json");

            return cfg;
        }

        [Fact]
        public async Task FroyoTest()
        {
            using Config cfg = BuildConfig("froyo");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }

        [Fact]
        public async Task SherbertTest()
        {
            using Config cfg = BuildConfig("sherbert");
            cfg.FileList.RemoveAt(0);

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }

        [Fact]
        public async Task BluebellTest()
        {
            using Config cfg = BuildConfig("bluebell");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.True(await wv.RunOnce(cfg).ConfigureAwait(false));
        }
    }
}
