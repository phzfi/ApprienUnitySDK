using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Apprien
{
    [CustomEditor(typeof(ApprienConnectionTester))]
    public class ApprienConnectionTesterEditor : Editor
    {
        private IEnumerator _initializeFetch;
        private IEnumerator _pricesFetch;

        private ApprienManager _apprienManager;

        private bool _fetchingStatus;
        private bool _connectionCheckPressed;
        private bool _connectionOK;
        private bool _onlyActiveProducts;
        private bool _fetchingProducts;
        private bool _anyProducts;
        private List<ApprienProduct> _fetchedProducts;
        private string _catalogResourceName = "ApprienIAPProductCatalog";

        void OnEnable()
        {
            // Add the update hook
            EditorApplication.update += Update;
        }
        void OnDisable()
        {
            // Remove the update hook
            EditorApplication.update -= Update;
        }

        void Awake()
        {
            _apprienManager = new ApprienManager(
                Application.identifier,
                ApprienIntegrationType.GooglePlayStore
            );
            _fetchedProducts = new List<ApprienProduct>();

            _anyProducts = true;
        }

        void Update()
        {
            // Update the IEnumerator until it ends
            if (_initializeFetch != null)
            {
                if (!_initializeFetch.MoveNext())
                {
                    Repaint();
                    _initializeFetch = null;
                }
            }

            if (_pricesFetch != null)
            {
                if (!_pricesFetch.MoveNext())
                {
                    Repaint();
                    _pricesFetch = null;
                }
            }
        }

        private void FetchPrices()
        {
            _fetchingProducts = true;
            _fetchedProducts.Clear();

            var catalogFile = Resources.Load<TextAsset>(_catalogResourceName);
            if (catalogFile != null)
            {
                var catalog = ProductCatalog.FromTextAsset(catalogFile);
                var products = ApprienProduct.FromIAPCatalog(catalog);

                if (products.Length == 0)
                {
                    _anyProducts = false;
                }
                else
                {
                    _anyProducts = true;
                    _pricesFetch = FetchPricesCoroutine(products);
                }
            }
            else
            {
                _anyProducts = false;
            }
        }

        private IEnumerator FetchPricesCoroutine(ApprienProduct[] products)
        {
            var fetch = _apprienManager.FetchApprienPrices(products);
            while (fetch.MoveNext())
            {
                yield return null;
            }

            _fetchingProducts = false;
            _fetchedProducts = products.ToList();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (_apprienManager == null)
            {
                EditorGUILayout.HelpBox("The assembly containing this editor script was recompiled. Please deselect and reselect the asset.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("API Integration", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Connection"))
            {
                _connectionCheckPressed = false;
                _fetchingStatus = true;

                _initializeFetch = TestConnection((available) =>
                {
                    _fetchingStatus = false;
                    _connectionCheckPressed = true;
                    _connectionOK = available;
                });
            }

            EditorGUILayout.LabelField("Status:");
            if (_fetchingStatus)
            {
                EditorGUILayout.LabelField("  Loading...");
            }

            if (_connectionCheckPressed)
            {
                // Display connection information
                EditorGUILayout.LabelField(_connectionOK ?
                    "  Apprien API connection is OK." :
                    "  Apprien API connection failed.");
            }

            EditorGUI.BeginDisabledGroup(_connectionOK == false);
            EditorGUILayout.HelpBox("After testing the connection, click below to fetch all Apprien generated IAP variants for products defined in the default IAP Catalog. Make sure the correct package name is set in Player settings, i.e. your com.company.product identifier.", MessageType.Info);

            // Enable after active/inactive products are supported in Apprien
            //_onlyActiveProducts = EditorGUILayout.ToggleLeft("Fetch only active products", _onlyActiveProducts);

            _catalogResourceName = EditorGUILayout.TextField("Catalog resource name", _catalogResourceName);

            if (GUILayout.Button("Test fetching Apprien-generated products"))
            {
                FetchPrices();
            }

            if (!_anyProducts)
            {
                // catalog file does not exist
                EditorGUILayout.LabelField("Could not load catalog file: " + _catalogResourceName);
                return;
            }

            EditorGUILayout.LabelField("Products:");

            if (_fetchingProducts)
            {
                EditorGUILayout.LabelField("  Loading...");
            }
            else
            {
                foreach (var product in _fetchedProducts)
                {
                    EditorGUILayout.LabelField("  Base Product ID: " + product.BaseIAPId);
                    EditorGUILayout.LabelField("  Apprien variant ID: " + product.ApprienVariantIAPId);
                    EditorGUILayout.Space();
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Perform an availability check for the Apprien service
        /// </summary>
        /// <param name="callback">The parameter is true if Apprien is reachable</param>
        /// <returns>Returns an IEnumerator that can be forwarded manually or passed to StartCoroutine</returns>
        public IEnumerator TestConnection(Action<bool> callback)
        {
            // Check service status
            var statusCheck = _apprienManager.CheckServiceStatus();

            while (statusCheck.MoveNext())
            {
                yield return null;
            }

            // The request IEnumerator will resolve to a boolean value in the end
            // Inform the calling component that Apprien is online
            if (callback != null)
            {
                callback((bool)statusCheck.Current);
            }
        }
    }
}
