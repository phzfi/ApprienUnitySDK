using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace Apprien
{
    /// <summary>
    /// Defines the available integrations Apprien supports.
    /// </summary>
    public enum ApprienIntegrationType
    {
        /// <summary>
        /// Represents Google Play Store integration
        /// </summary>
        GooglePlayStore,
        AppleAppStore
    }

    /// <summary>
    /// <para>
    /// Apprien Unity SDK to optimize IAP prices.
    /// </para>
    /// <para>
    /// Class Apprien is Plain-old-C#-object -client to the Apprien REST API.
    /// You can use it either with UnityStoreManager, or some other IAP plugin.
    /// </para>
    /// <para>
    /// Apprien is an automated pricing engine that calculates the optimum
    /// prices by every 15mins in each country. We can typically increase the
    /// revenue and Life Time Value of the game by 20-40%, which makes it easier
    /// to:
    /// 1) acquire more users (spend the money to User Acquisition)
    /// 2) find publishers or financiers
    /// 3) take it easy :)
    /// </para>
    /// <para>
    /// See more from https://www.apprien.com
    /// API Documentation on https://game.apprien.com
    /// </para>
    /// </summary>
    public class ApprienManager
    {
        /// <summary>
        /// The package name for the game. Usually Application.identifier.
        /// </summary>
        public string GamePackageName;

        /// <summary>
        /// OAuth2 token received from Apprien Dashboard.
        /// </summary>
        public string Token = "TODO acquire token from Apprien Dashboard/support";

        /// <summary>
        /// Define the store ApprienManager should integrate against, e.g. GooglePlayStore
        /// </summary>
        public ApprienIntegrationType IntegrationType;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public const int REQUEST_TIMEOUT = 5;

        /// <summary>
        /// Apprien REST API endpoint for testing the availability of the service
        /// </summary>
        public string REST_GET_APPRIEN_STATUS = "https://game.apprien.com/status";

        /// <summary>
        /// Apprien REST API endpoint for testing the validity of the given token
        /// </summary>
        public string REST_GET_VALIDATE_TOKEN_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/auth";

        /// <summary>
        /// Apprien REST API endpoint for fetching all optimum product variants
        /// </summary>
        public string REST_GET_ALL_PRICES_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/prices";

        /// <summary>
        /// Apprien REST API endpoint for fetching the optimum product variant for a single product
        /// </summary>
        public string REST_GET_PRICE_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/products/{2}/prices";

        /// <summary>
        /// Apprien REST API endpoint for POSTing the receipt json for successful transactions
        /// </summary>
        public string REST_POST_RECEIPT_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/receipts";

        /// <summary>
        /// Apprien REST API endpoint for POSTing the receipt json for successful transactions
        /// </summary>
        public string REST_POST_ERROR_URL = "https://game.apprien.com/error?message={0}&responseCode={1}&storeGame={2}&store={3}";

        /// <summary>
        /// Apprien REST API endpoint for POSTing a notice to Apprien that product was shown.false

        /// </summary>
        public string REST_POST_PRODUCTS_SHOWN_URL = "https://game.apprien.com/api/v1/stores/{0}/shown/products";

        /// <summary>
        /// Dictionary for mapping store names (in Apprien REST API URLs) to ApprienIntegrationType
        /// </summary>
        private static readonly Dictionary<ApprienIntegrationType, string> _integrationURI =
            new Dictionary<ApprienIntegrationType, string>() {
                { ApprienIntegrationType.GooglePlayStore, "google" },
                { ApprienIntegrationType.AppleAppStore, "apple" },
            };

        /// <summary>
        /// Gets the store's string identifier for the currently set ApprienIntegrationType
        /// </summary>
        public string StoreIdentifier
        {
            get
            {
                return ApprienManager._integrationURI[IntegrationType];
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
        /// Initializes a new instance of the <see cref="ApprienManager" /> class.
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
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
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
                callback((bool)statusCheck.Current, (bool)tokenCheck.Current);
            }
        }

        /// <summary>
        /// Check whether Apprien API service is online.
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckServiceStatus()
        {
            var requestSendTimestamp = DateTime.Now;
            using (var request = UnityWebRequest.Get(REST_GET_APPRIEN_STATUS))
            {
                SendWebRequest(request);

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
                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while checking service status: HTTP error: " + request.downloadHandler.text);
                    yield return false;
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while checking service status: Network error");
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
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckTokenValidity()
        {
            var requestSendTimestamp = DateTime.Now;
            var url = string.Format(REST_GET_VALIDATE_TOKEN_URL, StoreIdentifier, GamePackageName);
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                SendWebRequest(request);

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
                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while checking token validity: HTTP error: " + request.downloadHandler.text);
                    yield return false;
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while checking token validity: Network error");
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
        /// Sends error message when Apprien encounter any problems
        /// </summary>
        /// <param name="responseCode">Http responsecode</param>
        /// <param name="errorMessage">errorMessage changes depending on the error</param>
        private void SendError(int responseCode, string errorMessage)
        {
            var url = string.Format(REST_POST_ERROR_URL, errorMessage, responseCode, GamePackageName, StoreIdentifier);

            using (var post = UnityWebRequest.Post(url, ""))
            {
                SendWebRequest(post);
            }
        }

        /// <summary>
        /// <para>
        /// Fetch all Apprien variant IAP ids with optimum prices.
        /// </para>
        /// <para>
        /// Prices are located in the Apprien -generated IAP id variants. Typically
        /// the actual prices are fetched from the Store (Google or Apple) by the
        /// StoreManager by providing the IAP id (or in this case the variant).
        /// </para>
        /// </summary>
        /// <param name="callback">Callback that is called when all product variant requests have completed.</param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator FetchApprienPrices(ApprienProduct[] apprienProducts, Action callback = null)
        {
            var requestSendTimestamp = DateTime.Now;
            var url = string.Format(REST_GET_ALL_PRICES_URL, StoreIdentifier, GamePackageName);

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Session-Id", ApprienIdentifier);
                SendWebRequest(request);

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

                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices: HTTP error: " + request.downloadHandler.text);
                    // On error return the fixed price = base IAP id
                    if (callback != null)
                    {
                        callback();
                    }
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices: Network error");
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
                        // Parse the JSON data and update the variant IAP ids
                        try
                        {
                            // Create lookup to update the products in more linear time
                            var productLookup = new Dictionary<string, ApprienProduct>();
                            foreach (var product in apprienProducts)
                            {
                                productLookup[product.BaseIAPId] = product;
                            }

                            var json = request.downloadHandler.text;
                            var productList = JsonUtility.FromJson<ApprienProductList>(json);
                            foreach (var product in productList.products)
                            {
                                if (productLookup.ContainsKey(product.@base))
                                {
                                    productLookup[product.@base].ApprienVariantIAPId = product.variant;
                                }
                            }
                        }
                        catch { } // If the JSON cannot be parsed, products will be using default IAP ids

                        if (callback != null)
                        {
                            callback();
                        }
                    }
                    else
                    {
                        // If Apprien returns a non-200 message code, return base IAP id price
                        SendError((int)request.responseCode, "Error occured while fetching Apprien prices. Error: " + request.downloadHandler.text);
                        if (callback != null)
                        {
                            callback();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <para>
        /// Fetch Apprien variant IAP id for the given product.
        /// NOTE: Only use this overload for fetching single products, if required by game/store logic. 
        /// Use the other overload when fetching multiple products, to save on request volume.
        /// </para>
        /// <para>>
        /// Prices are located in the Apprien -generated IAP id variants. Typically
        /// the actual prices are fetched from the Store (Google or Apple) by the
        /// StoreManager by providing the IAP id (or in this case the variant).
        /// </para>
        /// </summary>
        /// <param name="product">Apprien.Product instance. After the request completes, will contain the Apprien IAP id variant.</param>
        /// <param name="callback">Callback that is called when the request finishes. Takes string argument, containing the resolved IAP id.</param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator FetchApprienPrice(ApprienProduct product, Action callback = null)
        {
            var requestSendTimestamp = DateTime.Now;
            var url = string.Format(REST_GET_PRICE_URL, StoreIdentifier, GamePackageName, product.BaseIAPId);

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Session-Id", ApprienIdentifier);
                SendWebRequest(request);

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

                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices. HTTP error: " + request.downloadHandler.text);
                    // On error return the fixed price = base IAP id
                    if (callback != null)
                    {
                        callback();
                    }
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices. Network error");
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
                        SendError((int)request.responseCode, "Error occured while fetching Apprien prices");
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
        /// <para>
        /// Posts the receipt to Apprien for calculating new prices.
        /// </para>
        /// <para>
        /// Passes messages OnApprienPostReceiptSuccess or OnApprienPostReceiptFailed to the given MonoBehaviour.
        /// </para>
        /// </summary>
        /// <param name="unityComponent">MonoBehaviour, typically 'this'.</param>
        /// <param name="receiptJson"></param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator PostReceipt(MonoBehaviour unityComponent, string receiptJson)
        {
            var formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("deal=receipt", receiptJson));

            var url = String.Format(REST_POST_RECEIPT_URL, StoreIdentifier, GamePackageName);

            using (var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                yield return SendWebRequest(request);

                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting receipt. HTTP error: " + request.downloadHandler.text);
                    unityComponent.SendMessage("OnApprienPostReceiptFailed", request.responseCode + ": " + request.error, SendMessageOptions.DontRequireReceiver);
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting receipt. Network error");
                    unityComponent.SendMessage("OnApprienPostReceiptFailed", request.responseCode + ": " + request.error, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    unityComponent.SendMessage("OnApprienPostReceiptSuccess", request.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
                }
            }

        }

        /// <summary>
        /// Tell Apprien that these products were shown. NOTE: This is needed for Apprien to work correctly.
        /// </summary>
        /// <param name="apprienProducts"></param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator ProductsShown(ApprienProduct[] apprienProducts)
        {
            var formData = new List<IMultipartFormSection>();

            for (var i = 0; i < apprienProducts.Length; i++)
            {
                formData.Add(new MultipartFormDataSection("iap_ids[" + i + "]", apprienProducts[i].ApprienVariantIAPId));
            }

            var url = String.Format(REST_POST_PRODUCTS_SHOWN_URL, StoreIdentifier);

            using (var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                yield return SendWebRequest(request);

                if (IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting receipt. HTTP error: " + request.downloadHandler.text);
                }
                else if (IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting receipt. Network error");
                }
            }
        }

        /// <summary>
        /// <para>
        /// Parses the base IAP id from the Apprien response (variant IAP id)
        /// </para>
        /// <para>
        /// Variant IAP id is e.g. "z_base_iap_id.apprien_500_dfa3", where 
        /// - the prefix is z_ (2 characters) to sort the IAP ids on store listing to then end
        /// - followed by the base IAP id that can be parsed by splitting the string by the separator ".apprien_"
        /// - followed by the price in cents
        /// - followed by 4 character hash
        /// </para>
        /// </summary>
        /// <param name="storeIapId">Apprien product IAP id on the Store (Google or Apple) e.g. z_pack2_gold.apprien_399_abcd</param>
        /// <returns>Returns the base IAP id for the given Apprien variant IAP id.</returns>
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

#if UNITY_2017_1_OR_NEWER
        private UnityWebRequestAsyncOperation SendWebRequest(UnityWebRequest request)
        {
            return request.SendWebRequest();
        }
#elif UNITY_5_6_OR_NEWER
        private AsyncOperation SendWebRequest(UnityWebRequest request)
        {
            return request.Send();
        }
#endif

        private bool IsHttpError(UnityWebRequest request)
        {
#if UNITY_2017_1_OR_NEWER
            return request.isHttpError;
#else
            return request.responseCode >= 400;
#endif
        }

        private bool IsNetworkError(UnityWebRequest request)
        {
#if UNITY_2017_1_OR_NEWER
            return request.isNetworkError;
#else
            return request.isError;
#endif
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
        /// <returns>Returns an array of Apprien Products built from the given ConfigurationBuilder object</returns>
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
        /// <returns>Returns an array of Apprien Products built from the given ProductCatalog object</returns>
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

    /// <summary>
    /// Product list class used for parsing JSON.
    /// </summary>
    [System.Serializable]
    public class ApprienProductList
    {
        public List<ApprienProductListProduct> products;
    }

    [System.Serializable]
    public class ApprienProductListProduct
    {
        public string @base; // @ because base is a keyword
        public string variant;
    }
}