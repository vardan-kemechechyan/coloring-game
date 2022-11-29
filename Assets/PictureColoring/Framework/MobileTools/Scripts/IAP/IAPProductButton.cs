using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if BBG_MT_IAP
using BBG.MobileTools;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace BBG
{
	[RequireComponent(typeof(Button))]
	public class IAPProductButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private ProductId			productId				= null;
		[SerializeField] private Text				titleText				= null;
		[SerializeField] private Text				descriptionText			= null;
		[SerializeField] private Text				priceText				= null;
		[SerializeField] private RectTransform		dependantElement		= null;
		[SerializeField] private RectTransform		targetElement			= null;

		#endregion

		#region Member Variables

		private const string LogTag = "IAPProductButton";

		private Button button;

		#endregion

		#region Unity Methods

		private void Start()
		{
			button = gameObject.GetComponent<Button>();

			button.onClick.AddListener(OnClicked);

			UpdateButton();

#if BBG_MT_IAP
			IAPManager.Instance.OnGameServicesInitialized += UpdateButton;
			IAPManager.Instance.OnIAPInitialized	+= OnIAPInitialized;
			IAPManager.Instance.OnProductPurchased	+= OnProductPurchased;
			#endif
		}

		#endregion

		#region Private Methods

		private void OnClicked()
		{
			#if BBG_MT_IAP
			IAPManager.Instance.BuyProduct(productId.productId);
			#endif
		}

		private void OnProductPurchased(string id)
		{
			if (productId.productId == id)
			{
				UpdateButton();
			}
		}

		private void OnIAPInitialized(bool success)
		{
			UpdateButton();
		}

		private void UpdateButton()
		{
			button.interactable = false;

			#if BBG_MT_IAP
			if (IAPManager.Instance.IsInitialized)
			{
				if (string.IsNullOrEmpty(productId.productId))
				{
					GameDebugManager.LogError(LogTag, "IAP Product Button does not have a product id assigned to it.");
					SetTitleText("no product id");
				}
				else
				{
					Product product = IAPManager.Instance.GetProductInformation(productId.productId);

					if (product == null)
					{
						GameDebugManager.LogError(LogTag, "Product with id \"" + productId + "\" does not exist.");
						SetTitleText(productId + ": does not exist");
					}
					else if (!product.availableToPurchase)
					{
						GameDebugManager.LogError(LogTag, "Product with id \"" + productId + "\" is not available to purchase.");
						SetTitleText(productId + ": un-available");
					}
					else
					{
						button.interactable = true;

						SetupButton(product);
					}
				}
			}
			#endif
		}

		#if BBG_MT_IAP
		private void SetupButton(Product product)
		{
			if (IAPManager.Instance.IsProductPurchased(product.definition.id))
			{
				// If the product has been purchased then hide the button (Only for non-consumable products)
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);

				if (priceText != null)
				{
					priceText.text = product.metadata.localizedPriceString;

					if(dependantElement != null)
					{
						//dependantElement.sizeDelta = new Vector2(targetElement.sizeDelta.x, dependantElement.sizeDelta.y);
						Invoke("UpdateDependantUIs", 0.05f);
					}
				}

				/*if (titleText != null)
				{
					#if UNITY_EDITOR
					titleText.text = product.definition.id;
					#else
					string title = product.metadata.localizedTitle;
					
					// Strip the "(App Name)" text that is included by google for some reason
					int startIndex	= title.LastIndexOf('(');
					int endIndex	= title.LastIndexOf(')');
					
					if (startIndex > 0 && endIndex > 0 && startIndex < endIndex)
					{
					title = title.Remove(startIndex, endIndex - startIndex + 1);
					title = title.Trim();
					}
					
					titleText.text = title;
					#endif
				}*/

				if (descriptionText != null)
				{
					descriptionText.text = product.metadata.localizedDescription;
				}
			}
		}
		#endif

		void UpdateDependantUIs()
		{
			dependantElement.sizeDelta = new Vector2(targetElement.sizeDelta.x, dependantElement.sizeDelta.y);
		}

		private void SetTitleText(string text)
		{
			if (titleText != null)
			{
				titleText.text = text;
			}
		}

		#endregion
	}
}
