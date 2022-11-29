using BBG.MobileTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class SelectLevelPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private RectTransform	pictureContainer	= null;
		[SerializeField] private PictureCreator	pictureCreator		= null;
		[SerializeField] private GameObject		loadingIndicator	= null;
		[SerializeField] private float			containerSize		= 0f;
		[Space]
		[SerializeField] private GameObject	continueButton		= null;
		[SerializeField] private GameObject	deleteButton		= null;
		[SerializeField] private GameObject	restartButton		= null;
		[SerializeField] private GameObject	unlockButton		= null;
		[SerializeField] private GameObject AdsIcon = null;
		[SerializeField] private GameObject MoneyIcon = null;
		[SerializeField] private Text		unlockAmountText	= null;
		[SerializeField] private Image FakeThumbnail = null;

		#endregion

		#region Member Variables

		private LevelData levelData;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			levelData = inData[0] as LevelData;

			bool isLocked = (bool)inData[1];

			bool isCompleted	= !isLocked && levelData.LevelSaveData.isCompleted;
			bool isPlaying		= !isLocked && !isCompleted && GameManager.Instance.IsLevelPlaying(levelData.Id);

			continueButton.SetActive(isPlaying);
			deleteButton.SetActive(isPlaying || isCompleted);
			restartButton.SetActive(isPlaying || isCompleted);
			unlockButton.SetActive(isLocked);

			if (isLocked)
			{
				AdsIcon.SetActive(levelData.UnlockForAds);
				MoneyIcon.SetActive(!levelData.UnlockForAds);

				if(levelData.UnlockForAds)
				{
					unlockAmountText.text = "";
				}
				else
					unlockAmountText.text = levelData.coinsToUnlock.ToString();
			}

			SetThumbnaiImage();
		}

		public override void OnHiding(bool cancelled)
		{
			base.OnHiding(cancelled);

			ReleaseLevel();
		}

		/*public override void HideWithAction(string action)
		{
			base.HideWithAction(action);
		}*/

		#endregion

		#region Private Methods

		private void SetThumbnaiImage()
		{
			//TODO: {bookmark} SetThumbnail

			bool loading = LoadManager.Instance.LoadLevel(levelData, OnLoadManagerFinished);

			if (loading)
			{
				loadingIndicator.SetActive(levelData.WebOrLocal != "web" ? true : false);

				FakeThumbnail.sprite = levelData.LevelSaveData.isCompleted ? levelData.thumbnail_completed : levelData.thumbnail_empty;
				FakeThumbnail.gameObject.SetActive(true);
			}
			else
			{
				SetupImages();

				loadingIndicator.SetActive(false);
				FakeThumbnail.gameObject.SetActive(false);
			}
		}

		private void OnLoadManagerFinished(string levelId, bool success)
		{
			if (success && levelData != null && levelId == levelData.Id)
			{
				loadingIndicator.SetActive(false);

				SetupImages();
			}
		}

		private void SetupImages()
		{
			LevelFileData levelFileData = LoadManager.Instance.GetLevelFileData(levelData.Id);

			float imageWidth	= levelFileData.imageWidth;
			float imageHeight	= levelFileData.imageHeight;
			float xScale		= imageWidth >= imageHeight ? 1f : imageWidth / imageHeight;
			float yScale		= imageWidth <= imageHeight ? 1f : imageHeight / imageWidth;

			pictureContainer.sizeDelta = new Vector2(containerSize * xScale, containerSize * yScale);

			float pictureScale = Mathf.Min(pictureContainer.rect.width / imageWidth, pictureContainer.rect.height / imageHeight, 1f);

			pictureCreator.RectT.sizeDelta	= new Vector2(imageWidth, imageHeight);
			pictureCreator.RectT.localScale	= new Vector3(pictureScale, pictureScale, 1f);

			pictureCreator.Setup(levelData.Id, padding: 2);
		}

		private void ReleaseLevel()
		{
			if (levelData != null)
			{
				LoadManager.Instance.ReleaseLevel(levelData.Id);
				levelData = null;
			}
		}

		#endregion
	}
}
