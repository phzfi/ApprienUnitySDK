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

        public float RequestTimeout => _backend != null ? _backend.RequestTimeout : 0f;

        private IApprienBackendConnection _backend;

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
            _backend = new ApprienBackendConnection(gamePackageName, integrationType, token, ApprienIdentifier);
        }

        public ApprienManager(IApprienBackendConnection backend)
        {
            _backend = backend;
        }

        public void SetToken(string token)
        {
            _backend.SetToken(token);
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
            return _backend.FetchApprienPrices(apprienProducts, callback);
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
            return _backend.FetchApprienPrice(product, callback);
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
            return _backend.PostReceipt(unityComponent, receiptJson);
        }

        /// <summary>
        /// Tell Apprien that these products were shown. This is needed for the pricing engine to work efficiently.
        /// </summary>
        /// <param name="apprienProducts"></param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine.</returns>
        public IEnumerator ProductsShown(ApprienProduct[] apprienProducts)
        {
            return _backend.ProductsShown(apprienProducts);
        }

        /// <summary>
        /// Check whether Apprien API service is online.
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckServiceStatus()
        {
            return _backend.CheckServiceStatus();
        }

        /// <summary>
        /// Validates the supplied access token with the Apprien API
        /// </summary>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator<bool?> CheckTokenValidity(string token)
        {
            return _backend.CheckTokenValidity(token);
        }
    }
}
