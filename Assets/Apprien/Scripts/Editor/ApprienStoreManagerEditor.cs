using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEditor;

namespace Apprien.Unity.SDK {

    /// <summary>
    /// Apprien Unity Store Manager Editor
    /// 
    /// If you are using some other IAP manager, you don't need this file.
    /// </summary>
    [CustomEditor(typeof(ApprienStoreManager))]
	public class ApprienStoreManagerEditor : Editor {

		protected static GUIStyle styleRichText = new GUIStyle ();

		public override void OnInspectorGUI() {
			ApprienStoreManager apprien = (ApprienStoreManager)target;
			DrawInspectorGUI (apprien);
		}

		public static void DrawInspectorGUI(ApprienStoreManager apprien) {
			styleRichText.richText = true;
			EditorGUI.BeginChangeCheck ();

			EditorGUILayout.BeginVertical ("Box");
			apprien.editorToggle = EditorGUILayout.BeginToggleGroup ("Apprien Store Manager", apprien.editorToggle);

			if (apprien.editorToggle) {

				EditorGUILayout.BeginVertical ("Box");
				EditorGUILayout.HelpBox ("Apprien app id and token", MessageType.None);
				apprien.appid = EditorGUILayout.TextField ("App Id", apprien.appid);
				apprien.token = EditorGUILayout.TextField ("Token", apprien.token);
				EditorGUILayout.EndVertical ();

				EditorGUILayout.BeginVertical ("Box");
				EditorGUILayout.HelpBox ("Reference Products", MessageType.None);
				List<Apprien.Product> products = apprien.products;
				int size = EditorGUILayout.IntField("Count", products.Count);
				while (size > products.Count) {
					products.Add (new Apprien.Product (""));
				}
				while (size < products.Count) {
					products.RemoveAt (products.Count - 1);
				}
				for (int i = 0; i < size; i++) {
					Apprien.Product product = products [i];
					EditorGUILayout.BeginVertical ("Box");
					product.name = EditorGUILayout.TextField("Name", product.name);
					product.type = (ProductType) EditorGUILayout.EnumPopup("Type", product.type);
					EditorGUILayout.EndVertical ();
					products [i] = product;
				}
				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.EndToggleGroup ();
			EditorGUILayout.EndVertical ();

			if (EditorGUI.EndChangeCheck () && !EditorApplication.isPlaying) {
				EditorUtility.SetDirty (apprien);
			}

		}
	}
}