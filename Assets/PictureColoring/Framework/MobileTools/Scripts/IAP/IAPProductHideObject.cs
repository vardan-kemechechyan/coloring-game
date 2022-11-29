using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if BBG_MT_IAP
using BBG.MobileTools;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace BBG
{
	/// <summary>
	/// This class will set the GameObject it is attached to de-active when the product id has been purchased
	/// </summary>
	public class IAPProductHideObject : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	productId = "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			#if BBG_MT_IAP
			IAPManager.Instance.OnIAPInitialized	+= OnIAPInitialized;
			IAPManager.Instance.OnProductPurchased	+= OnProductPurchased;
			#endif

			CheckIsPurchased();
		}

		#endregion

		#region Private Methods

		private void OnProductPurchased(string id)
		{
			if (productId == id)
			{
				CheckIsPurchased();
			}
		}

		private void OnIAPInitialized(bool success)
		{
			CheckIsPurchased();
		}

		private void CheckIsPurchased()
		{
			gameObject.SetActive(true);

			#if BBG_MT_IAP
			if (IAPManager.Instance.IsInitialized)
			{
				Product product = IAPManager.Instance.GetProductInformation(productId);

				if (product != null && product.availableToPurchase && IAPManager.Instance.IsProductPurchased(productId))
				{
					gameObject.SetActive(false);
					IAPManager.Instance.OnIAPInitialized	-= OnIAPInitialized;
					IAPManager.Instance.OnProductPurchased	-= OnProductPurchased;
				}
			}
			#endif
		}

		#endregion
	}
}
