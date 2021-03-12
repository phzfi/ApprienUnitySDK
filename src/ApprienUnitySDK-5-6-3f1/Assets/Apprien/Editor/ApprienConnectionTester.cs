using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Apprien
{
    [CustomEditor(typeof(ApprienConnection))]
    public class ApprienConnectionTester : Editor
    {
        private SerializedProperty _apprienConnection;

        private IEnumerator _initializeFetch;
        private IEnumerator _pricesFetch;

        private ApprienManager _apprienManager;

        private bool _fetchingStatus;
        private bool _connectionCheckPressed;
        private bool _connectionOK;
        private bool _tokenOK;
        private bool _onlyActiveProducts;
        private bool _fetchingProducts;
        private bool _anyProducts;
        private List<ApprienProduct> _fetchedProducts;
        private string _catalogResourceName = "IAPProductCatalog";

        void OnEnable()
        {
            _apprienConnection = serializedObject.FindProperty("Token");

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
                ApprienIntegrationType.GooglePlayStore,
                "" // The token will be set via the Editor UI button press
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
                    _initializeFetch = null;
                }
            }

            if (_pricesFetch != null)
            {
                if (!_pricesFetch.MoveNext())
                {
                    _pricesFetch = null;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Display the Inspector properties defined in the ApprienConnection ScriptableObject, i.e. the token
            DrawDefaultInspector();

            if (_apprienConnection.stringValue.Length == 0)
            {
                EditorGUILayout.HelpBox("Provide the authentication token into the field above before testing the connection. You should see a list of all products loaded into Apprien for the game.", MessageType.Info);
                return;
            }

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

                // Refresh the token to the manager before testing connection
                var token = _apprienConnection.stringValue;
                _apprienManager.Token = token;

                _initializeFetch = _apprienManager.TestConnection((available, valid) =>
                {
                    _fetchingStatus = false;
                    _connectionCheckPressed = true;
                    _connectionOK = available;
                    _tokenOK = valid;
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
                    "  Connection OK" :
                    "  Unable to connecto to Apprien.");

                EditorGUILayout.LabelField(_tokenOK ?
                    "  Token OK" :
                    "  Apprien Token is invalid.");
            }

            EditorGUI.BeginDisabledGroup(_connectionOK == false || _tokenOK == false);
            EditorGUILayout.HelpBox("After testing the connection, click below to fetch all Apprien generated IAP variants for products defined in the default IAP Catalog. Make sure the correct package name is set in Player settings, i.e. your com.company.product identifier.", MessageType.Info);

            // Enable after active/inactive products are supported in Apprien
            //_onlyActiveProducts = EditorGUILayout.ToggleLeft("Fetch only active products", _onlyActiveProducts);

            _catalogResourceName = EditorGUILayout.TextField("Catalog resource name", _catalogResourceName);

            if (GUILayout.Button("Test fetching Apprien-generated products"))
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
                        _pricesFetch = _apprienManager.FetchApprienPrices(products, () =>
                        {
                            _fetchingProducts = false;
                            _fetchedProducts = products.ToList();
                        });
                    }
                }
                else
                {
                    _anyProducts = false;
                }

            }

            if (!_anyProducts)
            {
                EditorGUILayout.LabelField("No products are defined in the default IAP Catalog.");
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
                    EditorGUILayout.LabelField("  IAP id: " + product.BaseIAPId);
                    EditorGUILayout.LabelField("  Apprien variant: " + product.ApprienVariantIAPId);
                    EditorGUILayout.Space();
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}