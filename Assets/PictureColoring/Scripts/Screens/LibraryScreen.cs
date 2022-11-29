using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class LibraryScreen : Screen
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private CategoryListItem	categoryListItemPrefab	= null;
		[SerializeField] private Transform			categoryListContainer	= null;

		[Space]

		[SerializeField] private LevelListItem		levelListItemPrefab		= null;
		[SerializeField] private GridLayoutGroup	levelListContainer		= null;
		[SerializeField] private ScrollRect			levelListScrollRect		= null;

		[Header("Rate Us Window")]
		[SerializeField] private ReviewScreen rateUsWindow;

		#endregion

		#region Member Variables

		private ObjectPool							categoryListItemPool;
		private RecyclableListHandler<LevelData>	levelListHandler;
		private List<CategoryListItem>				activeCategoryListItems;
		private int									activeCategoryIndex;

		#endregion

		#region Public Methods

		public override void Initialize()
		{
			base.Initialize();
			
			activeCategoryListItems	= new List<CategoryListItem>();
			categoryListItemPool	= new ObjectPool(categoryListItemPrefab.gameObject, 1, categoryListContainer);

			// Set the cells size based on the width of the screen
			//HACK: Set grid cellsize automatically
			Utilities.SetGridCellSize(levelListContainer);

			SetupCategoryList();
			//TODO: {bookmark} Library is initialized here
			SetupLibraryList();

			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelPlayedEvent, OnLevelGameEvent);
			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelCompletedEvent, OnLevelGameEvent);
			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelProgressDeletedEvent, OnLevelGameEvent);
			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelUnlockedEvent, OnLevelGameEvent);
		}

		public override void OnShowing()
		{
			if (levelListHandler != null)
			{
				levelListHandler.Refresh();

				AnalyticEvents.ReportEvent("libraryscreen_open");

				if(PlayerPrefs.GetInt("newRegionColored") == 1)
				{
					rateUsWindow.CheckIfToShow();
					PlayerPrefs.SetInt("newRegionColored", 0);
					PlayerPrefs.Save();
				}
			}
		}

		#endregion

		#region Private Methods

		private void OnLevelGameEvent(string id, object[] data)
		{
			// Call the Setup method on all visible LevelListItems
			levelListHandler.Refresh();
		}

		private void SetupCategoryList()
		{
			print("In LibraryScreen");

			categoryListItemPool.ReturnAllObjectsToPool();
			activeCategoryListItems.Clear();

			for (int i = 0; i < GameManager.Instance.Categories.Count + 1; i++)
			{
				CategoryListItem categoryListItem = categoryListItemPool.GetObject<CategoryListItem>();

				activeCategoryListItems.Add(categoryListItem);

				if (i == 0)
				{
					categoryListItem.Setup("All");
				}
				else
				{
					categoryListItem.Setup(GameManager.Instance.Categories[i - 1].displayName);
				}

				// Set all list items to not selected at the beginning
				categoryListItem.SetSelected(false);

				// Setup click index and listener
				categoryListItem.Index				= i;
				categoryListItem.OnListItemClicked	= OnCategoryListItemSelected;
			}

			// Set the CategoryListItem as selected
			SetCategoryListItemSelected(0);
		}

		/// <summary>
		/// Invoked when a CategoryListItem is clicked
		/// </summary>
		private void OnCategoryListItemSelected(int index, object data)
		{
			if (activeCategoryIndex != index)
			{
				// Set the CategoryListItem as selected
				SetCategoryListItemSelected(index);

				// Setup the library list for the new selected category
				//TODO: {bookmark} Called when the picture is selected
				SetupLibraryList();
			}
		}

		/// <summary>
		/// Sets the given category list item index as the selected index
		/// </summary>
		private void SetCategoryListItemSelected(int index)
		{
			// Set the current categoy list item to not selected
			if (activeCategoryIndex >= 0 && activeCategoryIndex < activeCategoryListItems.Count)
			{
				activeCategoryListItems[activeCategoryIndex].SetSelected(false);
			}

			// Set the new category list item to selected
			if (index >= 0 && index < activeCategoryListItems.Count)
			{
				activeCategoryListItems[index].SetSelected(true);

				activeCategoryIndex = index;
			}
		}

		/// <summary>
		/// Clears then resets the list of library level items using the current active category index
		/// </summary>
		private void SetupLibraryList()
		{
			//TODO: {bookmark} Library is set here

			if(activeCategoryIndex > GameManager.Instance.Categories.Count)
			{
				return;
			}

			List<LevelData> levelDatas = null;

			// If the active category index is 0 then the "All" category is selected so we need to show all the levels
			if (activeCategoryIndex == 0)
			{
				levelDatas = GameManager.Instance.AllLevels;
			}
			// Else get the levels in the selected category
			else
			{
				levelDatas = GameManager.Instance.Categories[activeCategoryIndex - 1].levels;
			}

			// Check if this is the first time we are setting up the library list
			if (levelListHandler == null)
			{
				// Create a new RecyclableListHandler to handle recycling list items that scroll off screen
				levelListHandler = new RecyclableListHandler<LevelData>(levelDatas, levelListItemPrefab, levelListContainer.transform as RectTransform, levelListScrollRect);

				levelListHandler.OnListItemClicked = GameManager.Instance.LevelSelected;

				levelListHandler.Setup();
			}
			else
			{
				// Update the the RecyclableListHandler with the new data set
				levelListHandler.UpdateDataObjects(levelDatas);
			}
		}

		#endregion
	}
}
