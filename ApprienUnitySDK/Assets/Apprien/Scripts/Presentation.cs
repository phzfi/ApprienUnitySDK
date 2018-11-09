// Enable .NET 4.x in Player settings to enable more unit tests of Apprien SDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Apprien;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace ApprienUnitySDK
{
    public class StoreManager : MonoBehaviour, IStoreListener
    {
        public ApprienConnection ApprienConnection;

        private ApprienManager _apprienManager;
        private ApprienProduct[] _apprienProducts;

        private ConfigurationBuilder _builder;
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        public void Awake()
        {
            // Initialize the Apprien Manager with:
            // Store package name (i.e. com.company.product)
            // Integration type (Google Play etc.)
            // OAuth2 token received from Apprien team or dashboard
            _apprienManager = new ApprienManager(
                Application.identifier,
                ApprienIntegrationType.GooglePlayStore,
                ApprienConnection.Token
            );

            // Create ApprienProducts from the Unity IAP Catalog
            var catalog = ProductCatalog.LoadDefaultCatalog();
            _apprienProducts = ApprienProduct.FromIAPCatalog(catalog);

            // Initialize Unity IAP configuration builder
            _builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add default products
            foreach (var product in catalog.allValidProducts)
            {
                _builder.AddProduct(product.id, product.type);
            }

            // Add Apprien products
            StartCoroutine(
                _apprienManager.FetchApprienPrice(
                    _apprienProducts,
                    () =>
                    {
                        foreach (var product in _apprienProducts)
                        {
                            // Apprien variant IAP
                            _builder.AddProduct(
                                product.ApprienVariantIAPId,
                                product.ProductType
                            );
                        }

                        // Initialize the UnityPurchasing API with the fetched IAP ids
                        UnityPurchasing.Initialize(this, _builder);
                    }
                )
            );

        }

        /*
        public void Awake()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();

            // Initialize Unity IAP configuration builder
            _builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products
            foreach (var product in catalog.allValidProducts)
            {
                _builder.AddProduct(product.id, product.type);
            }

            // Initialize the UnityPurchasing API with the fetched IAP ids
            UnityPurchasing.Initialize(this, _builder);
        }
        */

        /*
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Overall Purchasing system, configured with products for this application.
            _storeController = controller;

            // Store specific subsystem, for accessing device-specific store features.
            _extensionProvider = extensions;

            // Update prices to UI
            foreach (var product in _storeController.products.all)
            {
                var id = product.definition.id;
                var price = product.metadata.localizedPriceString;
            }
        }
        */

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Overall Purchasing system, configured with products for this application.
            _storeController = controller;

            // Store specific subsystem, for accessing device-specific store features.
            _extensionProvider = extensions;

            // Update prices to UI
            foreach (var apprienProduct in _apprienProducts)
            {
                // If there are problems, the ID defaults to the base IAP ID
                var product = controller.products.WithID(apprienProduct.ApprienVariantIAPId);
                var id = product.definition.id;
                var price = product.metadata.localizedPriceString;
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            throw new NotImplementedException();
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            throw new NotImplementedException();
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}