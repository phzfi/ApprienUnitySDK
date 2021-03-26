using System.Collections;
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
        private enum TabType
        {
            IAPs,
            Subscriptions
        }

        private ApprienManager _apprienManager;

        [SerializeField]
        private ApprienConnection ApprienConnection;

        [SerializeField]
        private Text[] productTitleLabels;

        [SerializeField]
        private Text[] standardPriceLabels;
        [SerializeField]
        private Text[] standardPriceIdLabels;

        [SerializeField]
        private Text[] dynamicPriceLabels;
        [SerializeField]
        private Text[] dynamicPriceIdLabels;

        private TabType _currentTab = TabType.IAPs;

        [SerializeField]
        private Button iapsButton;
        [SerializeField]
        private Button subscriptionsButton;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        private ConfigurationBuilder _builder;
        private ApprienProduct[] _apprienProducts;

        void Awake()
        {
            if (ApprienConnection == null || ApprienConnection.Token.Length == 0)
            {
                Debug.LogWarning("Token not provided for Apprien SDK. Unable to configure dynamic prices.");
            }
            // includes initializeproducts, also updates ui correctly.
            SwitchToTab(0); //InitializeProducts();
        }

        private void InitializeProducts()
        {
            // Create ApprienProducts from the IAP or subscription Catalog
            var catalogFile = Resources.Load<TextAsset>(_currentTab == TabType.IAPs ? "ApprienIAPProductCatalog" : "ApprienSubscriptionProductCatalog");
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

            // Add standard IAP ids, so that there is always a fallback if Apprien variants cannot be fetched
            foreach (var product in _apprienProducts)
            {
                _builder.AddProduct(product.BaseIAPId, product.ProductType);
            }

            FetchPrices();
        }

        /// <summary>
        /// Fetch Apprien variant IAP ids and re-initialize UnityPurchasing
        /// </summary>
        private void FetchPrices()
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
                    }
                )
            );
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
            var iapProducts = _storeController.products;

            for (var i = 0; i < _apprienProducts.Length; i++)
            {
                var apprienProduct = _apprienProducts[i];
                var iapApprienProduct = iapProducts.WithID(apprienProduct.ApprienVariantIAPId);
                var iapStandardProduct = iapProducts.WithID(apprienProduct.BaseIAPId);

                var apprienPrice = iapApprienProduct.metadata.localizedPriceString;
                var standardPrice = iapStandardProduct.metadata.localizedPriceString;

                standardPriceLabels[i].text = standardPrice;
                dynamicPriceLabels[i].text = apprienPrice;

                // Update the product title
                // TODO: if needed later, might use iapProducts.WithID(apprienProduct.BaseIAPId).metadata.localizedTitle
                var productName = _currentTab == TabType.IAPs ? "Product" : "Subscription";
                productName += " " + (i + 1);
                productTitleLabels[i].text = productName;
                // Update the Standard IAP ids to text
                standardPriceIdLabels[i].text = apprienProduct.BaseIAPId;
                // Update the Apprien IAP ids to text
                dynamicPriceIdLabels[i].text = apprienProduct.ApprienVariantIAPId;
            }

            // Tell Apprien that the products were shown
            StartCoroutine(_apprienManager.ProductsShown(_apprienProducts));
        }

        private void ResetTexts()
        {
            // Reset prices
            for (var i = 0; i < _apprienProducts.Length; i++)
            {
                productTitleLabels[i].text = "Loading title..";
                dynamicPriceIdLabels[i].text = "Loading id...";
                standardPriceIdLabels[i].text = "Loading id...";
                dynamicPriceLabels[i].text = "Loading price...";
                standardPriceLabels[i].text = "Loading price...";
            }
        }

        // fetch dynamic prices from apprien api and update example ui
        public void RefreshButtonPressed()
        {
            ResetTexts();
            FetchPrices();
        }

        // toggle between tabs
        public void ToggleTabButtonPressed()
        {
            TabType toTab = _currentTab == TabType.IAPs ? TabType.Subscriptions : TabType.IAPs;
            SwitchToTab((int)toTab);
        }

        // switch to tab by index
        public void SwitchToTab(int toTab)
        {
            // switch visible tab/Tab
            _currentTab = (TabType)toTab;
            //_iapsTab.SetActive(_currentTab == TabType.IAPs);
            //_subscriptionsTab.SetActive(_currentTab == TabType.Subscriptions);
            // update tab link buttons
            iapsButton.interactable = _currentTab != TabType.IAPs;
            subscriptionsButton.interactable = _currentTab != TabType.Subscriptions;

            // inits iaps or subscriptions based on current active tab/Tab
            InitializeProducts();

            // set "loading" texts
            // note: call after initializeproducts
            ResetTexts();
        }

        // Call from Unity UI Button from On Click
        public void PurchaseStandardIAPButtonPressed(int buttonIndex)
        {
            if (_apprienProducts == null)
                return;
            if (!(buttonIndex < _apprienProducts.Length))
                return;

            var apprienProduct = _apprienProducts[buttonIndex];
            PurchaseProduct(apprienProduct.BaseIAPId);
        }

        // Call from Unity UI Button from On Click
        public void PurchaseDynamicIAPButtonPressed(int buttonIndex)
        {
            if (_apprienProducts == null)
                return;
            if (!(buttonIndex < _apprienProducts.Length))
                return;

            var apprienProduct = _apprienProducts[buttonIndex];
            PurchaseProduct(apprienProduct.ApprienVariantIAPId);
        }

        // Make a purchase
        public void PurchaseProduct(string productId)
        {
            if (_storeController != null)
            {
                // Fetch the currency Product reference from Unity Purchasing
                Product product = _storeController.products.WithID(productId);
                if (product != null && product.availableToPurchase)
                {
                    _storeController.InitiatePurchase(product);
                }
            }
        }

    }
}