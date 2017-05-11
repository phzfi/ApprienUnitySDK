using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace ApprienSDK {

	/// <summary>
	/// Apprien store manager.
	/// </summary>
	public class ApprienStoreManager : MonoBehaviour, IStoreListener {

		#if UNITY_EDITOR
		public bool editorToggle;
		#endif

		protected IStoreController controller;
		protected IExtensionProvider extensions;

		/// <summary>
		/// The products.
		/// </summary>
		public List<Apprien.Product> products = new List<Apprien.Product>();

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
		/// Raises the apprien initialized event.
		/// </summary>
		/// <param name="products">Products.</param>
		public void OnApprienInitialized(List<Apprien.Product> products) {
			ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
			Apprien.AddProducts (builder, products);
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
			Apprien.OnProcessPurchase (this, e);
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
