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

    public static class ApprienUtility
    {
        /// <summary>
        /// Returns the first byte of MD5-hashed SystemInfo.deviceUniqueIdentifier as a hexadecimal string (two symbols).
        /// The identifier is sent to Apprien Game API 
        /// </summary>
        /// <value></value>
        public static string ApprienIdentifier
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
        /// Apprien REST API endpoint for POSTing a notice to Apprien that product was shown.
        /// </summary>
        public static string REST_POST_PRODUCTS_SHOWN_URL = "https://game.apprien.com/api/v1/stores/{0}/shown/products";

        /// <summary>
        /// Apprien REST API endpoint for testing the availability of the service
        /// </summary>
        public static string REST_GET_APPRIEN_STATUS = "https://game.apprien.com/status";

        /// <summary>
        /// Apprien REST API endpoint for testing the validity of the given token
        /// </summary>
        public static string REST_GET_VALIDATE_TOKEN_URL = "https://game.apprien.com/api/v1/stores/{0}/games/{1}/auth";

        /// <summary>
        /// Convert the ApprienIntegrationType enum into a resource URI that gets passed to the Apprien backend.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetIntegrationUri(ApprienIntegrationType type)
        {
            switch (type)
            {
                case ApprienIntegrationType.GooglePlayStore:
                    return "google";
                case ApprienIntegrationType.AppleAppStore:
                    return "apple";
            }

            return "unknown";
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
    }
}
