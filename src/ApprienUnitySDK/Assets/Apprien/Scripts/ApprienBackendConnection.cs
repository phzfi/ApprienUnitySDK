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
        IEnumerator<ApprienFetchPricesResponse> FetchApprienPrices(IUnityWebRequest request);
        IEnumerator<ApprienFetchPriceResponse> FetchApprienPrice(IUnityWebRequest request);
        IEnumerator<ApprienPostReceiptResponse> PostReceipt(IUnityWebRequest request);
        IEnumerator ProductsShown(IUnityWebRequest request);
        IEnumerator<bool?> CheckServiceStatus(IUnityWebRequest request);
        IEnumerator<bool?> CheckTokenValidity(IUnityWebRequest request);
        void SetToken(string token);
        string GamePackageName { get; }
        string Token { get; }
        string StoreIdentifier { get; }
        string ApprienIdentifier { get; }
        float RequestTimeout { get; set; }
    }

    public class ApprienBackendConnection : IApprienBackendConnection
    {
        /// <summary>
        /// The package name for the game. Usually Application.identifier.
        /// </summary>
        private string _gamePackageName;
        public string GamePackageName => _gamePackageName;

        /// <summary>
        /// OAuth2 token received from Apprien Dashboard.
        /// </summary>
        private string _token = "TODO acquire token from Apprien Dashboard/support";
        public string Token => _token;

        /// <summary>
        /// Define the store ApprienManager should integrate against, e.g. GooglePlayStore
        /// </summary>
        private ApprienIntegrationType _integrationType;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        private float _requestTimeout = 3f;
        public float RequestTimeout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = value; }
        }

        /// <summary>
        /// Gets the store's string identifier for the currently set IntegrationType
        /// </summary>
        public string StoreIdentifier => ApprienUtility.GetIntegrationUri(_integrationType);

        private string _apprienIdentifier;
        public string ApprienIdentifier => _apprienIdentifier;

        private ITimeProvider _timeProvider;

        public ApprienBackendConnection(
            string gamePackageName,
            ApprienIntegrationType integrationType,
            string token,
            string apprienIdentifier
        ) : this(gamePackageName, integrationType, token, apprienIdentifier, new TimeProvider()) { }

        public ApprienBackendConnection(
            string gamePackageName,
            ApprienIntegrationType integrationType,
            string token,
            string apprienIdentifier,
            ITimeProvider timeProvider
        )
        {
            _gamePackageName = gamePackageName;
            _integrationType = integrationType;
            _token = token;
            _apprienIdentifier = apprienIdentifier;
            _timeProvider = timeProvider;
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
        public void SendError(long responseCode, string errorMessage)
        {
            SendError(responseCode, errorMessage, _gamePackageName, StoreIdentifier);
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
        public IEnumerator<ApprienFetchPricesResponse> FetchApprienPrices(IUnityWebRequest request)
        {
            var requestSendTimestamp = _timeProvider.GetTimeNow();

            SendWebRequest(request);

            while (!request.isDone)
            {
                // Timeout the request and return false
                if ((_timeProvider.GetTimeNow() - requestSendTimestamp).TotalSeconds > _requestTimeout)
                {
                    yield return new ApprienFetchPricesResponse
                    {
                        Success = false,
                        Error = "Request timed out"
                    };
                    yield break;
                }

                // Specify that the request is still in progress
                yield return null;
            }

            var responseBody = request.downloadHandler?.text;

            if (IsHttpError(request))
            {
                SendError(request.responseCode, $"Error occured while fetching prices. HTTP error {request.responseCode}, body: {responseBody}");
                yield return new ApprienFetchPricesResponse
                {
                    Success = false,
                    Error = request.error,
                    Message = responseBody
                };
                yield break;
            }
            else if (IsNetworkError(request))
            {
                SendError(request.responseCode, "Error occured while fetching Apprien prices: Network error");
                yield return new ApprienFetchPricesResponse
                {
                    Success = false,
                    Error = request.error,
                    Message = responseBody
                };
                yield break;
            }
            else
            {
                yield return new ApprienFetchPricesResponse
                {
                    Success = true,
                    JSON = responseBody
                };
                yield break;
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
        public IEnumerator<ApprienFetchPriceResponse> FetchApprienPrice(IUnityWebRequest request)
        {
            var requestSendTimestamp = _timeProvider.GetTimeNow();

            SendWebRequest(request);

            while (!request.isDone)
            {
                // Timeout the request and return false
                if ((_timeProvider.GetTimeNow() - requestSendTimestamp).TotalSeconds > _requestTimeout)
                {
                    yield return new ApprienFetchPriceResponse
                    {
                        Success = false,
                        Error = "Request timed out"
                    };
                    yield break;
                }

                // Specify that the request is still in progress
                yield return null;
            }

            var responseBody = request.downloadHandler?.text;

            if (IsHttpError(request))
            {
                // send apprien api info about the error
                SendError(request.responseCode, $"Error occured while fetching prices. HTTP error {request.responseCode}, body: {responseBody}");
                yield return new ApprienFetchPriceResponse
                {
                    Success = false,
                    Error = request.error,
                    Message = responseBody
                };
                yield break;
            }
            else if (IsNetworkError(request))
            {
                // send apprien api info about the error
                SendError(request.responseCode, "Error occured while fetching Apprien prices. Network error");
                yield return new ApprienFetchPriceResponse
                {
                    Success = false,
                    Error = request.error,
                    Message = responseBody
                };
                yield break;
            }
            else
            {
                yield return new ApprienFetchPriceResponse
                {
                    Success = true,
                    VariantId = responseBody,
                    Error = request.error,
                    Message = responseBody
                };
                yield break;
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
        public IEnumerator<ApprienPostReceiptResponse> PostReceipt(IUnityWebRequest request)
        {
            var requestSendTimestamp = _timeProvider.GetTimeNow();
            SendWebRequest(request);

            while (!request.isDone)
            {
                // Timeout the request and break
                if ((_timeProvider.GetTimeNow() - requestSendTimestamp).TotalSeconds > _requestTimeout)
                {
                    yield return new ApprienPostReceiptResponse
                    {
                        ResponseCode = 0,
                        Error = request.error,
                        Message = "Request timed out"
                    };
                    yield break;
                }

                // Specify that the request is still in progress
                yield return null;
            }

            var responseBody = request.downloadHandler?.text;

            if (IsHttpError(request))
            {
                SendError(request.responseCode, $"Error occured while posting receipt. HTTP error {request.responseCode}, body: {responseBody}");
            }
            else if (IsNetworkError(request))
            {
                SendError(request.responseCode, "Error occured while posting receipt. Network error");
            }

            yield return new ApprienPostReceiptResponse
            {
                ResponseCode = request.responseCode,
                Error = request.error,
                Message = responseBody
            };
            yield break;
        }

        /// <summary>
        /// Tell Apprien that these products were shown. NOTE: This is needed for Apprien to work correctly.
        /// </summary>
        /// <param name="apprienProducts"></param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator ProductsShown(IUnityWebRequest request)
        {
            yield return SendWebRequest(request);

            if (IsHttpError(request))
            {
                var responseBody = request.downloadHandler?.text;
                SendError(request.responseCode, $"Error occured while posting products shown. HTTP error {request.responseCode}, body: {responseBody}");
            }
            else if (IsNetworkError(request))
            {
                SendError(request.responseCode, "Error occured while posting products shown. Network error");
            }
        }

        /// <summary>
        /// Check whether Apprien API service is online.
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckServiceStatus(IUnityWebRequest request)
        {
            var requestSendTimestamp = _timeProvider.GetTimeNow();
            SendWebRequest(request);

            while (!request.isDone)
            {
                // Timeout the request and return false
                if ((_timeProvider.GetTimeNow() - requestSendTimestamp).TotalSeconds > _requestTimeout)
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
                //Debug.LogError("Connection check: HTTP Error " + request.responseCode);
                yield return false;
                yield break;
            }
            else if (IsNetworkError(request))
            {
                //Debug.LogError("Connection check: Network Error " + request.responseCode);
                yield return false;
                yield break;
            }

            // The service is online
            yield return true;
            yield break;
        }

        /// <summary>
        /// Validates the supplied access token with the Apprien API
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckTokenValidity(IUnityWebRequest request)
        {
            var requestSendTimestamp = _timeProvider.GetTimeNow();
            SendWebRequest(request);

            while (!request.isDone)
            {
                // Timeout the request and return false
                if ((_timeProvider.GetTimeNow() - requestSendTimestamp).TotalSeconds > _requestTimeout)
                {
                    yield return false;
                    yield break;
                }

                // Specify that the request is still in progress
                yield return null;
            }
            // If there was any error in the request, the token is invalid
            if (IsHttpError(request))
            {
                //Debug.LogError("Token check: HTTP Error " + request.responseCode);
                yield return false;
                yield break;
            }
            else if (IsNetworkError(request))
            {
                //Debug.LogError("Token check: Network Error " + request.responseCode);
                yield return false;
                yield break;
            }

            // The token is valid
            yield return true;
        }

        /// <summary>
        /// Sends an error message to Apprien backend when the SDK encounters problems
        /// </summary>
        /// <param name="responseCode"></param>
        /// <param name="errorMessage"></param>
        private void SendError(long responseCode, string errorMessage, string packageName, string storeIdentifier)
        {
            var url = string.Format(ApprienManager.REST_POST_ERROR_URL, errorMessage, responseCode, packageName, storeIdentifier);
            using (var unityWebRequest = UnityWebRequest.Post(url, ""))
            {
                SendWebRequest(new UnityWebRequestWrapper(unityWebRequest));
            }
        }

#if UNITY_2017_1_OR_NEWER
        private static UnityWebRequestAsyncOperation SendWebRequest(IUnityWebRequest request)
        {
            return request.SendWebRequest();
        }
#elif UNITY_5_6_OR_NEWER
        private static AsyncOperation SendWebRequest(UnityWebRequest request)
        {
            return request.Send();
        }
#endif

        private static bool IsHttpError(IUnityWebRequest request)
        {
            bool fail;

#if UNITY_2020_1_OR_NEWER
            fail = request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError;
#elif UNITY_2017_1_OR_NEWER
            fail = request.isHttpError;
#else
            fail = request.responseCode >= 400;
#endif
            if (fail)
            {
                Debug.LogError($"{request.method} request URL '{request.url}' HTTP error code '{request.responseCode}'");
            }

            return fail;
        }

        private static bool IsNetworkError(IUnityWebRequest request)
        {
            bool fail;

#if UNITY_2020_1_OR_NEWER
            fail = request.result == UnityWebRequest.Result.ConnectionError;
#elif UNITY_2017_1_OR_NEWER
            fail = request.isNetworkError;
#else
            fail = request.isError;
#endif
            if (fail)
            {
                Debug.LogError($"{request.method} request URL '{request.url}' NETWORK error Code '{request.responseCode}'");
            }

            return fail;
        }
    }

    public class ApprienFetchPricesResponse
    {
        public bool Success;
        public string JSON;
        public string Message;
        public string Error;
    }

    public class ApprienFetchPriceResponse
    {
        public bool Success;
        public string VariantId;
        public string Message;
        public string Error;
    }

    public class ApprienPostReceiptResponse
    {
        public long ResponseCode;
        public bool Success => ResponseCode == 200;
        public string Message;
        public string Error;
    }
}
