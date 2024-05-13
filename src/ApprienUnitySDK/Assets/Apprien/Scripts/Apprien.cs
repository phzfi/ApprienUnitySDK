using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace Apprien
{
    /// <summary>
    /// <para>
    /// Apprien Unity SDK to optimize IAP prices.
    /// </para>
    /// <para>
    /// Apprien can be used either with UnityStoreManager, or other IAP plugin with custom interface.
    /// </para>
    /// <para>
    /// Apprien is an automated pricing engine that calculates the optimum
    /// prices every 15 minutes in each country. We can typically increase the
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
        /// Apprien REST API endpoint for fetching all optimum product variants
        /// </summary>
        public static string REST_GET_ALL_PRICES_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/prices";

        /// <summary>
        /// Apprien REST API endpoint for fetching the optimum product variant for a single product
        /// </summary>
        public static string REST_GET_PRICE_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/products/{2}/prices";

        /// <summary>
        /// Apprien REST API endpoint for POSTing the receipt json for successful transactions
        /// </summary>
        public static string REST_POST_RECEIPT_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/receipts";

        /// <summary>
        /// Apprien REST API endpoint for POSTing the receipt json for successful transactions
        /// </summary>
        public static string REST_POST_ERROR_URL = "https://game.apprien.com/error?message={0}&responseCode={1}&storeGame={2}&store={3}";

        /// <summary>
        /// Apprien REST API endpoint for testing the availability of the service
        /// </summary>
        public static string REST_GET_APPRIEN_STATUS = "https://game.apprien.com/status";

        private string _gamePackageName => _backend?.GamePackageName;
        private string _storeIdentifier => _backend?.StoreIdentifier;
        private string _apprienIdentifier => _backend?.ApprienIdentifier;

        private IApprienBackendConnection _backend;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApprienManager" /> class.
        /// </summary>
        /// <param name="gamePackageName">The package name of the game. Usually Application.identifier</param>
        /// <param name="integrationType">Store integration, e.g. GooglePlayStore, AppleAppStore.</param>
        public ApprienManager(
            string gamePackageName,
            ApprienIntegrationType integrationType
        ) : this(
            new ApprienBackendConnection(
                gamePackageName,
                integrationType,
                ApprienUtility.ApprienIdentifier)
            )
        { }

        public ApprienManager(IApprienBackendConnection backend)
        {
            _backend = backend;
        }

        /// <summary>
        /// Set the request timeout for Apprien server requests.
        /// </summary>
        /// <param name="seconds"></param>
        public void SetRequestTimeout(float seconds)
        {
            _backend.RequestTimeout = seconds;
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
        public IEnumerator<ApprienFetchPricesResponse> FetchApprienPrices(ApprienProduct[] apprienProducts)
        {
            var url = string.Format(REST_GET_ALL_PRICES_URL, _storeIdentifier, _gamePackageName);

            var unityWebRequest = UnityWebRequest.Get(url);
            unityWebRequest.SetRequestHeader("Session-Id", _apprienIdentifier);
            var request = new UnityWebRequestWrapper(unityWebRequest);

            var fetch = _backend.FetchApprienPrices(request);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            // Apply the variant to the product, if the fetch was successful
            if (response != null && response.Success)
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

                    var productList = JsonUtility.FromJson<ApprienProductList>(response.JSON);
                    foreach (var product in productList.products)
                    {
                        if (productLookup.ContainsKey(product.@base))
                        {
                            productLookup[product.@base].ApprienVariantIAPId = product.variant;
                        }
                    }
                }
                catch { } // If the JSON cannot be parsed, products will be using default IAP ids
            }

            // Caller can use the result to determine actions on success, failure etc.
            yield return response;
            yield break;
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
        public IEnumerator<ApprienFetchPriceResponse> FetchApprienPrice(ApprienProduct product)
        {
            var url = string.Format(REST_GET_PRICE_URL, _storeIdentifier, _gamePackageName, product.BaseIAPId);

            var unityWebRequest = UnityWebRequest.Get(url);
            unityWebRequest.SetRequestHeader("Session-Id", _apprienIdentifier);
            var request = new UnityWebRequestWrapper(unityWebRequest);

            var fetch = _backend.FetchApprienPrice(request);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            // Apply the variant to the product, if the fetch was successful
            if (response != null && response.Success)
            {
                product.ApprienVariantIAPId = response.VariantId;
            }

            // Caller can use the result to determine actions on success, failure etc.
            yield return response;
            yield break;
        }

        /// <summary>
        /// <para>
        /// Posts the receipt to Apprien for calculating new prices.
        /// </para>
        /// </summary>
        /// <param name="receiptJson"></param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator<ApprienPostReceiptResponse> PostReceipt(string receiptJson)
        {
            var formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("deal=receipt", receiptJson));

            var url = String.Format(REST_POST_RECEIPT_URL, _storeIdentifier, _gamePackageName);
            var unityWebRequest = UnityWebRequest.Post(url, formData);

            var request = new UnityWebRequestWrapper(unityWebRequest);
            return _backend.PostReceipt(request);
        }

        /// <summary>
        /// Check whether Apprien API service is online.
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckServiceStatus()
        {
            var unityWebRequest = UnityWebRequest.Get(REST_GET_APPRIEN_STATUS);
            var request = new UnityWebRequestWrapper(unityWebRequest);
            return _backend.CheckServiceStatus(request);
        }
    }
}
