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
	[RequireComponent(typeof(Button))]
	public class RewardAdButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string		currencyId				= "";
		[SerializeField] private int		amountToReward			= 0;
		[SerializeField] private GameObject	uiContainer				= null;
		#if UNITY_EDITOR
		[SerializeField] private bool		testInEditor			= false;
		#endif

		[Space]

		[SerializeField] private bool	showOnlyWhenCurrencyIsLow	= false;
		[SerializeField] private int	currencyShowTheshold		= 0;

		[Space]

		[SerializeField] private bool	showRewardGrantedPopup		= false;
		[SerializeField] private string	rewardGrantedPopupId		= "";
		[SerializeField] private string	rewardGrantedPopupTitle		= "";
		[SerializeField] private string	rewardGrantedPopupMessage	= "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			uiContainer.SetActive(false);

			bool areRewardAdsEnabled = false;
			
			#if BBG_MT_ADS
			areRewardAdsEnabled= MobileAdsManager.Instance.AreRewardAdsEnabled;
			#endif

			#if UNITY_EDITOR
			areRewardAdsEnabled = testInEditor;
			#endif

			if (areRewardAdsEnabled)
			{
				UpdateUI();

				#if BBG_MT_ADS
				MobileAdsManager.Instance.OnRewardAdLoaded	+= UpdateUI;
				MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
				#endif

				CurrencyManager.Instance.OnCurrencyChanged	+= OnCurrencyChanged;

				gameObject.GetComponent<Button>().onClick.AddListener(OnClicked);
			}
		}

		#endregion

		#region Private Methods

		private void OnCurrencyChanged(string changedCurrencyId)
		{
			if (currencyId == changedCurrencyId)
			{
				UpdateUI();
			}
		}

		private void UpdateUI()
		{
			bool rewardAdLoded		= false;
			bool passShowThreshold	= (!showOnlyWhenCurrencyIsLow || CurrencyManager.Instance.GetAmount(currencyId) <= currencyShowTheshold);

			#if BBG_MT_ADS
			rewardAdLoded = MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded;
			#endif

			uiContainer.SetActive(rewardAdLoded && passShowThreshold);

			#if UNITY_EDITOR
			if (testInEditor)
			{
				uiContainer.SetActive(passShowThreshold);
			}
			#endif
		}

		private void OnAdsRemoved()
		{
			#if BBG_MT_ADS
			MobileAdsManager.Instance.OnRewardAdLoaded	-= UpdateUI;
			MobileAdsManager.Instance.OnAdsRemoved		-= OnAdsRemoved;
			#endif

			CurrencyManager.Instance.OnCurrencyChanged	-= OnCurrencyChanged;

			uiContainer.SetActive(false);
		}

		private void OnClicked()
		{
			/*#if UNITY_EDITOR
			if (testInEditor)
			{
				OnRewardAdGranted();

				return;
			}
			#endif*/

			uiContainer.SetActive(false);

			#if BBG_MT_ADS
			MobileAdsManager.Instance.ShowRewardAd(null, OnRewardAdGranted);
			#endif
		}

		private void OnRewardAdGranted()
		{
			rewardGrantedPopupMessage = "YOU HAVE BEEN AWARDED \n1 FREE HINT!";

			CurrencyManager.Instance.Give(currencyId, amountToReward);

			if (showRewardGrantedPopup)
			{
				object[] popupData =
				{
					rewardGrantedPopupTitle,
					rewardGrantedPopupMessage
				};

				PopupManager.Instance.Show(rewardGrantedPopupId, popupData);
			}

			HintAnimationManager.cancelAnimation = true;

			var catName = new Dictionary<string, object>();
			catName.Add("category_name", GameManager.Instance.GetDisplayNameByLevelID(GameManager.Instance.ActiveLevelData.Id));
			catName.Add("pictureName", GameManager.Instance.ActiveLevelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
			AnalyticEvents.ReportEvent("rewarded_hint", catName);
		}

		#endregion
	}
}
