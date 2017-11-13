using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Apprien.Unity.SDK {

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
	public class Apprien {

		/// <summary>
		/// The IAP Product.
		/// </summary>
		[System.Serializable]
		public struct Product {
			public string name;
			public string apprien;

            public Product(string name) {
				this.name = name;
				this.apprien = name;
			}
        }

		/// <summary>
		/// Product response.
		/// </summary>
		protected struct ProductResponse {
			public string reference;
			public string recommended;
		}


		protected static string appid;
		protected static string token;

		protected static string REST_GET_PRODUCT_URL = "https://game.apprien.com/products/{0}"; // productName sku i.e. pack_gold2
        protected static string REST_GET_PRICE_URL = "https://game.apprien.com/stores/google/products/{0}/prices"; // productName i.e. pack_gold2
		protected static string REST_POST_RECEIPT_URL = "https://game.apprien.com/receipts";

		/// <summary>
		/// Initialize the specified receiver, appid, token and products.
		/// </summary>
		/// <param name="receiver">Receiver.</param>
		/// <param name="appid">Appid.</param>
		/// <param name="token">Token.</param>
		/// <param name="products">Products.</param>
		public static void Initialize(MonoBehaviour receiver, string appid, string token, List<Product> products) {
			Apprien.appid = appid;
			Apprien.token = token;

			receiver.StartCoroutine (FetchApprienProducts(receiver, products));
		}

		/// <summary>
		/// Fetches IAP products from Apprien.
        ///
        /// If you have a fixed list of SKUs, you can skip this and
        /// just fetch the prices for your current product skus
        ///
		/// </summary>
		/// <returns>Apprien products.</returns>
        /// <param name="receiver">receiver which is typically "this" (Unity Component).</param>
		/// <param name="products">Products.</param>
		protected static IEnumerator FetchApprienProducts(MonoBehaviour receiver, List<Product> products) {
			for(int i = 0; i < products.Count; i++) {
				Product product = products [i];
				string productname = product.name;
				UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRODUCT_URL, productname));
				www.SetRequestHeader ("Authorization", "Bearer " + token);
				yield return www.Send();
				if (www.isNetworkError) {
					Debug.Log(www.error);
				} else {
					Debug.Log (www.downloadHandler.text);
					if (www.responseCode == 200) {
						ProductResponse response = JsonUtility.FromJson <ProductResponse> (www.downloadHandler.text);
						product.apprien = response.recommended;
					}
				}
				products [i] = product;
			}
			receiver.SendMessage ("OnApprienInitialized", products, SendMessageOptions.RequireReceiver);
		}

        /// <summary>
        /// Fetchs the apprien prices.
        /// </summary>
        /// <returns>The apprien product variants (with different prices).</returns>
        /// <param name="receiver">receiver.</param>
        /// <param name="products">Products.</param>
        public static IEnumerator FetchApprienPrice(MonoBehaviour receiver, string product, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRICE_URL, product));
            www.SetRequestHeader ("Authorization", "Bearer " + token);
            yield return www.Send();
            if (www.isNetworkError)
            {
                Debug.Log(www.error);
                callback(product);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                if (www.responseCode == 200)
                {
                    string productId = JsonUtility.FromJson<string>(www.downloadHandler.text);
                    callback(productId);
                }
            }
        }

        /// <summary>
		/// Posts the receipt.
		/// </summary>
		/// <returns>The receipt.</returns>
		/// <param name="receiver">receiver.</param>
		/// <param name="e">E.</param>
        public static IEnumerator PostReceipt(MonoBehaviour receiver, string receiptJson) {
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("deal=receipt",receiptJson) );

			UnityWebRequest www = UnityWebRequest.Post(string.Format(REST_POST_RECEIPT_URL), formData);
            www.SetRequestHeader ("Authorization", "Bearer " + token);
			yield return www.Send();

			if(www.isNetworkError) {
				Debug.Log(www.error);
				receiver.SendMessage ("OnApprienPostReceiptFailed", www.error, SendMessageOptions.DontRequireReceiver);
			}
			else {
				receiver.SendMessage ("OnApprienPostReceiptSuccess", www.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
			}
			yield return null;
		}
	}
}