using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BBG.PictureColoring;

#if BBG_MT_ADS
using BBG.MobileTools;
#endif

namespace BBG
{
	public class NotEnoughCurrencyPopup : Popup
	{
		#region Private Members

		bool rewardForAds;

		#endregion

		#region Inspector Variables

		[Space]

		[SerializeField] private Text		titleText				= null;
		[SerializeField] private Text		messageText				= null;
		[SerializeField] private Text		rewardAdButtonText		= null;
		[SerializeField] private GameObject rewardAdButton			= null;
		[SerializeField] private GameObject storeButton				= null;
		[SerializeField] private GameObject buttonsContainer		= null;
		#if UNITY_EDITOR
		[SerializeField] private bool		testRewardAdsInEditor	= false;
		#endif

		#endregion

		#region Member Variables

		private CurrencyManager.Settings currencySettings;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			this.currencySettings = inData[0] as CurrencyManager.Settings;

			titleText.text			= currencySettings.popupTitleText;
			messageText.text		= currencySettings.popupMessageText;
			rewardAdButtonText.text = currencySettings.rewardButtonText = "WATCH AD +100";

			bool showStoreButton	= currencySettings.popupHasStoreButton;
			bool showRewardAdButton	= currencySettings.popupHasRewardAdButton;

			#if BBG_MT_ADS
			showRewardAdButton &= MobileAdsManager.Instance.AreRewardAdsEnabled;
			#endif

			storeButton.SetActive(showStoreButton);
			buttonsContainer.SetActive(showRewardAdButton || showStoreButton);

			if (showRewardAdButton)
			{
				#if BBG_MT_ADS
				rewardAdButton.SetActive(MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded);

				MobileAdsManager.Instance.OnRewardAdLoaded	+= OnRewardAdLoaded;
				MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
				#endif
			}
			else
			{
				rewardAdButton.SetActive(false);
			}

			#if UNITY_EDITOR
			if (testRewardAdsInEditor && currencySettings.popupHasRewardAdButton)
			{
				rewardAdButton.SetActive(true);
			}
			#endif
		}

		public override void OnHiding(bool cancelled)
		{
			base.OnHiding(cancelled);

			#if BBG_MT_ADS
			MobileAdsManager.Instance.OnRewardAdLoaded	-= OnRewardAdLoaded;
			MobileAdsManager.Instance.OnAdsRemoved		-= OnAdsRemoved;
			#endif
		}

		public void OnRewardAdButtonClick()
		{
			/*#if UNITY_EDITOR
			if (testRewardAdsInEditor)
			{
				OnRewardAdGranted();
				Hide(false);
				return;
			}
			#endif*/

			#if BBG_MT_ADS
			if (MobileAdsManager.Instance.RewardAdState != AdNetworkHandler.AdState.Loaded)
			{
				rewardAdButton.SetActive(false);

				Debug.LogError("[NotEnoughCurrencyPopup] The reward button was clicked but there is no ad loaded to show.");

				return;
			}

			if(GameManager.Instance.TryingToUnlockLevelData != null )
			{
				rewardForAds = GameManager.Instance.TryingToUnlockLevelData.UnlockForAds;
			}

			MobileAdsManager.Instance.ShowRewardAd(OnRewardAdClosed, OnRewardAdGranted);
			#endif

			Hide(false);
		}

		#endregion

		#region Private Methods

		private void OnRewardAdLoaded()
		{
			rewardAdButton.SetActive(true);
		}

		private void OnRewardAdClosed()
		{
			rewardAdButton.SetActive(false);
		}

		private void OnRewardAdGranted()
		{
			if(!rewardForAds)
			{
				currencySettings.rewardAdGrantedPopupMessage = "You have been awarded\n\nFREE 100";

				CurrencyManager.Instance.Give(currencySettings.rewardCurrencyId, currencySettings.rewardAmount);

				object[] popupData =
				{
					currencySettings.rewardAdGrantedPopupTitle,
					currencySettings.rewardAdGrantedPopupMessage
				};

				PopupManager.Instance.Show(currencySettings.rewardAdGrantedPopupId, popupData);
			}
			else
			{
				currencySettings.rewardAdGrantedPopupMessage = "Congrats! Level has been unlocked!";

				object[] popupData =
				{
					"Level",
					"Congrats! Level has been unlocked!"
				};

				PopupManager.Instance.Show(currencySettings.rewardAdGrantedPopupId, popupData);

				if(GameManager.Instance.TryingToUnlockLevelData != null)
					GameManager.Instance.UnlockAndStart(GameManager.Instance.TryingToUnlockLevelData);
			}
		}

		private void OnAdsRemoved()
		{
			#if BBG_MT_ADS
			MobileAdsManager.Instance.OnRewardAdLoaded -= OnRewardAdLoaded;
			#endif
			
			rewardAdButton.SetActive(false);
		}

		#endregion
	}
}
