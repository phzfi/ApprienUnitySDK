using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Networking;

namespace ApprienSDK {

	/// <summary>
	/// Apprien.
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

		protected static string REST_GET_PRODUCT_URL = "http://api.apprien.com/products/{0}"; // productName
		protected static string REST_POST_RECEIPT_URL = "http://api.apprien.com/products/{0}"; // productName

		/// <summary>
		/// Initialize the specified reciever, appid, token and products.
		/// </summary>
		/// <param name="reciever">Reciever.</param>
		/// <param name="appid">Appid.</param>
		/// <param name="token">Token.</param>
		/// <param name="products">Products.</param>
		public static void Initialize(MonoBehaviour reciever, string appid, string token, List<Product> products) {
			Apprien.appid = appid;
			Apprien.token = token;

			reciever.StartCoroutine (FetchApprienProducts(reciever, products));
		}

		/// <summary>
		/// Fetchs the apprien products.
		/// </summary>
		/// <returns>The apprien products.</returns>
		/// <param name="reciever">Reciever.</param>
		/// <param name="products">Products.</param>
		protected static IEnumerator FetchApprienProducts(MonoBehaviour reciever, List<Product> products) {
			for(int i = 0; i < products.Count; i++) {
				Product product = products [i];
				string productname = product.name;
				UnityWebRequest www = UnityWebRequest.Get(string.Format(REST_GET_PRODUCT_URL, productname));
				www.SetRequestHeader ("token", token);
				yield return www.Send();
				if (www.isError) {
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
			reciever.SendMessage ("OnApprienInitialized", products, SendMessageOptions.RequireReceiver);
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
		/// <param name="reciever">Reciever.</param>
		/// <param name="e">E.</param>
		public static void OnProcessPurchase(MonoBehaviour reciever, PurchaseEventArgs e) {
			reciever.StartCoroutine (PostReceipt(reciever, e));
		}

		/// <summary>
		/// Posts the receipt.
		/// </summary>
		/// <returns>The receipt.</returns>
		/// <param name="reciever">Reciever.</param>
		/// <param name="e">E.</param>
		protected static IEnumerator PostReceipt(MonoBehaviour reciever, PurchaseEventArgs e) {
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			formData.Add(new MultipartFormDataSection("deal=receipt", e.purchasedProduct.receipt) );

			UnityWebRequest www = UnityWebRequest.Post(string.Format(REST_POST_RECEIPT_URL), formData);
			www.SetRequestHeader ("token", token);
			yield return www.Send();

			if(www.isError) {
				Debug.Log(www.error);
				reciever.SendMessage ("OnApprienPostReceiptFailed", www.error, SendMessageOptions.DontRequireReceiver);
			}
			else {
				reciever.SendMessage ("OnApprienPostReceiptSuccess", www.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
			}
			yield return null;
		}
	}
}