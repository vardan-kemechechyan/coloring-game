using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class CategoryListItem : ClickableListItem
	{
		#region Inspector Variables

		[SerializeField] private Text	categoryText	= null;
		[SerializeField] private Image	underlineObject	= null;
		[SerializeField] private Button	buttonCategory	= null;
		[SerializeField] private Color	normalColor		= Color.white;
		[SerializeField] private Color	selectedColor	= Color.white;

		#endregion

		#region Public Methods

		public void Setup(string displayText)
		{
			categoryText.text = displayText;
		}

		public string GetCategoryText() { return categoryText.text; }

		public void SetSelected(bool isSelected)
		{
			categoryText.color						= isSelected ? selectedColor : normalColor;
			underlineObject.color					= isSelected ? selectedColor : normalColor;

			var colors = buttonCategory.colors;
			
			Color c = colors.normalColor;
			c.a = isSelected ? 1f : 0f;
			colors.normalColor = c;
			
			buttonCategory.colors = colors;	

			underlineObject.gameObject.SetActive(isSelected);

			var catName = new Dictionary<string, object>();
			catName.Add("category_name", categoryText.text);
			AnalyticEvents.ReportEvent("category_open", catName);
		}

		#endregion
	}
}
