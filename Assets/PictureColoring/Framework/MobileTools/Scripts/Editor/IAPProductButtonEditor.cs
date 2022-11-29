using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BBG
{
	[CustomEditor(typeof(IAPProductButton))]
	public class IAPProductButtonEditor : Editor
	{
		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			bool iapEnabled = false;

			#if BBG_MT_IAP
			iapEnabled = BBG.MobileTools.IAPSettings.IsIAPEnabled;
			#endif

			if (!iapEnabled)
			{
				EditorGUILayout.HelpBox("IAP is not enabled.", MessageType.Warning);
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("productId"));

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("priceText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("dependantElement"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("targetElement"));

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion
	}
}
