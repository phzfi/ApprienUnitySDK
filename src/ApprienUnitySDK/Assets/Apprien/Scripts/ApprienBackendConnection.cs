using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace Apprien
{
    public interface IApprienBackendConnection
    {
        IEnumerator FetchApprienPrices(ApprienProduct[] apprienProducts, Action callback = null);
        IEnumerator FetchApprienPrice(ApprienProduct product, Action callback = null);
        IEnumerator PostReceipt(MonoBehaviour unityComponent, string receiptJson);
        IEnumerator ProductsShown(ApprienProduct[] apprienProducts);
        IEnumerator<bool?> CheckTokenValidity(string token);
        void SetToken(string token);
        float RequestTimeout { get; }
    }

    public class ApprienBackendConnection : IApprienBackendConnection
    {
        /// <summary>
        /// The package name for the game. Usually Application.identifier.
        /// </summary>
        private string _gamePackageName;

        /// <summary>
        /// OAuth2 token received from Apprien Dashboard.
        /// </summary>
        private string _token = "TODO acquire token from Apprien Dashboard/support";

        /// <summary>
        /// Define the store ApprienManager should integrate against, e.g. GooglePlayStore
        /// </summary>
        private ApprienIntegrationType _integrationType;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        private float _requestTimeout = 3f;
        public float RequestTimeout => _requestTimeout;

        /// <summary>
        /// Gets the store's string identifier for the currently set ApprienIntegrationType
        /// </summary>
        private string _storeIdentifier
        {
            get
            {
                return ApprienUtility.GetIntegrationUri(_integrationType);
            }
        }

        private string _apprienIdentifier;

        public ApprienBackendConnection(
            string gamePackageName,
            ApprienIntegrationType integrationType,
            string token,
            string apprienIdentifier
        )
        {
            _gamePackageName = gamePackageName;
            _integrationType = integrationType;
            _token = token;
            _apprienIdentifier = apprienIdentifier;
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        /// <summary>
        /// Sends an error message to Apprien backend when the SDK encounters problems
        /// </summary>
        /// <param name="responseCode"></param>
        /// <param name="errorMessage"></param>
        public void SendError(int responseCode, string errorMessage)
        {
            ApprienUtility.SendError(responseCode, errorMessage, _gamePackageName, _storeIdentifier);
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
            var url = string.Format(ApprienUtility.REST_GET_ALL_PRICES_URL, _storeIdentifier, _gamePackageName);

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _token);
                request.SetRequestHeader("Session-Id", _apprienIdentifier);
                ApprienUtility.SendWebRequest(request);

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > _requestTimeout)
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
                    SendError((int)request.responseCode, $"Error occured while fetching Apprien prices: HTTP error: {request.downloadHandler.text}");
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices: Network error");
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
                        SendError((int)request.responseCode, "Error occured while fetching Apprien prices. Error: " + request.downloadHandler.text);
                    }
                }

                // Regardless of the outcome, execute the callback
                callback?.Invoke();
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
            var url = string.Format(ApprienUtility.REST_GET_PRICE_URL, _storeIdentifier, _gamePackageName, product.BaseIAPId);

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _token);
                request.SetRequestHeader("Session-Id", _apprienIdentifier);
                ApprienUtility.SendWebRequest(request);

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > _requestTimeout)
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
                    // send apprien api info about the error
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices. HTTP error: " + request.downloadHandler.text);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    // send apprien api info about the error
                    SendError((int)request.responseCode, "Error occured while fetching Apprien prices. Network error");
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
                        SendError((int)request.responseCode, "Error occured while fetching Apprien prices");
                        Debug.Log("Apprien request error: " + request.responseCode + ". " + request.downloadHandler.text);
                    }
                }

                // Regardless of the outcome, execute the callback
                callback?.Invoke();
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

            var url = String.Format(ApprienUtility.REST_POST_RECEIPT_URL, _storeIdentifier, _gamePackageName);

            using (var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _token);
                yield return ApprienUtility.SendWebRequest(request);

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting receipt. HTTP error: " + request.downloadHandler.text);
                    unityComponent.SendMessage("OnApprienPostReceiptFailed", request.responseCode + ": " + request.error, SendMessageOptions.DontRequireReceiver);
                }
                else if (ApprienUtility.IsNetworkError(request))
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

            var url = String.Format(ApprienUtility.REST_POST_PRODUCTS_SHOWN_URL, _storeIdentifier);

            using (var request = UnityWebRequest.Post(url, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _token);
                yield return ApprienUtility.SendWebRequest(request);

                if (ApprienUtility.IsHttpError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting products shown. HTTP error: " + request.downloadHandler.text);
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    SendError((int)request.responseCode, "Error occured while posting products shown. Network error");
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
            var url = string.Format(ApprienUtility.REST_GET_VALIDATE_TOKEN_URL, ApprienUtility.GetIntegrationUri(ApprienIntegrationType.GooglePlayStore), Application.identifier);
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _token);
                ApprienUtility.SendWebRequest(request);

                while (!request.isDone)
                {
                    // Timeout the request and return false
                    if ((DateTime.Now - requestSendTimestamp).TotalSeconds > _requestTimeout)
                    {
                        Debug.Log("Apprien Token validity check: Request Timeout");
                        yield return false;
                        yield break;
                    }

                    // Specify that the request is still in progress
                    yield return null;
                }
                // If there was an error sending the request, or the server returns an error code > 400
                if (ApprienUtility.IsHttpError(request))
                {
                    //Debug.LogError("Token check: HTTP Error " + request.responseCode);
                    yield return false;
                    yield break;
                }
                else if (ApprienUtility.IsNetworkError(request))
                {
                    //Debug.LogError("Token check: Network Error " + request.responseCode);
                    yield return false;
                    yield break;
                }

                // The token is valid
                yield return true;
            }
        }
    }
}
