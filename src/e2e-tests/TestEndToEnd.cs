using System.Threading.Tasks;
using Xunit;

namespace CSE.WebValidate.Tests.EndToEnd
{
    public class TestEndToEnd
    {
        Config BuildConfig(string server)
        {
            Config cfg = new Config
            {
                Server = "https://" + server + ".azurewebsites.net"
            };
            cfg.Timeout = 10;
            cfg.MaxConcurrentRequests = 100;
            cfg.MaxErrors = 10;

            cfg.FileList.Add("baseline.json");

            // TODO - temporarily remove as we change bluebell and sherbert
//            cfg.FileList.Add("bad.json");

            return cfg;
        }

        // TODO - uncomment once gelato is reliable

        //[Fact]
        //public async Task GelatoTest()
        //{
        //    using Config cfg = BuildConfig("gelato");

        //    // load and validate all of our test files

        //    using var wv = new WebV(cfg);
        //    Assert.Equal(0, await wv.RunOnce(cfg).ConfigureAwait(false));
        //}

        [Fact]
        public async Task SherbertTest()
        {
            using Config cfg = BuildConfig("sherbert");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.Equal(0, await wv.RunOnce(cfg, new System.Threading.CancellationToken()).ConfigureAwait(false));
        }

        [Fact]
        public async Task BluebellTest()
        {
            using Config cfg = BuildConfig("bluebell");

            // load and validate all of our test files
            using var wv = new WebV(cfg);
            Assert.Equal(0, await wv.RunOnce(cfg, new System.Threading.CancellationToken()).ConfigureAwait(false));
        }
    }
}
