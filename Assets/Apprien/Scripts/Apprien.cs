using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Apprien.Unity.SDK
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
    /// to
    /// 1) acquire more users (spend the money to User Acquisition)
    /// 2) find publishers or financiers
    /// 3) take it easy :)
    ///
    /// See more from www.apprien.com
    /// </summary>
    public class Apprien
    {

        /// <summary>
        /// The IAP Product for Apprien.
        /// </summary>
        [System.Serializable]
        public struct Product
        {
            /// SKU (stock keeping unit) is the store name for the product
            public string skuBaseName;
            /// Apprien creates variants of the base sku by name
            /// z_skuBaseName.apprien_1990_v34f
            /// where 1990 is e.g. 1990 USD and the last 4 digits are an unique
            /// hash
            /// The variants start by "z_" to sort them last and distiguish them
            /// easily from the base skus
			public string skuApprienVariantName;

            public Product(string skuBaseName)
            {
                this.skuBaseName = skuBaseName;
                this.skuApprienVariantName = skuBaseName; //default to the baseSku
            }
        }

        /// <summary>
        /// Product response.
        /// </summary>
        protected struct ProductResponse
        {
            public string reference;
            public string recommended;
        }


        protected static string appId;
        protected static string token;

        protected static string REST_GET_PRODUCT_URL = "https://game.apprien.com/products/{0}"; // productName sku i.e. pack_gold2
        protected static string REST_GET_PRICE_URL = "https://game.apprien.com/stores/google/products/{0}/prices"; // productName i.e. pack_gold2
        protected static string REST_POST_RECEIPT_URL = "https://game.apprien.com/receipts";

        /// <summary>
        /// Initialize the specified unityComponent, appId, token and products.
        /// </summary>
        /// <param name="unityComponent">unityComponent.</param>
        /// <param name="appId">appId.</param>
        /// <param name="token">Token.</param>
        /// <param name="products">Products.</param>
        public static void Initialize(MonoBehaviour unityComponent, string appId, string token, List<Product> products)
        {
            Apprien.appId = appId;
            Apprien.token = token;

            unityComponent.StartCoroutine(FetchApprienProducts(unityComponent, products));
        }

        /// <summary>
        /// Fetches IAP products from Apprien.
        ///
        /// If you have a fixed list of SKUs, you can skip this and
        /// just fetch the prices for your current product skus
        ///
        /// </summary>
        /// <returns>Apprien products.</returns>
        /// <param name="unityComponent">Monobehaviour unityComponent, which is typically 'this'.</param>
        /// <param name="products">Products.</param>
        protected static IEnumerator FetchApprienProducts(MonoBehaviour unityComponent, List<Product> products)
        {
            for (int i = 0; i < products.Count; i++)
            {
                Product product = products[i];
                string productName = product.skuBaseName;
                UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRODUCT_URL, productName));
                www.SetRequestHeader("Authorization", "Bearer " + token);
                yield return www.Send();
                if (www.isNetworkError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    Debug.Log(www.downloadHandler.text);
                    if (www.responseCode == 200)
                    {
                        ProductResponse response = JsonUtility.FromJson<ProductResponse>(www.downloadHandler.text);
                        product.skuApprienVariantName = response.recommended;
                    }
                }
                products[i] = product;
            }
            unityComponent.SendMessage("OnApprienInitialized", products, SendMessageOptions.RequireReceiver);
        }

        /// <summary>
        /// Fetches the Apprien prices.
        /// 
        /// Prices are located in the Apprien -generated SKU variants. Typically
        /// the actual prices are fetched from the Store (Google or Apple) by the
        /// StoreManager by providing the sku name (or in this case the variant).
        /// </summary>
        /// <returns>The apprien product variants (with different prices).</returns>
        /// <param name="unityComponent">Monobehaviour unityComponent, which is typically 'this'.</param>
        /// <param name="skuBaseName">Product skuBaseName i.e. my_pack_2 from Google/Apple store.</param>
        public static IEnumerator FetchApprienPrice(MonoBehaviour unityComponent, string skuBaseName, System.Action<string> callback)
        {
            UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRICE_URL, skuBaseName));
            www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.Send();
            if (www.isNetworkError)
            {
                Debug.Log(www.error);
                callback(skuBaseName);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                if (www.responseCode == 200)
                {
                    string skuApprienVariantName = JsonUtility.FromJson<string>(www.downloadHandler.text);
                    callback(skuApprienVariantName);
                }
            }
        }

        /// <summary>
		/// Posts the receipt to Apprien for calculate new prices.
		/// </summary>
		/// <returns>The receipt.</returns>
		/// <param name="unityComponent">Monobehaviour unityComponent, which is typically 'this'.</param>
        /// <param name="receiptJson">receiptJson.</param>
        public static IEnumerator PostReceipt(MonoBehaviour unityComponent, string receiptJson)
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("deal=receipt", receiptJson));

            UnityWebRequest www = UnityWebRequest.Post(string.Format(REST_POST_RECEIPT_URL), formData);
            www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.Send();

            if (www.isNetworkError)
            {
                Debug.Log(www.error);
                unityComponent.SendMessage("OnApprienPostReceiptFailed", www.error, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                unityComponent.SendMessage("OnApprienPostReceiptSuccess", www.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
            }
            yield return null;
        }
    }

    /// <summary>
    /// Parses the base SKU name (used by the game to display the graphic assets and 
    /// to deliver the goods to the player) based on the Apprien response (variant SKU)
    ///
    /// Variant SKU is e.g. "z_base_sku_name.apprien_500_dfa3", where 
    /// - the prefix is z_ (2 characters) to sort the skus on store listing to then end
    /// - followed by the base sku name that can be parsed by splitting the string by separator ".apprien_"
    /// - followed by the price in cents
    /// - followed by 4 character hash
    /// </summary>
    /// <param name="products">Products.</param>
    public static string GetBaseSku(string storeSku)
    {
        //default result to (base) storeSku
        string result = storeSku;
        //first check if this is a variant sku or base sku
        int apprienSeparatorPosition = result.IndexOf(".apprien_");
        if (apprienSeparatorPosition > 0)
        {
            //it's an Apprien sku variant, then check if response is JSON or text
            int jsonPosition = result.IndexOf("[");
            int offset = 0;
            if (jsonPosition > 0)
            {
                offset = 2;
            }
            //remove offset and prefix
            result = storeSku.Substring(2 + offset, storeSku.Length - 2 - offset);
            result = result.Substring(0, result.Length - apprienSeparatorPosition - 1);
        }
        return result;
    }

}
