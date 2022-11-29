using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class MyWorksScreen : Screen
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private LevelListItem		listItemPrefab		= null;
		[SerializeField] private GridLayoutGroup	listContainer		= null;
		[SerializeField] private ScrollRect			listScrollRect		= null;

		#endregion

		#region Member Variables

		private List<LevelData>						myWorksLevelDatas;
		private RecyclableListHandler<LevelData>	listHandler;

		#endregion

		#region Properties

		#endregion

		#region Unity Methods

		public override void Initialize()
		{
			base.Initialize();

			// Set the cells size based on the width of the screen
			//HACK: Set grid cellsize automatically
			Utilities.SetGridCellSize(listContainer);

			//TODO: {bookmark} MyWork library is initialized here
			SetupLibraryList();

			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelPlayedEvent, OnLevelPlayedEvent);
			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelCompletedEvent, OnLevelCompletedEvent);
			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelProgressDeletedEvent, OnLevelDeletedEvent);
		}

		public override void OnShowing()
		{
			if (listHandler != null)
			{
				AnalyticEvents.ReportEvent("myworks_open");

				listHandler.Refresh();
			}
		}

		#endregion

		#region Private Methods

		private void OnLevelPlayedEvent(string eventId, object[] data)
		{
			// Add the LevelData that has started playing to the list of my works level datas
			myWorksLevelDatas.Add(data[0] as LevelData);

			// Update the list handler with the new list of level datas
			listHandler.UpdateDataObjects(myWorksLevelDatas);
		}

		private void OnLevelCompletedEvent(string eventId, object[] data)
		{
			LevelData levelData = data[0] as LevelData;

			// Remove the LevelData that was completed and re-insert it 
			myWorksLevelDatas.Remove(levelData);
			myWorksLevelDatas.Insert(0, levelData);

			// Update the list handler with the new list of level datas
			listHandler.UpdateDataObjects(myWorksLevelDatas);
		}

		private void OnLevelDeletedEvent(string eventId, object[] data)
		{
			LevelData levelData = data[0] as LevelData;

			// Remove the deleted LevelData
			myWorksLevelDatas.Remove(levelData);

			// Update the list handler with the new list of level datas
			listHandler.UpdateDataObjects(myWorksLevelDatas);
		}

		/// <summary>
		/// Clears then resets the list of library level items using the current active category index
		/// </summary>
		private void SetupLibraryList()
		{
			//TODO: {bookmark} My workscreen Library is set here

			GameManager.Instance.GetMyWorksLevelDatas(out myWorksLevelDatas);

			if (listHandler == null)
			{
				listHandler = new RecyclableListHandler<LevelData>(myWorksLevelDatas, listItemPrefab, listContainer.transform as RectTransform, listScrollRect);

				listHandler.OnListItemClicked = GameManager.Instance.LevelSelected;

				listHandler.Setup();
			}
			else
			{
				listHandler.UpdateDataObjects(myWorksLevelDatas);
			}
		}

		#endregion
	}
}
