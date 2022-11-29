using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class PictureCreator : MonoBehaviour
	{
		#region Member Variables

		private bool isInitialized;

		[SerializeField] private Image				backgroundImage;
		private Image								lineImage;
		private List<PictureImage>					pictureImages;
		private int									padding;
		private LevelFileData levelFileData;
		string levelId;

		#endregion // Member Variables

		#region Properties

		public RectTransform RectT { get { return transform as RectTransform; } }

		#endregion // Properties

		#region Public Methods
		
		public void Setup(string _levelId, Material selectedRegionMaterial = null, int padding = 0)
		{
			this.padding = padding;

			if (!isInitialized)
			{
				Initialize();
			}

			levelId = _levelId;

			Clear();

			lineImage.enabled = true;

			levelFileData = LoadManager.Instance.GetLevelFileData(levelId);

			int i;

			List<Region>[] atlasRegions = new List<Region>[levelFileData.atlases];
			for (i = 0; i < atlasRegions.Length; i++)
			{
				atlasRegions[i] = new List<Region>();
			}

			for (i = 0; i < levelFileData.regions.Count; i++)
			{
				Region region = levelFileData.regions[i];
				atlasRegions[region.atlasIndex].Add(region);
			}

			for (i = 0; i < atlasRegions.Length; i++)
			{
				if (i == pictureImages.Count)
				{
					pictureImages.Add(CreateImage());
				}

				var pictureImage = pictureImages[i];

				pictureImage.enabled = true;
				pictureImage.sprite = LoadManager.Instance.GetRegionSprite(levelId, i);
				pictureImage.material = selectedRegionMaterial;
				pictureImage.Setup(atlasRegions[i], levelId);
				pictureImage.SetSelectedIndex(-1);

				//if(i == 0 && backgroundImage != null /*&& activeLevelData.IsColorComplete(colorIndex)*/ /*&& LoadManager.Instance.GetLevelFileData(levelId)*/)
				/*{
					if( GameManager.Instance.AllLevels.Find(x => x.Id == levelId).IsColorComplete(0))
						backgroundImage.color = levelFileData.colors[pictureImage.GetRegions()[0].colorIndex];
				}*/
			}

			for (; i < pictureImages.Count; i++)
			{
				pictureImages[i].enabled = false;
			}
		}

		public void SetSelectedColor(int colorIndex)
		{
			if (!isInitialized) return;

			for (int i = 0; i < pictureImages.Count; i++)
			{
				pictureImages[i].SetSelectedIndex(colorIndex);
			}
		}

		public void Clear()
		{
			if (!isInitialized) return;

			for (int i = 0; i < pictureImages.Count; i++)
			{
				pictureImages[i].Clear();
				pictureImages[i].enabled = false;
			}

			lineImage.enabled = false;
		}

		public void RegionColored()
		{
			if (!isInitialized) return;

			RefreshImages();
		}

		public void RefreshImages()
		{
			if (!isInitialized) return;

			for (int i = 0; i < pictureImages.Count; i++)
			{
				pictureImages[i].SetAllDirty();

				/*if(i == 0 && backgroundImage != null )
				{
					levelFileData = LoadManager.Instance.GetLevelFileData(levelId);

					if(GameManager.Instance.AllLevels.Find(x => x.Id == levelId).IsColorComplete(0))
						//backgroundImage.color = levelFileData.colors[atlasRegions[i][0].colorIndex];
						backgroundImage.color = levelFileData.colors[pictureImages[i].GetRegions()[0].colorIndex];
				}*/
			}
		}

		#endregion // Public Methods

		#region Private Methods

		private void Initialize()
		{
			pictureImages = new List<PictureImage>();

			lineImage = gameObject.GetComponent<Image>();

			if (lineImage == null)
			{
				GameObject obj = new GameObject("black_line_image", typeof(RectTransform));
				obj.transform.SetParent(transform, false);

				lineImage = obj.AddComponent<Image>();
				lineImage.color = Color.black;

				lineImage.rectTransform.anchorMin = Vector2.zero;
				lineImage.rectTransform.anchorMax = Vector2.one;
				lineImage.rectTransform.offsetMin = new Vector2(padding, padding);
				lineImage.rectTransform.offsetMax = new Vector2(-padding, -padding);
			}

			isInitialized = true;
		}

		private PictureImage CreateImage()
		{
			GameObject obj = new GameObject("picture_image", typeof(RectTransform));

			obj.transform.SetParent(transform, false);

			RectTransform rectT = obj.transform as RectTransform;

			rectT.anchorMin = Vector2.zero;
			rectT.anchorMax = Vector2.one;
			rectT.offsetMin = Vector2.zero;
			rectT.offsetMax = Vector2.zero;
			rectT.pivot = Vector2.zero;

			return obj.AddComponent<PictureImage>();
		}

		#endregion // Private Methods
	}
}
