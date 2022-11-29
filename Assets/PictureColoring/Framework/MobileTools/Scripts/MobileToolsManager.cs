using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if BBG_MT_IAP || BBG_MT_ADS
using BBG.MobileTools;
#endif

namespace BBG
{
    public class MobileToolsManager : MonoBehaviour
	{
		[System.Serializable]
		public class OnProductPurchasedEvent : UnityEngine.Events.UnityEvent { }

		[System.Serializable]
		public class ProductPurchasedEvent
		{
			public ProductId productId;
			public OnProductPurchasedEvent pruchasedEvent;
		}

		#region Inspector Variables

		[Header("General")]
		public string privacyPolicyUrl;

		[Header("Ads")]

		public GameObject adsConsentPopup = null;
		public GameObject adsLoadingIntercept = null;

		[Header("IAP")]

		public GameObject iapLoadingIntercept = null;
		public List<ProductPurchasedEvent> productPurchasedEvents = null;

		public int interstitialDelay;
		public int rewardedDelay;

		#endregion

		#region Unity Methods

		private void Awake()
		{
			#if BBG_MT_ADS
			if (MobileAdsManager.Instance == null)
			{
				GameObject obj = new GameObject("MobileAdsManager", typeof(MobileAdsManager));
				obj.transform.SetParent(transform);

				MobileAdsManager mobileAdsManager = obj.GetComponent<MobileAdsManager>();
				mobileAdsManager.consentPopup = adsConsentPopup;
				mobileAdsManager.loadingObj = adsLoadingIntercept;
				mobileAdsManager.interstitialDelay = interstitialDelay;
				mobileAdsManager.rewardedDelay = rewardedDelay;
			}
			#endif
			
			#if BBG_MT_IAP
			if (IAPManager.Instance == null)
			{
				GameObject obj = new GameObject("IAPManager", typeof(IAPManager));
				obj.transform.SetParent(transform);

				IAPManager iapManager = obj.GetComponent<IAPManager>();
				iapManager.loadingObj = iapLoadingIntercept;

				iapManager.OnProductPurchased += OnIapProductPurchased;
			}
			#endif
		}

		#endregion

		#region Public Methods

		public void RemoveAds()
		{
			#if BBG_MT_ADS
			MobileAdsManager.Instance.RemoveAds();
			#endif
		}

		public void SetConsentStatus(int consent)
		{
			#if BBG_MT_ADS
			MobileAdsManager.Instance.SetConsentStatus(consent);
			#endif
		}

		public void OpenPrivacyPolicy()
		{
			Application.OpenURL(privacyPolicyUrl);
		}

		#endregion

		#region Private Methods

		#if BBG_MT_IAP

		private void OnIapProductPurchased(string productId)
		{
			for (int i = 0; i < productPurchasedEvents.Count; i++)
			{
				if (productPurchasedEvents[i].productId.productId == productId)
				{
					productPurchasedEvents[i].pruchasedEvent.Invoke();
				}
			}
		}

		#endif

		#endregion
	}
}
