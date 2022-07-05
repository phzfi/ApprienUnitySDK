#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Apprien;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

using NSubstitute;

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
    public class ApprienManagerTests
    {
        private ApprienManager _apprienManager;
        private IApprienBackendConnection _backend;

        private string _defaultIAPid;
        private List<string> _testIAPids;
        private Dictionary<string, ApprienProduct> _productsLookup;

        private string _gamePackageName;
        private string _token;
        private string _apprienIdentifier;
        private string _storeIdentifier;

        private ProductCatalog _catalog;
        private ConfigurationBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _gamePackageName = "dummy.package.name";
            _token = "dummy-token";
            _apprienIdentifier = "FF"; // One byte as a hex string
            _storeIdentifier = "google";

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

            _backend = Substitute.For<IApprienBackendConnection>();

            _apprienManager = new ApprienManager(_backend);

            // Backend mocks
            _backend.GamePackageName.Returns(_gamePackageName);
            _backend.Token.Returns(_token);
            _backend.ApprienIdentifier.Returns(_apprienIdentifier);
            _backend.StoreIdentifier.Returns(_storeIdentifier);

            // Mock the fetch of all prices
            var pricesFetchMock = Substitute.For<IEnumerator<ApprienFetchPricesResponse>>();
            pricesFetchMock.MoveNext().ReturnsForAnyArgs(false);
            pricesFetchMock.Current.Returns(new ApprienFetchPricesResponse()
            {
                Success = true,
                JSON = JsonUtility.ToJson(new ApprienProductList
                {
                    products = _testIAPids.Select(id => new ApprienProductListProduct
                    {
                        @base = id,
                        variant = $"{id}-variant"
                    }).ToList()
                })
            });

            _backend.FetchApprienPrices(Arg.Is<IUnityWebRequest>(
                r => r.GetRequestHeader("Authorization") == $"Bearer {_token}" &&
                     r.url.Equals(string.Format(ApprienManager.REST_GET_ALL_PRICES_URL, _storeIdentifier, _gamePackageName))
            )).Returns(pricesFetchMock);

            // Mock the fetch of single price
            foreach (var id in _testIAPids)
            {
                var priceFetchMock = Substitute.For<IEnumerator<ApprienFetchPriceResponse>>();
                priceFetchMock.MoveNext().ReturnsForAnyArgs(false);
                priceFetchMock.Current.Returns(new ApprienFetchPriceResponse()
                {
                    Success = true,
                    VariantId = $"{id}-variant"
                });

                _backend.FetchApprienPrice(Arg.Is<IUnityWebRequest>(
                    r => r.GetRequestHeader("Authorization") == $"Bearer {_token}" &&
                         r.url.Equals(string.Format(ApprienManager.REST_GET_PRICE_URL, _storeIdentifier, _gamePackageName, id))
                )).Returns(priceFetchMock);
            }
        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void ProductVariantIdShouldDefaultToBaseId()
        {
            Assert.AreEqual(_defaultIAPid, GetProduct(_defaultIAPid).ApprienVariantIAPId);
        }

        [Test]
        public void GettingBaseIapIdShouldReturnCorrectId()
        {
            var iapId = "z_base_iap_id.apprien_500_dfa3";
            var expected = "base_iap_id";

            Assert.AreEqual(expected, ApprienUtility.GetBaseIAPId(iapId));

            iapId = "z_loadout_bordkanone_37.apprien_1099_2wkh";
            expected = "loadout_bordkanone_37";

            Assert.AreEqual(expected, ApprienUtility.GetBaseIAPId(iapId));
        }

        [Test]
        public void CreatingProductsFromCatalogShouldWork()
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
        public void CreatingProductsFromConfigurationBuilderShouldWork()
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

        [UnityTest]
        public IEnumerator FetchingManyProductsShouldSucceed()
        {
            var products = new ApprienProduct[] { GetProduct(0), GetProduct(1), GetProduct(2) };

            var fetch = _apprienManager.FetchApprienPrices(products);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual($"{_testIAPids[i]}-variant", products[i].ApprienVariantIAPId);
            }
        }

        [UnityTest]
        public IEnumerator FetchingOneProductShouldSucceed()
        {
            var product = GetProduct(0);

            var fetch = _apprienManager.FetchApprienPrice(product);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual($"{_testIAPids[0]}-variant", product.ApprienVariantIAPId);

            // Fetch a second product
            product = GetProduct(1);

            fetch = _apprienManager.FetchApprienPrice(product);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual($"{_testIAPids[1]}-variant", product.ApprienVariantIAPId);
        }

        // NOTE: failure will default to the original base iap id
        [UnityTest]
        public IEnumerator FetchingProductsWithBadTokenShouldReturnBaseIAPId()
        {
            _backend.Token.Returns("another-dummy-token");

            var product = GetProduct(0);
            var fetch = _apprienManager.FetchApprienPrice(product);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_testIAPids[0], product.ApprienVariantIAPId);
        }

        [UnityTest]
        public IEnumerator FetchingNonVariantProductShouldReturnBaseIAPId()
        {
            // IAP without a variant
            var product = GetProduct();
            var fetch = _apprienManager.FetchApprienPrice(product);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(_defaultIAPid, product.ApprienVariantIAPId);
        }

        [UnityTest]
        public IEnumerator FetchingPricesNetworkErrorsShouldReturnBaseIAPId()
        {
            var products = new ApprienProduct[] { GetProduct(0), GetProduct(1), GetProduct(2) };

            var pricesFetchMock = Substitute.For<IEnumerator<ApprienFetchPricesResponse>>();
            pricesFetchMock.MoveNext().ReturnsForAnyArgs(false);
            pricesFetchMock.Current.Returns(new ApprienFetchPricesResponse()
            {
                Success = false,
                JSON = "contents are irrelevant"
            });

            _backend.FetchApprienPrices(Arg.Is<IUnityWebRequest>(
                r => r.GetRequestHeader("Authorization") == $"Bearer {_token}" &&
                     r.url.Equals(string.Format(ApprienManager.REST_GET_ALL_PRICES_URL, _storeIdentifier, _gamePackageName))
            )).Returns(pricesFetchMock);

            var fetch = _apprienManager.FetchApprienPrices(products);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < products.Count(); i++)
            {
                Assert.AreEqual(products[i].BaseIAPId, products[i].ApprienVariantIAPId);
            }
        }

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
