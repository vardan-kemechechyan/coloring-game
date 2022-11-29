using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class MainScreenSubNavButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Image	buttonIcon		= null;
		[SerializeField] private Text	buttonText		= null;
		[SerializeField] private Button buttonCategory = null;
		[SerializeField] private Color	normalColor		= Color.white;
		[SerializeField] private Color	selectedColor	= Color.white;
		[SerializeField] private string	nav_ID	= "";

		#endregion

		#region Unity Start

		private void Start()
		{
			SetIconAlpha();
		}

		#endregion

		#region Unity Methods

		public void SetSelected(bool isSelected)
		{
			//buttonIcon.color = isSelected ? selectedColor : normalColor;
			buttonText.color = isSelected ? selectedColor : normalColor;

			var colors = buttonCategory.colors;

			Color c = colors.normalColor;
			c.a = isSelected ? 1f : 0f;
			colors.normalColor = c;

			buttonCategory.colors = colors;

			SetIconAlpha();
		}

		public void SetIconAlpha()
		{
			if(nav_ID == "my_works")
			{
				List<LevelData> ld = new List<LevelData>();
				GameManager.Instance.GetMyWorksLevelDatas(out ld);

				var iconColor = buttonIcon.color;

				Color c = iconColor;
				c.a = ld.Count != 0 ? 1f : 0.5f;
				iconColor = c;

				buttonIcon.color = iconColor;

				var textColor = buttonText.color;

				Color cT = textColor;
				cT.a = ld.Count != 0 ? 1f : 0.5f;
				textColor = cT;

				buttonText.color = textColor;
			}
		}

		#endregion
	}
}
