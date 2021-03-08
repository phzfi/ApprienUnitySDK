using System;
using System.Collections.Generic;
using Apprien;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

// Partial code from https://unity3d.com/learn/tutorials/topics/ads-analytics/integrating-unity-iap-your-game
namespace ApprienUnitySDK.ExampleProject
{
    public class ExampleStoreUIController : MonoBehaviour, IStoreListener
    {
        // Internal type used for the demo to distinguish between IAP and subscription view
        private enum CanvasType
        {
            IAPs,
            Subscriptions
        }

        private ApprienManager _apprienManager;

        [Space]
        [Header("References")]
        [SerializeField] private ApprienConnection ApprienConnection;

        [Space]
        [SerializeField] private Text[] StandardPriceTexts;
        [SerializeField] private Text[] StandardPriceSKUTexts;
        [SerializeField] private Text[] ApprienPriceTexts;
        [SerializeField] private Text[] ApprienPriceSKUTexts;

        [Space]
        [SerializeField] private GameObject _IAPCanvas;
        [SerializeField] private GameObject _subscriptionCanvas;

        [Space]
        [Header("Debug")]
        [SerializeField][Range(0, 10)] private float fakeLoadingTime = 2f;
        [SerializeField] private ExampleStoreDebug_UI _exampleStoreDebugController_UI;
        [SerializeField] private ExampleStoreOfflineProductController _exampleStoreOfflineProductController;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        private ConfigurationBuilder _builder;
        private ApprienProduct[] _apprienProducts;
        private CanvasType _currentType = CanvasType.IAPs;

        private void Awake()
        {
            if (ApprienConnection == null || ApprienConnection.Token.Length == 0)
            {
                Debug.LogWarning("Token not provided for Apprien SDK. Unable to configure dynamic prices.");
            }
            InitializeProducts();
        }

        private void InitializeProducts()
        {
            // Create ApprienProducts from the IAP or subscription Catalog
            var catalogFile = Resources.Load<TextAsset>(_currentType == CanvasType.IAPs ? "ApprienIAPProductCatalog" : "ApprienSubscriptionProductCatalog");
            var catalog = ProductCatalog.FromTextAsset(catalogFile);
            _apprienProducts = ApprienProduct.FromIAPCatalog(catalog);

            // Initialize Unity IAP configuration builder
            _builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Platform specific integration type for the manager.
            ApprienIntegrationType integrationType;

#if UNITY_IOS
			integrationType = ApprienIntegrationType.AppleAppStore;
#else
            integrationType = ApprienIntegrationType.GooglePlayStore;
#endif

            // Package name. Usually Application.identifier
            var packageName = Application.identifier;

            _apprienManager = new ApprienManager(
                Application.identifier,
                integrationType,
                ApprienConnection.Token
            );

            _exampleStoreDebugController_UI.DebugApprienStatus("CHECKING APPRIEN STATUS...", Color.black);

            // Test the connection. Optional
            TestApprienConnection(
                () =>
                {
                    _exampleStoreDebugController_UI.DebugApprienStatus("CONNECTION: SUCCESS", Color.green);

                },
                () =>
                {
                    _exampleStoreDebugController_UI.DebugApprienStatus("CONNECTION: FAILED", Color.red);
                });

             // Add standard IAP ids, so that there is always a fallback if Apprien variants cannot be fetched
            foreach (var product in _apprienProducts)
            {
                _builder.AddProduct(product.BaseIAPId, product.ProductType);
            }

            FetchPrices();
        }

        private void TestApprienConnection(Action OnSuccess, Action OnFailed)
        {
            StartCoroutine(
                _apprienManager.TestConnection(
                    (connected, valid) =>
                    {
                        if (connected && valid)
                        {
                            OnSuccess();
                        }
                        else
                        {
                            OnFailed();
                        }
                    }));
        }

        /// <summary>
        /// Fetch Apprien variant IAP ids and re-initialize UnityPurchasing
        /// </summary>
        private void FetchPrices()
        {
            TestApprienConnection(() =>
            {
                // Update the products with Apprien IAP ids
                StartCoroutine(
                    _apprienManager.FetchApprienPrices(
                        _apprienProducts,
                        () =>
                        {
                            // The products will now contain updated IAP ids. Add them to the product builder
                            // If the connection failed or the variants were not fetched for some reason
                            // this will add duplicates to the builder, which will ignore them safely.
                            foreach (var product in _apprienProducts)
                            {
                                // Apprien variant IAP id. If connection failed, the variant IAP id
                                // defaults to the base IAP id
                                _builder.AddProduct(product.ApprienVariantIAPId, product.ProductType);
                            }

                            // Initialize UnityPurchasing with the fetched IAP ids
                            UnityPurchasing.Initialize(this, _builder);

                            // Update standard IAP ids on the UI
                            for (var i = 0; i < _apprienProducts.Length; i++)
                            {
                                StandardPriceSKUTexts[i].text = _apprienProducts[i].BaseIAPId;
                            }
                        }
                    )
                );
            },
            () =>
            {
                SetDefaultOfflineProducts();
            });
        }

