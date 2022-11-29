using BBG.MobileTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.PictureColoring
{
	public class ColorList : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private ColorListItem	colorListItemPrefab	= null;
		[SerializeField] private Transform		colorListContainer	= null;

		#endregion

		#region Member Variables

		private ObjectPool			colorListItemPool;
		private List<ColorListItem>	colorListItems;

		#endregion

		#region Properties

		public int					SelectedColorIndex	{ get; set; }
		public System.Action<int>	OnColorSelected		{ get; set; }

		RectTransform RectTransform { get { return transform as RectTransform; } }

		public ObjectPool ColorListItemPool { get { return colorListItemPool; } }

		#endregion

		#region Public Methods

		public void Initialize()
		{
			colorListItemPool	= new ObjectPool(colorListItemPrefab.gameObject, 1, colorListContainer);
			colorListItems		= new List<ColorListItem>();

			MobileAdsManager.Instance.OnBannerAdShown -= RecalculatePosition;
			MobileAdsManager.Instance.OnBannerAdShown += RecalculatePosition;
		}

		public void Setup(int selectedColorIndex)
		{
			Clear();

			LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

			if (activeLevelData != null)
			{
				// Setup each color list item
				for (int i = 0; i < activeLevelData.LevelFileData.colors.Count; i++)
				{
					Color			color			= activeLevelData.LevelFileData.colors[i];
					ColorListItem	colorListItem	= colorListItemPool.GetObject<ColorListItem>();

					//colorListItems.Add(colorListItem);
					colorListItems.Insert(i, colorListItem);

					colorListItem.transform.SetSiblingIndex(i);

					colorListItem.Setup(color, i + 1, this);
					colorListItem.SetSelected(i == selectedColorIndex);

					CheckCompleted(i, true);

					colorListItem.Index				= i;
					colorListItem.OnListItemClicked	= OnColorListItemClicked;
				}

				RecalculateIndexes(activeLevelData);
			}

			SelectedColorIndex = selectedColorIndex;

			//RecalculatePosition();

			/*if(		MobileAdsManager.Instance.AreBannerAdsEnabled && 
				(	MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Showing  ||
					MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Shown    ||
					MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Loaded
				)
			  )
			{
				RectTransform.sizeDelta = new Vector2(0, 400);
				RectTransform.anchoredPosition = new Vector2(0f, 0f);
			}
			else
			{
				RectTransform.sizeDelta = new Vector2(0, 180);
				RectTransform.anchoredPosition = Vector2.zero;
			}*/
		}

		void RecalculatePosition()
		{
			print(MobileAdsManager.Instance.GetBannerHeightInPixels());

			if(		MobileAdsManager.Instance.AreBannerAdsEnabled &&
				(	MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Showing ||
					MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Shown ||
					MobileAdsManager.Instance.BannerAdHandler.BannerAdState == AdNetworkHandler.AdState.Loaded
				)
			  )
			{
				//RectTransform.sizeDelta = new Vector2(0, 400);
				RectTransform.sizeDelta = new Vector2(0, 180 + MobileAdsManager.Instance.GetBannerHeightInPixels() + 20);
				RectTransform.anchoredPosition = new Vector2(0f, 0f);
			}
			else
			{
				RectTransform.sizeDelta = new Vector2(0, 180);
				RectTransform.anchoredPosition = Vector2.zero;
			}
		}

		public void Clear()
		{
			// Clear the list
			colorListItemPool.ReturnAllObjectsToPool();
			colorListItems.Clear();
		}

		/// <summary>
		/// Checks if the color region is completed and if so sets the ColorListItem as completed
		/// </summary>
		public void CheckCompleted(int colorIndex, bool _disappeadImmediately = false)
		{
			LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

			if (activeLevelData != null && colorIndex < colorListItems.Count && activeLevelData.IsColorComplete(colorIndex))
			{
				colorListItems[colorIndex].SetCompleted(_disappeadImmediately);
			}
		}

		#endregion

		#region Private Methods

		public void RecalculateIndexes( LevelData activeLevelData)
		{
			int realIndex = -1;

			for(int i = 0; i < colorListContainer.childCount; i++)
			{
				if(!colorListContainer.GetChild(i).gameObject.activeSelf) return;

				//if(!activeLevelData.IsColorComplete(i))
				//{
				realIndex++;

				if(colorListItems[i].Index == realIndex) continue;

				colorListContainer.GetChild(i).GetComponent<ColorListItem>().Index = realIndex;

				var item = colorListItems[i];

				colorListItems.RemoveAt(colorListItems[i].Index);

				colorListItems.Insert(realIndex, item);

				//}
			}
		}

		private void OnColorListItemClicked(int index, object data)
		{
			if (index != SelectedColorIndex)
			{
				// Set the current selected ColorListItem to un-selected and select the new one
				colorListItems[SelectedColorIndex].SetSelected(false);
				colorListItems[index].SetSelected(true);

				SelectedColorIndex = index;

				HintAnimationManager.cancelAnimation = true;

				OnColorSelected(index);
			}
		}

		#endregion
	}
}
