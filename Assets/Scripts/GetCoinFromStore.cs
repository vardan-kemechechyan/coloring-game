using BBG;
using BBG.MobileTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCoinFromStore : MonoBehaviour
{
	[SerializeField] BBG.Popup popup;

	private bool initialized = false;

	#region Member Variables

	private CurrencyManager.Settings currencySettings;

	#endregion

	#region Rewarded Ads

	public void OnRewardAdButtonClick()
	{
		if(!initialized)
		{
			currencySettings = new CurrencyManager.Settings();

			currencySettings.rewardCurrencyId = "coins";
			currencySettings.rewardAmount = 100;
			currencySettings.rewardAdGrantedPopupId = "reward_ad_granted";
			currencySettings.rewardAdGrantedPopupTitle = "FREE COINS!";
			currencySettings.rewardAdGrantedPopupMessage = "You have been awarded 100 free coins!";
		}

#if BBG_MT_ADS
		if(MobileAdsManager.Instance.RewardAdState != AdNetworkHandler.AdState.Loaded)
		{
			Debug.LogError("[GetGoldFormStore] The reward button was clicked but there is no ad loaded to show.");

			return;
		}

		popup.Hide(false);

		MobileAdsManager.Instance.ShowRewardAd(null, OnRewardAdGranted);
#endif
	}

	private void OnRewardAdGranted()
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

	#endregion
}