        /// <summary>
        /// Set default products name id's and prices.
        /// <para> Can be modified from "ExampleStoreOfflineProductController".</para>
        /// <para> Example usage: During network connection failure.</para>
        /// </summary>
        public void SetDefaultOfflineProducts()
        {
            StartCoroutine(ISetDefaultProducts());
        }

        private System.Collections.IEnumerator ISetDefaultProducts()
        {
            var defaultStandardIAPs = _exampleStoreOfflineProductController.StandardIAPs;
            var defaultStandardSubscriptions = _exampleStoreOfflineProductController.StandardSubscriptions;
            var defaultApprienIAPs = _exampleStoreOfflineProductController.ApprienIAPs;
            var defaultApprienSubscriptions = _exampleStoreOfflineProductController.ApprienSubscriptions;

            yield return new WaitForSeconds(fakeLoadingTime);

            switch (_currentType)
            {
                case CanvasType.IAPs:

                    for (var i = 0; i < StandardPriceSKUTexts.Length; i++)
                    {
                        StandardPriceSKUTexts[i].text = defaultStandardIAPs[i].NameID;
                        StandardPriceTexts[i].text = defaultStandardIAPs[i].Price;
                    }

                    for (var i = 0; i < ApprienPriceSKUTexts.Length; i++)
                    {
                        ApprienPriceSKUTexts[i].text = defaultApprienIAPs[i].NameID;
                        ApprienPriceTexts[i].text = defaultApprienIAPs[i].Price;
                    }

                    break;

                case CanvasType.Subscriptions:

                    for (var i = 0; i < StandardPriceSKUTexts.Length; i++)
                    {
                        StandardPriceSKUTexts[i].text = defaultStandardSubscriptions[i].NameID;
                        StandardPriceTexts[i].text = defaultStandardSubscriptions[i].Price;
                    }

                    for (var i = 0; i < ApprienPriceSKUTexts.Length; i++)
                    {
                        ApprienPriceSKUTexts[i].text = defaultApprienSubscriptions[i].NameID;
                        ApprienPriceTexts[i].text = defaultApprienSubscriptions[i].Price;
                    }

                    break;

                default:

                    break;
            }
        }

        /// <summary>
        /// Called after Apprien IAP ids have replaced the base IAP ids, refreshes the prices of products
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="extensions"></param>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Overall Purchasing system, configured with products for this application.
            _storeController = controller;

            // Store specific subsystem, for accessing device-specific store features.
            _extensionProvider = extensions;

            // Refresh prices to UI
            RefreshUI();
        }

        /// <summary>
        /// Called when Unity Purchasing encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.Log("Initialize failed");
        }

        /// <summary>
        /// Called when a purchase completes.
        /// 
        /// May be called at any time after OnInitialized().
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            Debug.LogError("ProcessPurchase not implemented.");
            return PurchaseProcessingResult.Pending;
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        public void OnPurchaseFailed(UnityEngine.Purchasing.Product i, PurchaseFailureReason p)
        {
            Debug.Log("OnPurchaseFailed not implemented.");
        }

        /// <summary>
        /// Get the price for the given IAP from the Store
        /// </summary>
        public void RefreshUI()
        {
            TestApprienConnection(
                () =>
                {
                    var iapProducts = _storeController.products;
                    for (var i = 0; i < _apprienProducts.Length; i++)
                    {
                        var apprienProduct = _apprienProducts[i];
                        var iapApprienProduct = iapProducts.WithID(apprienProduct.ApprienVariantIAPId);
                        var iapStandardProduct = iapProducts.WithID(apprienProduct.BaseIAPId);

                        var apprienPrice = iapApprienProduct.metadata.localizedPriceString;
                        var standardPrice = iapStandardProduct.metadata.localizedPriceString;

                        StandardPriceTexts[i].text = standardPrice;
                        ApprienPriceTexts[i].text = apprienPrice;

                        // Update the Apprien IAP ids to text
                        ApprienPriceSKUTexts[i].text = apprienProduct.ApprienVariantIAPId;

                        // Tell Apprien that the products were shown
                        StartCoroutine(_apprienManager.ProductsShown(_apprienProducts));
                    }
                },
                () =>
                {
                    SetDefaultOfflineProducts();
                });
        }

        private void ResetTexts()
        {
            // Reset prices
            for (var i = 0; i < _apprienProducts.Length; i++)
            {
                ApprienPriceSKUTexts[i].text = "IAP id";
                StandardPriceSKUTexts[i].text = "IAP id";

                ApprienPriceTexts[i].text = "Loading...";
                StandardPriceTexts[i].text = "Loading...";
            }
        }

        public void RefreshButtonPressed()
        {
            ResetTexts();

            TestApprienConnection(
            () =>
            {
                FetchPrices();
            },
            () =>
            {
                SetDefaultOfflineProducts();
            });
        }

        public void SwitchButtonPressed()
        {
            _currentType = _currentType == CanvasType.IAPs ? CanvasType.Subscriptions : CanvasType.IAPs;
            _IAPCanvas.SetActive(_currentType == CanvasType.IAPs);
            _subscriptionCanvas.SetActive(_currentType == CanvasType.Subscriptions);

            ResetTexts();
            InitializeProducts();
        }
    }
}