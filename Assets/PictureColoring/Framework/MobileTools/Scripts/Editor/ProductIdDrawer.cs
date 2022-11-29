using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if BBG_MT_IAP
using BBG.MobileTools;
#endif

namespace BBG
{
	[CustomPropertyDrawer(typeof(ProductId))]
	public class ProductIdDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawIapProductId(position, property.FindPropertyRelative("productId"), true);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return DrawIapProductId(new Rect(), property.FindPropertyRelative("productId"), false);
		}

		public float DrawIapProductId(Rect position, SerializedProperty productIdProp, bool draw)
		{
			float totalHeight = 0;
		
			position.height = EditorGUIUtility.singleLineHeight;
			totalHeight += position.height;
			if (draw) EditorGUI.PropertyField(position, productIdProp);
			position.y += position.height;

			#if BBG_MT_IAP
			List<string> productIds = new List<string>();

			// Gather all the prodcut ids so we can display then in a dropdown
			for (int i = 0; i < IAPSettings.Instance.productInfos.Count; i++)
			{
				string productId = IAPSettings.Instance.productInfos[i].productId;

				if (!string.IsNullOrEmpty(productId))
				{
					productIds.Add(productId);
				}
			}

			int curSelectedIndex = productIds.IndexOf(productIdProp.stringValue);

			if (curSelectedIndex == -1)
			{
				position.height = EditorGUIUtility.singleLineHeight * 2;
				totalHeight += position.height;
				if (draw) EditorGUI.HelpBox(position, "This Product Id does not exist in the IAP Settings window.", MessageType.Warning);
				position.y += position.height;
			}
		
			if (productIds.Count == 0)
			{
				position.height = EditorGUIUtility.singleLineHeight * 2;
				totalHeight += position.height;
				if (draw) EditorGUI.HelpBox(position, "There are no Product Ids created in the IAP Settings window.", MessageType.Warning);
				position.y += position.height;
			}
			else
			{
				if (curSelectedIndex == -1)
				{
					productIds.Insert(0, "<select>");
					curSelectedIndex = 0;
				}

				position.height = EditorGUIUtility.singleLineHeight;
				totalHeight += position.height;
				if (draw)
				{
					int selectedIdIndex = EditorGUI.Popup(position, "Product Id", curSelectedIndex, productIds.ToArray());

					if (selectedIdIndex != curSelectedIndex)
					{
						productIdProp.stringValue = productIds[selectedIdIndex];
					}
				}
				position.y += position.height;
			}
			#endif

			return totalHeight;
		}
	}
}