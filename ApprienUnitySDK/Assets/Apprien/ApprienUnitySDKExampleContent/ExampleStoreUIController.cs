using System.Collections;
using System.Collections.Generic;
using Apprien;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

// Partial code from https://unity3d.com/learn/tutorials/topics/ads-analytics/integrating-unity-iap-your-game
namespace ApprienUnitySDK.ExampleProject
{
	public class ExampleStoreUIController : MonoBehaviour, IStoreListener
	{
		private ApprienManager _apprienManager;

		[SerializeField]
		private ApprienConnection ApprienConnection;

		[SerializeField]
		private Text[] StandardPriceTexts;

		[SerializeField]
		private Text[] StandardPriceSKUTexts;
 
		[SerializeField]
		private Text[] ApprienPriceTexts;

		[SerializeField]
		private Text[] ApprienPriceSKUTexts;

		private IStoreController _storeController;
		private IExtensionProvider _extensionProvider;

		private ConfigurationBuilder _builder;
		private ApprienProduct[] _apprienProducts;

		void Awake()
		{
			if (ApprienConnection == null || ApprienConnection.Token.Length == 0)
			{
				Debug.LogWarning("Token not provided for Apprien SDK. Unable to configure dynamic prices.");
			}

			// Create ApprienProducts from the IAP Catalog
			var catalogFile = Resources.Load<TextAsset>("ApprienIAPProductCatalog");
			var catalog = ProductCatalog.FromTextAsset(catalogFile);
			_apprienProducts = ApprienProduct.FromIAPCatalog(catalog);

			// Initialize Unity IAP configuration builder
			_builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Platform specific integration type for the manager.
			ApprienIntegrationType integrationType;

#if UNITY_IOS
			integrationType = ApprienIntegrationType.AppleAppStore;
#else
			integrationType = ApprienIntegrationType.GooglePlayStore;
#endif

			// Package name. Usually Application.identifier
			var packageName = Application.identifier;

			_apprienManager = new ApprienManager(
				Application.identifier,
				integrationType,
				ApprienConnection.Token
			);

			Debug.Log("Checking Apprien status...");
			// Test the connection. Optional
			StartCoroutine(
				_apprienManager.TestConnection(
					(connected, valid) =>
					{
						Debug.Log("Apprien is reachable: " + connected);
						Debug.Log("Token is valid: " + valid);
					})
			);

			// Add standard IAP ids, so that there is always a fallback if Apprien variants cannot be fetched
			foreach (var product in _apprienProducts)
			{
				_builder.AddProduct(product.BaseIAPId, product.ProductType);
			}

			FetchPrices();
		}

		/// <summary>
		/// Fetch Apprien variant IAP ids and re-initialize UnityPurchasing
		/// </summary>
		private void FetchPrices()
		{
			// Update the products with Apprien IAP ids
			StartCoroutine(
				_apprienManager.FetchApprienPrices(
					_apprienProducts,
					() =>
					{
						// The products will now contain updated IAP ids. Add them to the product builder
						// If the connection failed or the variants were not fetched for some reason
						// this will add duplicates to the builder, which will ignore them safely.
						foreach (var product in _apprienProducts)
						{
							// Apprien variant IAP id. If connection failed, the variant IAP id
							// defaults to the base IAP id
							_builder.AddProduct(product.ApprienVariantIAPId, product.ProductType);
						}

						// Initialize UnityPurchasing with the fetched IAP ids
						UnityPurchasing.Initialize(this, _builder);
					}
				)
			);

			// Update standard IAP ids on the UI
			for (var i = 0; i < _apprienProducts.Length; i++)
			{
				StandardPriceSKUTexts[i].text = _apprienProducts[i].BaseIAPId;
			}
		}

		/// <summary>
		/// Called after Apprien IAP ids have replaced the base IAP ids, refreshes the prices of products
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="extensions"></param>
		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			// Overall Purchasing system, configured with products for this application.
			_storeController = controller;

			// Store specific subsystem, for accessing device-specific store features.
			_extensionProvider = extensions;

			// Refresh prices to UI
			RefreshUI();
		}

		/// <summary>
		/// Called when Unity Purchasing encounters an unrecoverable initialization error.
		///
		/// Note that this will not be called if Internet is unavailable; Unity IAP
		/// will attempt initialization until it becomes available.
		/// </summary>
		public void OnInitializeFailed(InitializationFailureReason error)
		{
			Debug.Log("Initialize failed");
		}

		/// <summary>
		/// Called when a purchase completes.
		/// 
		/// May be called at any time after OnInitialized().
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
		{
			Debug.LogError("ProcessPurchase not implemented.");
			return PurchaseProcessingResult.Pending;
		}

		/// <summary>
		/// Called when a purchase fails.
		/// </summary>
		public void OnPurchaseFailed(UnityEngine.Purchasing.Product i, PurchaseFailureReason p)
		{
			Debug.Log("OnPurchaseFailed not implemented.");
		}

		/// <summary>
		/// Get the price for the given IAP from the Store
		/// </summary>
		public void RefreshUI()
		{
			var iapProducts = _storeController.products;
			for (var i = 0; i < _apprienProducts.Length; i++)
			{
				var apprienProduct = _apprienProducts[i];
				var iapApprienProduct = iapProducts.WithID(apprienProduct.ApprienVariantIAPId);
				var iapStandardProduct = iapProducts.WithID(apprienProduct.BaseIAPId);

				var apprienPrice = iapApprienProduct.metadata.localizedPriceString;
				var standardPrice = iapStandardProduct.metadata.localizedPriceString;

				StandardPriceTexts[i].text = standardPrice;
				ApprienPriceTexts[i].text = apprienPrice;

				// Update the Apprien IAP ids to text

				ApprienPriceSKUTexts[i].text = apprienProduct.ApprienVariantIAPId;
			}
		}

		public void RefreshButtonPressed()
		{
			// Reset prices
			for (var i = 0; i < _apprienProducts.Length; i++)
			{
				ApprienPriceSKUTexts[i].text = "IAP id";
				StandardPriceSKUTexts[i].text = "IAP id";

				ApprienPriceTexts[i].text = "Loading...";
				StandardPriceTexts[i].text = "Loading...";
			}

			FetchPrices();
		}
	}
}