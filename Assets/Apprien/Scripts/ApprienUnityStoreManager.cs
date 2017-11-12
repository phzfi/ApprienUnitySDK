using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Apprien.Unity.SDK {

	/// <summary>
    /// Apprien store manager uses Unity Store Manager (Unity.Purchasing).
    /// 
    /// If you use other IAP manager, you don't need this.
	/// </summary>
	public class ApprienUnityStoreManager : MonoBehaviour, IStoreListener {

		#if UNITY_EDITOR
		public bool editorToggle;
		#endif

		protected IStoreController controller;
		protected IExtensionProvider extensions;

        /// <summary>
        /// The IAP Product.
        /// </summary>
        [System.Serializable]
        public struct Product : Apprien.Product {
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
		/// The products.
		/// </summary>
		public List<Product> products = new List<Product>();

		/// <summary>
		/// The appid.
		/// </summary>
		public string appid;

		/// <summary>
		/// The token.
		/// </summary>
		public string token;

		/// <summary>
		/// Awake this instance.
		/// </summary>
		protected virtual void Awake () {
			DontDestroyOnLoad (this.gameObject);
			Apprien.Initialize(this, appid, token, products);
		}


        /// <summary>
        /// Adds the products.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="products">Products.</param>
        public void AddProducts(ConfigurationBuilder builder, List<Product> products) {
            foreach (Product product in products) {
                builder.AddProduct (product.apprien, product.type);

                //builder.AddProduct(product.name, product.type);
                //if (product.name != product.apprien) {
                //  builder.AddProduct (product.apprien, product.type);
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
                Product product = products [i];
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
        public void OnProcessPurchase(MonoBehaviour receiver, PurchaseEventArgs e) {
            receiver.StartCoroutine (PostReceipt(receiver, e));
        }


		/// <summary>
		/// Raises the apprien initialized event.
		/// </summary>
		/// <param name="products">Products.</param>
		public void OnApprienInitialized(List<Product> products) {
			ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
			this.AddProducts (builder, products);
			UnityPurchasing.Initialize (this, builder);
		}

		/// <summary>
		/// Called when Unity IAP is ready to make purchases.
		/// </summary>
		public void OnInitialized (IStoreController controller, IExtensionProvider extensions) {
			this.controller = controller;
			this.extensions = extensions;

			Apprien.OnStoreInitialized (controller, products);
		}

		/// <summary>
		/// Called when Unity IAP encounters an unrecoverable initialization error.
		///
		/// Note that this will not be called if Internet is unavailable; Unity IAP
		/// will attempt initialization until it becomes available.
		/// </summary>
		public void OnInitializeFailed (InitializationFailureReason error) {
			Debug.Log ("Initialize failed");
		}

		/// <summary>
		/// Called when a purchase completes.
		///
		/// May be called at any time after OnInitialized().
		/// </summary>
		public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e) {
			string receipt = e.purchasedProduct.receipt;
			this.OnProcessPurchase (this, e);
			return PurchaseProcessingResult.Complete;
		}

		/// <summary>
		/// Raises the apprien post receipt success event.
		/// </summary>
		/// <param name="text">Text.</param>
		protected virtual void OnApprienPostReceiptSuccess(string text) {
			Debug.Log ("OnApprienPostReceiptSuccess: " + text);
		}

		/// <summary>
		/// Raises the apprien post receipt failed event.
		/// </summary>
		/// <param name="text">Text.</param>
		protected virtual void OnApprienPostReceiptFailed(string text) {
			Debug.Log ("OnApprienPostReceiptFailed: " + text);
		}

		/// <summary>
		/// Called when a purchase fails.
		/// </summary>
		public void OnPurchaseFailed (Product i, PurchaseFailureReason p) {
			Debug.Log ("Purchase failed");
		}
	}
}
