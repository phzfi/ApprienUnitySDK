// Enable .NET 4.x in Player settings to enable more unit tests of Apprien SDK
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Apprien;

#if NET_4_6 || NET_STANDARD_2_0
using Mock4Net.Core;
#endif

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;
using UnityEngine.Purchasing.Extension;
using System.Collections.ObjectModel;
using System;

namespace ApprienUnitySDK.ExampleProject.Tests
{
    public class DummyStore : IStore
    {
        private IStoreCallback _biller;

        public void Initialize(IStoreCallback callback)
        {
            _biller = callback;
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            var products = new List<ProductDescription>();
            foreach (var product in productDefinitions)
            {
                var metadata = new ProductMetadata("$123.45", "Fake title for " + product.id, "Fake description", "USD", 123.45m);
                products.Add(new ProductDescription(product.storeSpecificId, metadata));
            }
            _biller.OnProductsRetrieved(products);
        }

        public void FinishTransaction(ProductDefinition product, string transactionId)
        {
            throw new System.NotImplementedException();
        }

        public void Purchase(ProductDefinition product, string developerPayload)
        {
            throw new System.NotImplementedException();
        }
    }
    public class DummyPurchasingModule : IPurchasingModule
    {
        public void Configure(IPurchasingBinder binder)
        {
            binder.RegisterStore("DummyStore", InstantiateDummyStore());
            // Our Purchasing service implementation provides the real implementation.
            //binder.RegisterExtension<IStoreExtension>(new FakeManufacturerExtensions());
        }

        public IStore InstantiateDummyStore()
        {
            return new DummyStore();
        }

        public IStoreExtension IManufacturerExtensions() { return null; }
    }

    [TestFixture]
    public class ApprienSDKTest
    {
        private ApprienManager _apprienManager;

        private string _defaultIAPid;
        private List<string> _testIAPids;
        private Dictionary<string, ApprienProduct> _productsLookup;

        private string _gamePackageName;
        private string _token;

        private ProductCatalog _catalog;
        private ConfigurationBuilder _builder;

#if NET_4_6 || NET_STANDARD_2_0
        private FluentMockServer _mockServer;
#endif

