using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Client.FileCacheStore;
using CacheCow.Client.Headers;
using Serilog;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests
{
    public class TestCacheCow : SuperTest
    {
        public TestCacheCow(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void TestCache()
        {
            Log("Setting up");
            var store = new FileStoreLocal("cache");

            var _client = store.CreateClient();

            _client.DefaultRequestHeaders.Add("user-agent",
                "Itinero-Transit-dev/0.0.2 (anyways.eu; pieter@anyways.eu)");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            _client.Timeout = TimeSpan.FromMilliseconds(5000);

            var uri = new Uri("https://graph.irail.be/sncb/connections");
            var response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();

            var cacheHit = response.Headers.GetCacheCowHeader() != null &&
                           response.Headers.GetCacheCowHeader().ToString().Contains("did-not-exist=false");
            Log(response.Headers?.GetCacheCowHeader()?.ToString());

            response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            Log(response.Headers.GetCacheCowHeader()?.ToString());

            cacheHit = response.Headers.GetCacheCowHeader() != null &&
                       response.Headers.GetCacheCowHeader().ToString().Contains("did-not-exist=false");
            Assert.True(cacheHit);

            // The cache timeout was 6 second at the time of writing the test
            // So we let this cache expire
            Thread.Sleep(20000);
            response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();

            Log(response.Headers.GetCacheCowHeader()?.ToString());

            Assert.True(
                    response.Headers.GetCacheCowHeader().ToString().Contains("was-stale=true"));
        }


        [Fact]
        public async Task Test404()
        {
            Log("Setting up");
            var store = new FileStoreLocal("cache");
            var _client = new HttpClient(); // store.CreateClient());

            _client.DefaultRequestHeaders.Add("user-agent",
                "Itinero-Transit-dev/0.0.2 (anyways.eu; pieter@anyways.eu)");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            _client.Timeout = TimeSpan.FromMilliseconds(5000);

            var uri = new Uri(
                "https://irail.be/non-existing-location-to-test-my-error-messages-Do-not-create-plz-my-test-will-fail");
            var response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            Log("" + response.StatusCode);

            Assert.Equal("NotFound", "" + response.StatusCode);
        }


        [Fact]
        public async Task Test404Cached()
        {
            Log("Setting up");
            var store = new FileStoreLocal("cache");
            var _client = store.CreateClient();

            _client.DefaultRequestHeaders.Add("user-agent",
                "Itinero-Transit-dev/0.0.2 (anyways.eu; pieter@anyways.eu)");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            _client.Timeout = TimeSpan.FromMilliseconds(5000);

            var uri = new Uri(
                "https://irail.be/non-existing-location-to-test-my-error-messages-Do-not-create-plz-my-test-will-fail");
            var response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            Log("" + response.StatusCode);

            Assert.Equal("NotFound", "" + response.StatusCode);
        }


        [Fact]
        public void TestNotFound()
        {
            Log("Setting up");
            var store = new FileStoreLocal("cache");
            var _client = store.CreateClient();

            _client.DefaultRequestHeaders.Add("user-agent",
                "Itinero-Transit-dev/0.0.2 (anyways.eu; pieter@anyways.eu)");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            _client.Timeout = TimeSpan.FromMilliseconds(5000);

            // Yep
            // I just smashed mah keyboard
            var uri = new Uri("https://www.qmlsdkjfqmlskdjf.mqlskdjfqmlskdjf/");

            try
            {
                var response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
                var data = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (HttpRequestException e)
            {
                Assert.True(e.Message.StartsWith("No such device or address"));
                return;
            }

            throw new Exception("This should have failed...");
        }
    }
}