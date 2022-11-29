using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace BBG.PictureColoring
{
	public class ColorListItem : ClickableListItem
	{
		#region Inspector Variables

		[SerializeField] private Image 		colorImage		= null;
		[SerializeField] private Text		numberText		= null;
		[SerializeField] private GameObject	completedObj	= null;
		[SerializeField] private GameObject	selectedObj		= null;
		[SerializeField] private ColorList parentContainer = null;

		#endregion

		#region Public Methods

		public void Setup(Color color, int number, ColorList parent)
		{
			colorImage.color	= color;
			numberText.text		= number.ToString();

			numberText.enabled = true;

			selectedObj.SetActive(false);
			completedObj.SetActive(false);

			parentContainer = parent;
		}

		public void SetSelected(bool isSelected)
		{
			selectedObj.SetActive(isSelected);
		}

		public void SetCompleted( bool _disappeadImmediately = false)
		{
			numberText.enabled = false;
			completedObj.SetActive(true);

			if(_disappeadImmediately)
			{
				gameObject.SetActive(false);
				gameObject.transform.SetAsLastSibling();
			}
			else
				DisappearAfterCompleted();
		}

		void DisappearAfterCompleted()
		{
			RectTransform RectT = transform as RectTransform;

			RectT.DOScale(Vector3.zero, 0.5f).SetEase(Ease.Linear).OnComplete(delegate() { 
			
				gameObject.SetActive(false);

				gameObject.transform.SetAsLastSibling();

				RectT.localScale = Vector3.one;
			});
		}

		#endregion
	}
}