        [SetUp]
        public void SetUp()
        {
            _gamePackageName = "dummy.package.name";
            _token = "dummy-token";

            // Setup products for testing
            _defaultIAPid = "test-default-id";

            _testIAPids = new List<string>()
            {
                "test-1-id",
                "test-2-id",
                "test-3-id",
                "test-4-id",
                "test-5-id"
            };

            // Setup product lookup
            _productsLookup = new Dictionary<string, ApprienProduct>();
            _productsLookup[_defaultIAPid] = new ApprienProduct(_defaultIAPid, ProductType.Consumable);

            // Add IAP ids to the catalog
            _catalog = new ProductCatalog();
            _catalog.Add(new ProductCatalogItem() { id = _defaultIAPid, type = ProductType.Consumable });

            // Create UnityPurchasing Products
            _builder = ConfigurationBuilder.Instance(new DummyPurchasingModule());
            _builder.AddProduct(_defaultIAPid, ProductType.Consumable);

            foreach (var id in _testIAPids)
            {
                _productsLookup[id] = new ApprienProduct(id, ProductType.Consumable);
                _catalog.Add(new ProductCatalogItem() { id = id, type = ProductType.Consumable });
                _builder.AddProduct(id, ProductType.Consumable);
            }

#if NET_4_6 || NET_STANDARD_2_0
            _mockServer = FluentMockServer.Start();
#endif
            _apprienManager = new ApprienManager(
                _gamePackageName,
                ApprienIntegrationType.GooglePlayStore,
                _token
            );
        }
#if NET_4_6 || NET_STANDARD_2_0
        private void SetupMockServer(float delaySeconds = 0f)
        {
            // Setup the mock server routes
            // Mock4Net is not very flexible, so we have to do some magic
            // Possible to change the mocking server to https://github.com/WireMock-Net/WireMock.Net
            // which also requires .NET 4.x

            // Unity Test Runner can't send proper web requests so we have to mock the server.

            foreach (var id in _testIAPids)
            {
                // If the API changes, update the mock server responses
                // https://github.com/alexvictoor/mock4net
                _mockServer.Given(Requests.WithUrl(
                        string.Format(
                            "/api/v1/stores/{0}/games/{1}/products/{2}/prices",
                            "google",
                            _gamePackageName,
                            id
                        )
                    ).UsingGet().WithHeader("Authorization", "Bearer " + _token))
                    .RespondWith(
                        Responses
                        .WithStatusCode(200)
                        .WithBody(id + "-variant")
                        .AfterDelay(TimeSpan.FromSeconds(delaySeconds))
                    );
            }

            _mockServer.Given(Requests.WithUrl(
                    string.Format(
                        "/api/v1/stores/{0}/games/{1}/products/{2}/prices",
                        "google",
                        _gamePackageName,
                        _defaultIAPid
                    )
                ).UsingGet().WithHeader("Authorization", "Bearer " + _token))
                .RespondWith(
                    Responses
                    .WithStatusCode(200)
                    .WithBody(_defaultIAPid)
                    .AfterDelay(TimeSpan.FromSeconds(delaySeconds))
                );

            _mockServer.Given(Requests.WithUrl(
                    string.Format(
                        "/api/v1/stores/{0}/games/{1}/prices",
                        "google",
                        _gamePackageName
                    )
                ).UsingGet().WithHeader("Authorization", "Bearer " + _token))
                .RespondWith(
                    Responses
                    .WithStatusCode(200)
                    .WithBody(JsonUtility.ToJson(new ApprienProductList
                    {
                        products = new List<ApprienProductListProduct>
                        {
                            new ApprienProductListProduct { @base = "test-1-id", variant = "test-1-id-variant" },
                            new ApprienProductListProduct { @base = "test-2-id", variant = "test-2-id-variant" },
                            new ApprienProductListProduct { @base = "test-3-id", variant = "test-3-id-variant" },
                            new ApprienProductListProduct { @base = "test-4-id", variant = "test-4-id-variant" },
                            new ApprienProductListProduct { @base = "test-5-id", variant = "test-5-id-variant" }
                        }
                    }))
                    .AfterDelay(TimeSpan.FromSeconds(delaySeconds))
                );

            _mockServer.Given(Requests.WithUrl("/error")
                .UsingPost())
                .RespondWith(
                    Responses
                    .WithStatusCode(200)
                    .AfterDelay(TimeSpan.FromSeconds(delaySeconds))
                );

            // No auth token
            _mockServer.Given(Requests.WithUrl("/api/v1/*").UsingGet())
                .RespondWith(
                    Responses
                    .WithStatusCode(403)
                    .AfterDelay(TimeSpan.FromSeconds(delaySeconds))
                );

            // Assign the URL for mocking Apprien
            ApprienUtility.REST_GET_ALL_PRICES_URL = "http://localhost:" + _mockServer.Port + "/api/v1/stores/{0}/games/{1}/prices";
            ApprienUtility.REST_GET_PRICE_URL = "http://localhost:" + _mockServer.Port + "/api/v1/stores/{0}/games/{1}/products/{2}/prices";
            ApprienUtility.REST_POST_ERROR_URL = "http://localhost:" + _mockServer.Port + "/error";
        }
#endif

        [TearDown]
        public void TearDown()
        {
#if NET_4_6 || NET_STANDARD_2_0
            _mockServer.Stop();
#endif
        }

        /*
         * Unit tests. Prefixed with underscore to sort them to the top in the Test Runner window
         */

        [Test]
        public void _ProductVariantIdShouldDefaultToBaseId()
        {
            Assert.AreEqual(_defaultIAPid, GetProduct(_defaultIAPid).ApprienVariantIAPId);
        }

        [Test]
        public void _GettingBaseIapIdShouldReturnCorrectId()
        {
            var iapId = "z_base_iap_id.apprien_500_dfa3";
            var expected = "base_iap_id";

            Assert.AreEqual(expected, ApprienUtility.GetBaseIAPId(iapId));

            iapId = "z_loadout_bordkanone_37.apprien_1099_2wkh";
            expected = "loadout_bordkanone_37";

            Assert.AreEqual(expected, ApprienUtility.GetBaseIAPId(iapId));
        }

