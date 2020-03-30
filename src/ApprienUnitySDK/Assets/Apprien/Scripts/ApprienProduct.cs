using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Apprien
{
    /// <summary>
    /// The IAP Product for Apprien.
    /// </summary>
    [System.Serializable]
    public class ApprienProduct
    {
        /// <summary>
        /// The base product id. Apprien will fallback to this id if a variant cannot be retrieved.
        /// </summary>
        public string BaseIAPId;

        /// <summary>
        /// Unity Purchasing -defined ProductType. e.g. Consumable, Non-Consumable etc.
        /// </summary>
        public ProductType ProductType;

        /// <summary>
        /// Apprien creates variants of the base IAP id, e.g.
        /// z_iapBaseName.apprien_1990_v34f
        /// where 1990 is e.g. 1990 USD cents and the last 4 symbols are a unique hash.
        /// The variants start with "z_" to sort them last and distiguish them
        /// easily from the base IAP ids
        /// </summary>
        [NonSerialized]
        public string ApprienVariantIAPId = "";

        /// <summary>
        /// Optional. If defined, the IAPId only applies to the given store. If this product exists in multiple stores,
        /// multiple ApprienProduct objects are required.
        /// The string is Unity's identifier for stores, e.g. "AppleAppStore", "GooglePlay" etc.
        /// </summary>
        public string Store;

        public ApprienProduct(string baseIapId, ProductType productType)
        {
            BaseIAPId = baseIapId;
            // Defaults the variant name to the base IAP id. FetchApprienPrice will replace this if fetch succeeds
            ApprienVariantIAPId = baseIapId;
            ProductType = productType;
        }

        public ApprienProduct(Product product)
        {
            BaseIAPId = product.definition.id;
            ApprienVariantIAPId = BaseIAPId;
            ProductType = product.definition.type;
        }

        /// <summary>
        /// Creates ApprienProduct objects from the products already added to the given builder.
        /// Does not add any products to the builder.
        /// </summary>
        /// <param name="builder">Reference to a builder containing products.</param>
        /// <returns>Returns an array of Apprien Products built from the given ConfigurationBuilder object</returns>
        public static ApprienProduct[] FromConfigurationBuilder(ConfigurationBuilder builder)
        {
            var products = new ApprienProduct[builder.products.Count];
            var i = 0;
            // HashSet cannot be indexed with [i]
            foreach (var product in builder.products)
            {
                products[i++] = new ApprienProduct(product.id, product.type);
            }

            return products;
        }

        /// <summary>
        /// Convert a Unity IAP Product Catalog into ApprienProduct objects ready for fetching Apprien prices.
        /// Does not alter the catalog
        /// </summary>
        /// <param name="catalog"></param>
        /// <returns>Returns an array of Apprien Products built from the given ProductCatalog object</returns>
        public static ApprienProduct[] FromIAPCatalog(ProductCatalog catalog)
        {
            var catalogProducts = catalog.allValidProducts;
            /*
            // TODO: Get the store-specific products
            foreach (var product in catalogProducts)
            {
                Debug.Log(product.GetStoreID("GooglePlay"));
                Debug.Log(product.GetStoreID("AppleAppStore"));
            }
            */
            var products = new ApprienProduct[catalogProducts.Count];

            var i = 0;
            // ICollection cannot be indexed with [i], foreach required
            foreach (var catalogProduct in catalogProducts)
            {
                products[i++] = new ApprienProduct(catalogProduct.id, catalogProduct.type);
            }

            return products;
        }
    }

    /// <summary>
    /// Product list class used for parsing JSON.
    /// </summary>
    [System.Serializable]
    public class ApprienProductList
    {
        public List<ApprienProductListProduct> products;
    }

    [System.Serializable]
    public class ApprienProductListProduct
    {
        public string @base; // @ because base is a keyword
        public string variant;
    }
}