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
        public float RequestTimeout = 3f;

        /// <summary>
        /// Gets the store's string identifier for the currently set ApprienIntegrationType
        /// </summary>
        public string StoreIdentifier
        {
            get
            {
                return ApprienUtility.GetIntegrationUri(IntegrationType);
            }
        }

        /// <summary>
        /// Returns the first byte of MD5-hashed SystemInfo.deviceUniqueIdentifier as a hexadecimal string (two symbols).
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
        /// Sends an error message to Apprien backend when the SDK encounters problems
        /// </summary>
        /// <param name="responseCode"></param>
        /// <param name="errorMessage"></param>
        private void SendError(int responseCode, string errorMessage)
        {
            ApprienUtility.SendError(responseCode, errorMessage, GamePackageName, StoreIdentifier);
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
            var url = string.Format(ApprienUtility.REST_GET_ALL_PRICES_URL, StoreIdentifier, GamePackageName);

            using(var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Session-Id", ApprienIdentifier);
                ApprienUtility.SendWebRequest(request);

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > RequestTimeout)
                    {
                        if (callback != null)
                        {
                            callback();
                        }
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int) request.responseCode, "Error occured while fetching Apprien prices: HTTP error: " + request.downloadHandler.text);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int) request.responseCode, "Error occured while fetching Apprien prices: Network error");
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
                    }
                    else
                    {
                        // If Apprien returns a non-200 message code, return base IAP id price
                        SendError((int) request.responseCode, "Error occured while fetching Apprien prices. Error: " + request.downloadHandler.text);
                    }
                }

                // Regardless of the outcome, execute the callback
                if (callback != null)
                {
                    callback();
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
            var url = string.Format(ApprienUtility.REST_GET_PRICE_URL, StoreIdentifier, GamePackageName, product.BaseIAPId);

            using(var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Session-Id", ApprienIdentifier);
                ApprienUtility.SendWebRequest(request);

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > RequestTimeout)
                    {
                        if (callback != null)
                        {
                            callback();
                        }
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int) request.responseCode, "Error occured while fetching Apprien prices. HTTP error: " + request.downloadHandler.text);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int) request.responseCode, "Error occured while fetching Apprien prices. Network error");
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        // Apprien IAP id variant fetched, apply it to the given product and
                        var apprienVariantIAPid = request.downloadHandler.text;
                        product.ApprienVariantIAPId = apprienVariantIAPid;
                    }
                    else
                    {
                        // If Apprien returns a non-200 message code, return base IAP id price
                        SendError((int) request.responseCode, "Error occured while fetching Apprien prices");
                        Debug.Log("Apprien request error: " + request.responseCode + ". " + request.downloadHandler.text);
                    }
                }

                // Regardless of the outcome, execute the callback
                if (callback != null)
                {
                    callback();
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

            var url = String.Format(ApprienUtility.REST_POST_RECEIPT_URL, StoreIdentifier, GamePackageName);

            using(var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                yield return ApprienUtility.SendWebRequest(request);

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int) request.responseCode, "Error occured while posting receipt. HTTP error: " + request.downloadHandler.text);
                    unityComponent.SendMessage("OnApprienPostReceiptFailed", request.responseCode + ": " + request.error, SendMessageOptions.DontRequireReceiver);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int) request.responseCode, "Error occured while posting receipt. Network error");
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

            var url = String.Format(ApprienUtility.REST_POST_PRODUCTS_SHOWN_URL, StoreIdentifier);

            using(var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                yield return ApprienUtility.SendWebRequest(request);

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int) request.responseCode, "Error occured while posting products shown. HTTP error: " + request.downloadHandler.text);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int) request.responseCode, "Error occured while posting products shown. Network error");
                }
            }
        }
    }
}