        [Test]
        public void _CreatingProductsFromCatalogShouldWork()
        {
            var products = ApprienProduct.FromIAPCatalog(_catalog);
            var productIds = products.Select(item => item.BaseIAPId).ToList();
            Assert.AreEqual(6, products.Length);
            Assert.Contains(_defaultIAPid, productIds);
            foreach (var id in _testIAPids)
            {
                Assert.Contains(id, productIds);
            }
        }

        [Test]
        public void _CreatingProductsFromConfigurationBuilderShouldWork()
        {
            var products = ApprienProduct.FromConfigurationBuilder(_builder);
            var productIds = products.Select(item => item.BaseIAPId).ToList();
            Assert.AreEqual(6, products.Length);
            Assert.Contains(_defaultIAPid, productIds);
            foreach (var id in _testIAPids)
            {
                Assert.Contains(id, productIds);
            }
        }
#if NET_4_6 || NET_STANDARD_2_0
        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingManyProductsShouldSucceed()
        {
            SetupMockServer();

            var products = new ApprienProduct[] { GetProduct(0), GetProduct(1), GetProduct(2) };

            var fetch = _apprienManager.FetchApprienPrices(products, () => { });

            while (fetch.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(_testIAPids[i] + "-variant", products[i].ApprienVariantIAPId);
            }
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingOneProductShouldSucceed()
        {
            SetupMockServer();

            var product = GetProduct(0);

            var fetch = _apprienManager.FetchApprienPrice(product, () => { });
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_testIAPids[0] + "-variant", product.ApprienVariantIAPId);
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingProductsWithBadURLShouldFail()
        {
            SetupMockServer();

            // Bad URL, v0
            ApprienUtility.REST_GET_PRICE_URL = "http://localhost:" + _mockServer.Port + "/api/v0/stores/google/games/{0}/products/{1}/prices";

            var product = GetProduct(0);
            var fetch = _apprienManager.FetchApprienPrice(product, () => { });
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_testIAPids[0], product.ApprienVariantIAPId);
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingProductsWithBadTokenShouldNotFetchVariants()
        {
            SetupMockServer();

            _apprienManager.Token = "another-dummy-token";

            var product = GetProduct(0);
            var fetch = _apprienManager.FetchApprienPrice(product, () => { });
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_testIAPids[0], product.ApprienVariantIAPId);
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingNonVariantProductShouldReturnBaseIAPId()
        {
            SetupMockServer();

            // IAP without a variant
            var product = GetProduct();
            var fetch = _apprienManager.FetchApprienPrice(product, () => { });
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_defaultIAPid, product.ApprienVariantIAPId);
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingProductsWithDelayShouldSucceed()
        {
            // Configure the SDK timeout to 5 second, but make the request take 0.5 seconds
            // Variant products should be fetched
            _apprienManager.RequestTimeout = 5f;
            SetupMockServer(0.5f);

            var products = new ApprienProduct[] { GetProduct(0), GetProduct(1), GetProduct(2) };

            var fetch = _apprienManager.FetchApprienPrices(products, () => { });

            while (fetch.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(_testIAPids[i] + "-variant", products[i].ApprienVariantIAPId);
            }
        }

        [UnityTest, Timeout(2000)]
        public IEnumerator FetchingProductsWithLongDelayShouldSucceed()
        {
            // Configure the SDK timeout to 0.1 second, but make the request take 0.5 seconds
            // Non-variant products should be fetched
            _apprienManager.RequestTimeout = 0.1f;
            SetupMockServer(0.5f);

            var products = new ApprienProduct[] { GetProduct(0), GetProduct(1), GetProduct(2) };

            var fetch = _apprienManager.FetchApprienPrices(products, () => { });

            while (fetch.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(_testIAPids[i], products[i].ApprienVariantIAPId);
            }
        }
#endif

        // Test helpers

        private ApprienProduct GetProduct(string iapId = null)
        {
            if (iapId == null)
            {
                iapId = _defaultIAPid;
            }

            return _productsLookup[iapId];
        }

        private ApprienProduct GetProduct(int index)
        {
            index = Mathf.Clamp(index, 0, _testIAPids.Count - 1);
            return GetProduct(_testIAPids[index]);
        }
    }
}
#endif