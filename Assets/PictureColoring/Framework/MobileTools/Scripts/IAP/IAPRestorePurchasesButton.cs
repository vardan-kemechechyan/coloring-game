using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG
{
	[RequireComponent(typeof(Button))]
	public class IAPRestorePurchasesButton : MonoBehaviour
	{
		#region Unity Methods

		private void Start()
		{
			gameObject.SetActive(Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer);

			#if BBG_MT_IAP
			gameObject.GetComponent<Button>().onClick.AddListener(BBG.MobileTools.IAPManager.Instance.RestorePurchases);
			#endif
		}

		#endregion
	}
}
