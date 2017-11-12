using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
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
		/// Product.
		/// </summary>
		[System.Serializable]
		public struct Product {
			public string name;
			public string apprien;
			public ProductMetadata metadata;
			public ProductMetadata metadataReference;
			public ProductType type;
			public string GetLocalizedPrice() {
				if (metadata == null) {
					Debug.LogWarning ("GetLocalizedPrice called before Apprien fully initialized");
					return "";
				}
				return metadata.localizedPrice + metadata.isoCurrencyCode;
			}
			public string GetReferencePrice() {
				if (metadataReference == null) {
					Debug.LogWarning ("GetReferencePrice called before Apprien fully initialized");
					return "";
				}
				return metadataReference.localizedPrice + metadataReference.isoCurrencyCode;
			}
			public Product(string name) {
				this.name = name;
				this.apprien = name;
				this.metadata = null;
				this.metadataReference = null;
				this.type = ProductType.Consumable;
			}
			public Product(string name, ProductType type) {
				this.name = name;
				this.apprien = name;
				this.metadata = null;
				this.metadataReference = null;
				this.type = type;
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

		protected static string REST_GET_PRODUCT_URL = "https://game.apprien.com/products/{0}"; // productName
		protected static string REST_POST_RECEIPT_URL = "https://game.apprien.com/products/{0}"; // productName

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
		/// Fetchs the apprien products.
		/// </summary>
		/// <returns>The apprien products.</returns>
		/// <param name="receiver">receiver.</param>
		/// <param name="products">Products.</param>
		protected static IEnumerator FetchApprienProducts(MonoBehaviour receiver, List<Product> products) {
			for(int i = 0; i < products.Count; i++) {
				Product product = products [i];
				string productname = product.name;
				UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRODUCT_URL, productname));
				www.SetRequestHeader ("token", token);
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
		/// Adds the products.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="products">Products.</param>
		public static void AddProducts(ConfigurationBuilder builder, List<Apprien.Product> products) {
			foreach (Apprien.Product product in products) {
				builder.AddProduct (product.apprien, product.type);

				//builder.AddProduct(product.name, product.type);
				//if (product.name != product.apprien) {
				//	builder.AddProduct (product.apprien, product.type);
				//}
			}
		}

		/// <summary>
		/// Raises the store initialized event.
		/// </summary>
		/// <param name="controller">Controller.</param>
		/// <param name="products">Products.</param>
		public static void OnStoreInitialized (IStoreController controller, List<Product> products) {
			for(int i = 0; i < products.Count; i++) {
				Apprien.Product product = products [i];
				product.metadata = controller.products.WithID (product.apprien).metadata;
				if (product.name != product.apprien) {
					if (controller.products.WithID (product.name) == null) {
						Debug.LogWarning ("Could not find product " + product.name);
					} else {
						product.metadataReference = controller.products.WithID (product.name).metadata;
					}
				}
				products [i] = product;
			}
		}

		/// <summary>
		/// Raises the process purchase event.
		/// </summary>
		/// <param name="receiver">receiver.</param>
		/// <param name="e">E.</param>
		public static void OnProcessPurchase(MonoBehaviour receiver, PurchaseEventArgs e) {
			receiver.StartCoroutine (PostReceipt(receiver, e));
		}

		/// <summary>
		/// Posts the receipt.
		/// </summary>
		/// <returns>The receipt.</returns>
		/// <param name="receiver">receiver.</param>
		/// <param name="e">E.</param>
		protected static IEnumerator PostReceipt(MonoBehaviour receiver, PurchaseEventArgs e) {
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			formData.Add(new MultipartFormDataSection("deal=receipt", e.purchasedProduct.receipt) );

			UnityWebRequest www = UnityWebRequest.Post(string.Format(REST_POST_RECEIPT_URL), formData);
			www.SetRequestHeader ("token", token);
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