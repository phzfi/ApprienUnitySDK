using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace Apprien
{
    /// <summary>
    /// Apprien Unity SDK to optimize IAP prices.
    ///
    /// Class Apprien is Plain-old-C#-object -client to the Apprien REST API.
    /// You can use it either with UnityStoreManager, or some other IAP plugin.
    ///
    /// Apprien is an automated pricing engine that calculates the optimum
    /// prices by every 15mins in each country. We can typically increase the
    /// revenue and Life Time Value of the game by 20-40%, which makes it easier
    /// to:
    /// 1) acquire more users (spend the money to User Acquisition)
    /// 2) find publishers or financiers
    /// 3) take it easy :)
    ///
    /// See more from https://www.apprien.com
    /// API Documentation on https://game.apprien.com
    /// </summary>
    public class ApprienManager
    {
        public string GamePackageName;
        public string Token = "TODO acquire token from Apprien Dashboard/support";
        public ApprienIntegrationType IntegrationType;

        // Request timeout in seconds
        public const int REQUEST_TIMEOUT = 5;

        // Apprien endpoints
        public string REST_GET_APPRIEN_STATUS = "https://game.apprien.com/status";
        public string REST_GET_VALIDATE_TOKEN_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/auth";
        public string REST_GET_ALL_PRICES_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/prices";
        public string REST_GET_PRICE_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/products/{2}/prices";
        public string REST_POST_RECEIPT_URL = "https://game.apprien.com/api/v1/receipts";

        public static readonly Dictionary<ApprienIntegrationType, string> IntegrationURI =
            new Dictionary<ApprienIntegrationType, string>() { { ApprienIntegrationType.GooglePlayStore, "google" }, };

        public string StoreIdentifier
        {
            get
            {
                return ApprienManager.IntegrationURI[IntegrationType];
            }
        }

        /// <summary>
        /// Returns the first byte of MD5-hashed SystemInfo.deviceUniqueIdentifier as string (two symbols).
        /// The identifier is sent to Apprien Game API 
        /// </summary>
        /// <value></value>
        public string ApprienIdentifier
        {
            get
            {
                var id = SystemInfo.deviceUniqueIdentifier;
                var bytes = System.Text.ASCIIEncoding.ASCII.GetBytes(id);
                var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                var hash = md5.ComputeHash(bytes);
                // Take the first byte only and convert it to hex
                return System.Convert.ToString(hash[0], 16);
            }
        }

        /// <summary>
        /// Initialize the Apprien SDK. 
        /// </summary>
        /// <param name="gamePackageName">The package name of the game. Usually Application.identifier</param>
        /// <param name="integrationType">Store integration, e.g. GooglePlayStore, AppleAppStore.</param>
        /// <param name="token">Token, retrieved from the Apprien Dashboard.</param>
        public ApprienManager(
            string gamePackageName,
            ApprienIntegrationType integrationType,
            string token
        )
        {
            GamePackageName = gamePackageName;
            IntegrationType = integrationType;
            Token = token;
        }

        /// <summary>
        /// Perform an availability check for the Apprien service and test the validity of the OAuth2 token.
        /// </summary>
        /// <param name="callback">The first parameter is true if Apprien is reachable. The second parameter is true if the provided token is valid</param>
        /// <returns></returns>
        public IEnumerator TestConnection(Action<bool, bool> callback)
        {
            // Check service status and validate the token
            var statusCheck = CheckServiceStatus();
            var tokenCheck = CheckTokenValidity();

            while (statusCheck.MoveNext() || tokenCheck.MoveNext())
            {
                yield return null;
            }

            // The two request IEnumerators will resolve to a boolean value in the end
            // Inform the calling component that Apprien is online
            if (callback != null)
            {
                callback((bool) statusCheck.Current, (bool) tokenCheck.Current);
            }
        }

        /// <summary>
        /// Check whether Apprien API service is online.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<bool?> CheckServiceStatus()
        {
            var requestSendTimestamp = DateTime.Now;
            using(var request = UnityWebRequest.Get(REST_GET_APPRIEN_STATUS))
            {
                request.SendWebRequest();

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > REQUEST_TIMEOUT)
                    {
                        Debug.Log("Timeout reached while checking Apprien status.");
                        yield return false;
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }

                // If there was an error sending the request, or the server returns an error code > 400
                if (request.isHttpError || request.isNetworkError)
                {
                    yield return false;
                }
                else
                {
                    // The service is online
                    yield return true;
                }
            }
        }

        /// <summary>
        /// Validates the supplied access token with the Apprien API
        /// </summary>
        /// <returns></returns>
        public IEnumerator<bool?> CheckTokenValidity()
        {
            var requestSendTimestamp = DateTime.Now;
            var url = string.Format(REST_GET_VALIDATE_TOKEN_URL, StoreIdentifier, GamePackageName);
            using(var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SendWebRequest();

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > REQUEST_TIMEOUT)
                    {
                        yield return false;
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }
                // If there was an error sending the request, or the server returns an error code > 400
                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.Log(request.responseCode);
                    yield return false;
                }
                else
                {
                    // The token is valid
                    yield return true;
                }

            }
        }

        /// <summary>
        /// Fetch all Apprien variant IAP ids with optimum prices.
        /// 
        /// Prices are located in the Apprien -generated IAP id variants. Typically
        /// the actual prices are fetched from the Store (Google or Apple) by the
        /// StoreManager by providing the IAP id (or in this case the variant).
        /// </summary>
        /// <param name="products">Array of Apprien.Product instances. After the request completes, the products will contain the Apprien IAP id variant</param>
        /// <param name="callback">Callback that is called when all product variant requests have completed.</param>
        /// <returns></returns>
        public IEnumerator FetchApprienPrice(ApprienProduct[] products, Action callback = null)
        {
            var fetchCoroutines = new List<IEnumerator>();
            // Send all Apprien requests at once, yield later
            foreach (var product in products)
            {
                fetchCoroutines.Add(FetchApprienPrice(product));
            }

            foreach (var coroutine in fetchCoroutines)
            {
                while (coroutine.MoveNext())
                {
                    yield return null;
                }
            }

            callback();
        }

        /// <summary>
        /// Fetch Apprien variant IAP id for the given product.
        /// NOTE: Only use this overload for fetching single products, if required by game/store logic. 
        /// Use the other overload when fetching multiple products, to save on request volume.
        /// 
        /// Prices are located in the Apprien -generated IAP id variants. Typically
        /// the actual prices are fetched from the Store (Google or Apple) by the
        /// StoreManager by providing the IAP id (or in this case the variant).
        /// </summary>
        /// <returns>The Apprien product variant name.</returns>
        /// <param name="product">Apprien.Product instance. After the request completes, will contain the Apprien IAP id variant.</param>
        /// <param name="callback">Callback that is called when the request finishes. Takes string argument, containing the resolved IAP id.</param>
        public IEnumerator FetchApprienPrice(ApprienProduct product, Action callback = null)
        {
            var requestSendTimestamp = DateTime.Now;
            var url = string.Format(REST_GET_PRICE_URL, StoreIdentifier, GamePackageName, product.BaseIAPId);

            using(var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Session-Id", ApprienIdentifier);
                request.SendWebRequest();

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > REQUEST_TIMEOUT)
                    {
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.Log(request.responseCode + ": " + request.error);
                    // On error return the fixed price = base IAP id
                    if (callback != null)
                    {
                        callback();
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        // Apprien IAP id variant fetched, apply it to the given product and
                        var apprienVariantIAPid = request.downloadHandler.text;
                        product.ApprienVariantIAPId = apprienVariantIAPid;
                        if (callback != null)
                        {
                            callback();
                        }
                    }
                    else
                    {
                        // If Apprien returns a non-200 message code, return base IAP id price
                        Debug.Log("Apprien request error: " + request.responseCode + ". " + request.downloadHandler.text);
                        if (callback != null)
                        {
                            callback();
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Posts the receipt to Apprien for calculating new prices.
        /// 
        /// Passes messages OnApprienPostReceiptSuccess or OnApprienPostReceiptFailed to the given MonoBehaviour.
        /// </summary>
        /// <returns>The receipt.</returns>
        /// <param name="unityComponent">MonoBehaviour, typically 'this'.</param>
        /// <param name="receiptJson"></param>
        public IEnumerator PostReceipt(MonoBehaviour unityComponent, string receiptJson)
        {
            var formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("deal=receipt", receiptJson));

            using(var request = UnityWebRequest.Post(REST_POST_RECEIPT_URL, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.Log(request.error);
                    unityComponent.SendMessage("OnApprienPostReceiptFailed", request.responseCode + ": " + request.error, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    unityComponent.SendMessage("OnApprienPostReceiptSuccess", request.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
                }
            }

        }

        /// <summary>
        /// Parses the base IAP id from the Apprien response (variant IAP id)
        ///
        /// Variant IAP id is e.g. "z_base_iap_id.apprien_500_dfa3", where 
        /// - the prefix is z_ (2 characters) to sort the IAP ids on store listing to then end
        /// - followed by the base IAP id that can be parsed by splitting the string by the separator ".apprien_"
        /// - followed by the price in cents
        /// - followed by 4 character hash
        /// </summary>
        /// <param name="storeIapId">Apprien product IAP id on the Store (Google or Apple) e.g. z_pack2_gold.apprien_399_abcd</param>
        public static string GetBaseIAPId(string storeIAPId)
        {
            // Default result to (base) storeIapId
            var result = storeIAPId;

            // First check if this is a variant IAP id or base IAP id
            var apprienSeparatorPosition = result.IndexOf(".apprien_");
            if (apprienSeparatorPosition > 0)
            {
                // Get the base IAP id part, remove the suffix
                result = result.Substring(0, apprienSeparatorPosition);

                // Remove prefix
                result = result.Substring(2);
            }

            return result;
        }
    }

    /// <summary>
    /// The IAP Product for Apprien.
    /// </summary>
    [System.Serializable]
    public class ApprienProduct
    {
        /// <summary>
        /// The base product id. Apprien will fallback to this id if a variant cannot be retrieved.
        /// </summary>
        public string BaseIAPId;

        /// <summary>
        /// Unity Purchasing -defined ProductType. e.g. Consumable, Non-Consumable etc.
        /// </summary>
        public ProductType ProductType;

        /// <summary>
        /// Apprien creates variants of the base IAP id, e.g.
        /// z_iapBaseName.apprien_1990_v34f
        /// where 1990 is e.g. 1990 USD cents and the last 4 symbols are a unique hash.
        /// The variants start with "z_" to sort them last and distiguish them
        /// easily from the base IAP ids
        /// </summary>
        [NonSerialized]
        public string ApprienVariantIAPId = "";

        /// <summary>
        /// Optional. If defined, the IAPId only applies to the given store. If this product exists in multiple stores,
        /// multiple ApprienProduct objects are required.
        /// The string is Unity's identifier for stores, e.g. "AppleAppStore", "GooglePlay" etc.
        /// </summary>
        public string Store;

        public ApprienProduct(string baseIapId, ProductType productType)
        {
            BaseIAPId = baseIapId;
            // Defaults the variant name to the base IAP id. FetchApprienPrice will replace this if fetch succeeds
            ApprienVariantIAPId = baseIapId;
            ProductType = productType;
        }

        public ApprienProduct(Product product)
        {
            BaseIAPId = product.definition.id;
            ApprienVariantIAPId = BaseIAPId;
            ProductType = product.definition.type;
        }

        /// <summary>
        /// Creates ApprienProduct objects from the products already added to the given builder.
        /// Does not add any products to the builder.
        /// </summary>
        /// <param name="builder">Reference to a builder containing products.</param>
        /// <returns></returns>
        public static ApprienProduct[] FromConfigurationBuilder(ConfigurationBuilder builder)
        {
            var products = new ApprienProduct[builder.products.Count];
            var i = 0;
            // HashSet cannot be indexed with [i]
            foreach (var product in builder.products)
            {
                products[i++] = new ApprienProduct(product.id, product.type);
            }

            return products;
        }

        /// <summary>
        /// Convert a Unity IAP Product Catalog into ApprienProduct objects ready for fetching Apprien prices.
        /// Does not alter the catalog
        /// </summary>
        /// <param name="catalog"></param>
        /// <returns></returns>
        public static ApprienProduct[] FromIAPCatalog(ProductCatalog catalog)
        {
            var catalogProducts = catalog.allValidProducts;
            /*
            // TODO: Get the store-specific products
            foreach (var product in catalogProducts)
            {
                Debug.Log(product.GetStoreID("GooglePlay"));
                Debug.Log(product.GetStoreID("AppleAppStore"));
            }
            */
            var products = new ApprienProduct[catalogProducts.Count];

            var i = 0;
            // ICollection cannot be indexed with [i], foreach required
            foreach (var catalogProduct in catalogProducts)
            {
                products[i++] = new ApprienProduct(catalogProduct.id, catalogProduct.type);
            }

            return products;
        }
    }

    public enum ApprienIntegrationType
    {
        GooglePlayStore,
        // Note: Apple App Store integration is not yet possible with Apprien. This feature is coming soon.
        // AppleAppStore
    }
}