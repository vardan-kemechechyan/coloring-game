using BBG.MobileTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
	public class GameManager : SaveableManager<GameManager>
	{
		#region Inspector Variables

		[Header("Data")]
		[SerializeField] private List<CategoryData> categories = null;

		[Header("Values")]
		[SerializeField] private bool	awardHints			= false;
		[SerializeField] private int	numLevelsBetweenAds	= 0;
		#endregion

		#region Member Variables

		private List<LevelData> allLevels;
		private int numLevelsStarted;

		// Contains all LevelSaveDatas which have been requested but the level has yet to be colored (This is not saved to file)
		private Dictionary<string, LevelSaveData> levelSaveDatas;

		// Contains all LevelSaveDatas which have atleast one region colored in but have not been completed yet
		private Dictionary<string, LevelSaveData> playedLevelSaveDatas;

		/// <summary>
		/// Contains all level ids which have been completed by the player
		/// </summary>
		private HashSet<string> unlockedLevels;

		/// <summary>
		/// Levels that have been completed atleast one and the player has been awarded the coins/hints
		/// </summary>
		private HashSet<string> awardedLevels;

		#endregion

		#region Properties

		public override string SaveId { get { return "game_manager"; } }

		public List<CategoryData>	Categories		{ get { return categories; } }
		public LevelData			ActiveLevelData	{ get; private set; }
		public LevelData			TryingToUnlockLevelData	{ get; private set; }

		public List<LevelData> AllLevels
		{
			get
			{
				if (allLevels == null)
				{
					allLevels = new List<LevelData>();

					for (int i = 0; i < categories.Count; i++)
					{
						allLevels.AddRange(categories[i].levels);
					}
				}

				return allLevels;
			}
		}

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();
			
			playedLevelSaveDatas	= new Dictionary<string, LevelSaveData>();
			levelSaveDatas			= new Dictionary<string, LevelSaveData>();
			awardedLevels			= new HashSet<string>();
			unlockedLevels			= new HashSet<string>();

			InitSave();

			ScreenManager.Instance.OnSwitchingScreens += OnSwitchingScreens;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Shows the level selected popup
		/// </summary>
		public void LevelSelected(LevelData levelData)
		{
			bool isLocked = levelData.locked && !levelData.LevelSaveData.isUnlocked;

			var catName = new Dictionary<string, object>();
			catName.Add("category_name", GetDisplayNameByLevelID(levelData.Id));
			catName.Add("pictureName", levelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
			AnalyticEvents.ReportEvent("painting_selected", catName);

			// Check if the level has been played or if its locked
			if (IsLevelPlaying(levelData.Id) || isLocked)
			{
				PopupManager.Instance.Show("level_selected", new object[] { levelData, isLocked }, (bool cancelled, object[] outData) => 
				{
					if (!cancelled)
					{
						string action = outData[0] as string;

						// Check what button the player selected
						switch (action)
						{
							case "continue":
								StartLevel(levelData);
								break;
							case "delete":
								DeleteLevelSaveData(levelData);
								break;
							case "restart":
								DeleteLevelSaveData(levelData);
								StartLevel(levelData);
								break;
							case "unlock":
								if(levelData.UnlockForAds)
								{
									OnRewardAdButtonClick();
								}
								else
								{
									// Try and spend the coins required to unlock the level
									if (CurrencyManager.Instance.TrySpend("coins", levelData.coinsToUnlock, levelData.UnlockForAds))
									{
										UnlockLevel(levelData);
										StartLevel(levelData);
									}
								}
								TryingToUnlockLevelData = levelData;
								break;
						}
					}
				});
			}
			else
			{
				StartLevel(levelData);
			}
		}

		public void UnlockAndStart(LevelData levelData)
		{
			UnlockLevel(levelData);
			StartLevel(levelData);
			TryingToUnlockLevelData = null;
		}

		public void StartLevel(LevelData levelData)
		{
			ReleaseLevel();

			// Set the new active LevelData
			ActiveLevelData = levelData;

			// Start loading everything needed to play the level
			bool loading = LoadManager.Instance.LoadLevel(levelData, OnLevelLoaded, true);

			if (loading)
			{
				GameEventManager.Instance.SendEvent(GameEventManager.LevelLoadingEvent);
			}
			else
			{
				OnLevelLoaded(levelData.Id, true);

				var catName = new Dictionary<string, object>();
				catName.Add("category_name", GetDisplayNameByLevelID(levelData.Id));
				catName.Add("pictureName", levelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
				AnalyticEvents.ReportEvent("painting_open", catName);
			}

			// Show the game screen now
			ScreenManager.Instance.Show("game");

			// Increate the number of levels started since the last ad was shown
			numLevelsStarted++;

			// Check if it's time to show an interstitial ad
			if (numLevelsStarted > numLevelsBetweenAds)
			{
				#if BBG_MT_ADS
				if (BBG.MobileTools.MobileAdsManager.Instance.ShowInterstitialAd(null))
				{
					// If an ad was successfully shown then reset the num levels started
					numLevelsStarted = 0;
					print("Started the game mode");
				}
				#endif
			}
		}

		/// <summary>
		/// Attempts to color the region at the given pixel x/y. Returns true if a new region is colored, false if nothing changed.
		/// </summary>
		public bool TryColorRegion(int x, int y, int colorIndex, out bool levelCompleted, out bool hintAwarded, out bool coinsAwarded)
		{
			LevelData activeLevelData = ActiveLevelData;

			levelCompleted	= false;
			hintAwarded		= false;
			coinsAwarded	= false;

			if (activeLevelData != null)
			{
				Region region = GetRegionAt(x, y);

				// Check that this is the correct region for the selected color index and the region has not already been colored in
				if (region != null && region.colorIndex == colorIndex && !activeLevelData.LevelSaveData.coloredRegions.Contains(region.id))
				{
					// Color the region
					ColorRegion(region);

					/*if(PlayerPrefs.GetInt("newRegionColored") == 0 )
					{
						PlayerPrefs.SetInt("newRegionColored", 1);
						PlayerPrefs.Save();
					}*/

					// Set the region as colored in the level save data
					activeLevelData.LevelSaveData.coloredRegions.Add(region.id);

					// Check if the level is not in the playedLevelSaveDatas dictionary, it not then this is the first region to be colored
					if (!playedLevelSaveDatas.ContainsKey(activeLevelData.Id))
					{
						// Set the LevelSaveData of the active LevelData in the playedLevelSaveDatas so will will saved now that a region has been colored
						playedLevelSaveDatas.Add(activeLevelData.Id, activeLevelData.LevelSaveData);
						levelSaveDatas.Remove(activeLevelData.Id);

						if(PlayerPrefs.GetInt("newRegionColored") == 0)
							PlayerPrefs.SetInt("newRegionColored", 1);

							int levelsStarted = PlayerPrefs.GetInt("levelsStarted");
						PlayerPrefs.SetInt("levelsStarted", ++levelsStarted);
						PlayerPrefs.Save();

						GameEventManager.Instance.SendEvent(GameEventManager.LevelPlayedEvent, activeLevelData);
					}

					// Check if all regions have been colored
					levelCompleted = activeLevelData.AllRegionsColored();

					if (levelCompleted)
					{
						// Check if this level has not been awarded hints / coins yet (ie. first time the level is completed)
						if (!awardedLevels.Contains(activeLevelData.Id))
						{
							awardedLevels.Add(activeLevelData.Id);

							if (awardHints)
							{
								// Award the player 1 hint for completing the level
								CurrencyManager.Instance.Give("hints", 1);

								hintAwarded = true;
							}

							if (activeLevelData.coinsToAward > 0)
							{
								// Award coins to the player for completing this level
								CurrencyManager.Instance.Give("coins", activeLevelData.coinsToAward);

								coinsAwarded = true;
							}
						}

						// The level is now complete
						activeLevelData.LevelSaveData.isCompleted = true;

						int firstColoredPicture = PlayerPrefs.GetInt("levelsCompleted");

						if(PlayerPrefs.GetInt("newRegionColored") == 0)
							PlayerPrefs.SetInt("newRegionColored", 1);

						PlayerPrefs.SetInt("levelsCompleted", ++firstColoredPicture);
						PlayerPrefs.Save();

						// Notify a lwevel has been compelted
						GameEventManager.Instance.SendEvent(GameEventManager.LevelCompletedEvent, activeLevelData);
					}

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns a LevelSaveData for the given level id
		/// </summary>
		public LevelSaveData GetLevelSaveData(string levelId)
		{
			LevelSaveData levelSaveData;

			if (playedLevelSaveDatas.ContainsKey(levelId))
			{
				levelSaveData = playedLevelSaveDatas[levelId];
			}
			else if (levelSaveDatas.ContainsKey(levelId))
			{
				levelSaveData = levelSaveDatas[levelId];
			}
			else
			{
				levelSaveData = new LevelSaveData();
				levelSaveDatas.Add(levelId, levelSaveData);
			}

			if (unlockedLevels.Contains(levelId))
			{
				levelSaveData.isUnlocked = true;
			}

			return levelSaveData;
		}

		/// <summary>
		/// Returns true if the level was completed atleast once by the player
		/// </summary>
		public bool IsLevelPlaying(string levelId)
		{
			return playedLevelSaveDatas.ContainsKey(levelId);
		}

		/// <summary>
		/// Gets all level datas that are beening played or have been completed
		/// </summary>
		public void GetMyWorksLevelDatas(out List<LevelData> myWorksLeveDatas)
		{
			myWorksLeveDatas = new List<LevelData>();

			int completeInsertIndex = 0;

			for (int i = 0; i < categories.Count; i++)
			{
				List<LevelData> levelDatas = categories[i].levels;
				
				for (int j = 0; j < levelDatas.Count; j++)
				{
					LevelData	levelData	= levelDatas[j];
					string		levelId		= levelData.Id;

					if (playedLevelSaveDatas.ContainsKey(levelId))
					{
						LevelSaveData levelSaveData = playedLevelSaveDatas[levelId];

						if (levelSaveData.isCompleted)
						{
							myWorksLeveDatas.Insert(completeInsertIndex++, levelData);
						}
						else
						{
							myWorksLeveDatas.Add(levelData);
						}
					}
				}		
			}
		}

		public string GetDisplayNameByLevelID(string levelID)
		{
			foreach(var category in categories)
			{
				foreach(var _leveldata in category.levels)
				{
					if(_leveldata.Id.Equals(levelID))
						return category.displayName;
				}
			}

			return "";
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Invoked when the LevelLoadManager has finished loading the active LevelData
		/// </summary>
		private void OnLevelLoaded(string levelId, bool success)
		{
			if (ActiveLevelData != null && ActiveLevelData.Id == levelId)
			{
				GameEventManager.Instance.SendEvent(GameEventManager.LevelLoadFinishedEvent, success);
			}
		}

		/// <summary>
		/// Gets the Region which contains the given pixel
		/// </summary>
		private Region GetRegionAt(int x, int y)
		{
			List<Region> regions = ActiveLevelData.LevelFileData.regions;

			// Check all regions for the one that contains the pixel
			for (int i = 0; i < regions.Count; i++)
			{
				Region region = regions[i];

				// Check if this region contains the pixel
				if (IsPixelInRegion(region, x, y))
				{
					return region;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns true if one of the triangles in this region contains the given pixel point
		/// </summary>
		private bool IsPixelInRegion(Region region, int pixelX, int pixelY)
		{
			if (pixelX < region.bounds.minX || pixelX > region.bounds.maxX || pixelY < region.bounds.minY || pixelY > region.bounds.maxY)
			{
				return false;
			}

			int index = region.pixelsByX ? (pixelX - region.bounds.minX) : (pixelY - region.bounds.minY);
			int value = region.pixelsByX ? pixelY : pixelX;

			List<int[]> pixelSections = region.pixelsInRegion[index];

			for (int k = 0; k < pixelSections.Count; k++)
			{
				int[] startEnd = pixelSections[k];
				int start = startEnd[0];
				int end = startEnd[1];

				if (value >= start && value <= end)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Colors the regions pixels on the levelData ColoredTexture
		/// </summary>
		private void ColorRegion(Region region)
		{
			// Color		regionColor		= ActiveLevelData.LevelFileData.colors[region.colorIndex];
			// Texture2D	coloredTexture	= ActiveLevelData.ColoredTexture;
			// Color[]		coloredPixels	= ActiveLevelData.ColoredTexture.GetPixels();
			// int			textureWidth	= coloredTexture.width;

			// int min = (region.pixelsByX ? region.bounds.minX : region.bounds.minY);

			// for (int j = 0; j < region.pixelsInRegion.Count; j++)
			// {
			// 	List<int[]> pixelSections = region.pixelsInRegion[j];

			// 	for (int k = 0; k < pixelSections.Count; k++)
			// 	{
			// 		int[]	startEnd	= pixelSections[k];
			// 		int		start		= startEnd[0];
			// 		int		end			= startEnd[1];

			// 		for (int i = start; i <= end; i++)
			// 		{
			// 			int x = region.pixelsByX ? min + j : i;
			// 			int y = region.pixelsByX ? i : min + j;

			// 			coloredPixels[y * textureWidth + x] = regionColor;
			// 		}
			// 	}
			// }

			// coloredTexture.SetPixels(coloredPixels);
			// coloredTexture.Apply();
		}

		/// <summary>
		/// Clears any progress from the level and sets the level as not completed
		/// </summary>
		private void DeleteLevelSaveData(LevelData levelData)
		{
			LevelSaveData levelSaveData = levelData.LevelSaveData;

			// Clear the colored regions
			levelSaveData.coloredRegions.Clear();

			// Make sure the completed flag is false
			levelSaveData.isCompleted = false;

			// Remove the level from the played and completed levels
			playedLevelSaveDatas.Remove(levelData.Id);

			GameEventManager.Instance.SendEvent(GameEventManager.LevelProgressDeletedEvent, levelData);
		}

		/// <summary>
		/// Unlocks the level
		/// </summary>
		private void UnlockLevel(LevelData levelData)
		{
			if (!unlockedLevels.Contains(levelData.Id))
			{
				unlockedLevels.Add(levelData.Id);
			}

			levelData.LevelSaveData.isUnlocked = true;

			var catName = new Dictionary<string, object>();
			catName.Add("category_name", GetDisplayNameByLevelID(levelData.Id));
			catName.Add("pictureName", levelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
			AnalyticEvents.ReportEvent("painting_unlocked", catName);

			GameEventManager.Instance.SendEvent(GameEventManager.LevelUnlockedEvent, new object[] { levelData });
		}

		/// <summary>
		/// Invoked by ScreenManager when screens are transitioning
		/// </summary>
		private void OnSwitchingScreens(string fromScreen, string toScreen)
		{
			if (fromScreen == "game")
			{
				ReleaseLevel();
			}
		}

		private void ReleaseLevel()
		{
			if (ActiveLevelData != null)
			{
				LoadManager.Instance.ReleaseLevel(ActiveLevelData.Id);
				ActiveLevelData = null;
			}
		}

		#endregion

		#region Save Methods

		public override Dictionary<string, object> Save()
		{
			Dictionary<string, object>	saveData		= new Dictionary<string, object>();
			List<object>				levelSaveDatas	= new List<object>();

			foreach (KeyValuePair<string, LevelSaveData> pair in playedLevelSaveDatas)
			{
				Dictionary<string, object> levelSaveData = new Dictionary<string, object>();

				levelSaveData["key"]	= pair.Key;
				levelSaveData["data"]	= pair.Value.ToJson();

				levelSaveDatas.Add(levelSaveData);
			}

			saveData["levels"]		= levelSaveDatas;
			saveData["awarded"]		= SaveHashSetValues(awardedLevels);
			saveData["unlocked"]	= SaveHashSetValues(unlockedLevels);

			return saveData;
		}

		protected override void LoadSaveData(bool exists, JSONNode saveData)
		{
			if (!exists)
			{
				return;
			}

			// Load all the levels that have some progress
			JSONArray levelSaveDatasJson = saveData["levels"].AsArray;

			for (int i = 0; i < levelSaveDatasJson.Count; i++)
			{
				JSONNode	levelSaveDataJson	= levelSaveDatasJson[i];
				string		key					= levelSaveDataJson["key"].Value;
				JSONNode	data				= levelSaveDataJson["data"];

				LevelSaveData levelSaveData = new LevelSaveData();

				levelSaveData.FromJson(data);

				playedLevelSaveDatas.Add(key, levelSaveData);
			}

			LoadHastSetValues(saveData["awarded"].Value, awardedLevels);
			LoadHastSetValues(saveData["unlocked"].Value, unlockedLevels);
		}

		/// <summary>
		/// Saves all values in the HashSet hash as a single string
		/// </summary>
		private string SaveHashSetValues(HashSet<string> hashSet)
		{
			string jsonStr = "";

			List<string> list = new List<string>(hashSet);

			for (int i = 0; i < list.Count; i++)
			{
				if (i != 0)
				{
					jsonStr += ";";
				}

				jsonStr += list[i];
			}

			return jsonStr;
		}

		/// <summary>
		/// Loads the hast set values.
		/// </summary>
		private void LoadHastSetValues(string str, HashSet<string> hashSet)
		{
			string[] values = str.Split(';');

			for (int i = 0; i < values.Length; i++)
			{
				hashSet.Add(values[i]);
			}
		}

		#endregion

		#region Menu Items

		#if UNITY_EDITOR

		[UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 10000 Coins", priority = 300)]
		private static void Give1000Coins()
		{
			CurrencyManager.Instance.Give("coins", 10000);
		}

		[UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 10000 Coins", validate = true)]
		private static bool Give1000CoinsValidate()
		{
			return Application.isPlaying;
		}

		[UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 1000 Hints", priority = 301)]
		private static void Give1000Hints()
		{
			CurrencyManager.Instance.Give("hints", 1000);
		}

		[UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 1000 Hints", validate = true)]
		private static bool Give1000HintsValidate()
		{
			return Application.isPlaying;
		}

#endif

		#endregion

		#region Rewarded Ads

		public void OnRewardAdButtonClick()
		{
#if BBG_MT_ADS
			if(MobileAdsManager.Instance.RewardAdState != AdNetworkHandler.AdState.Loaded)
			{
				Debug.LogError("[NoAdsToLoad] The reward button was clicked but there is no ad loaded to show.");

				return;
			}

			MobileAdsManager.Instance.ShowRewardAd(null, OnRewardAdGranted);
#endif
		}

		private void OnRewardAdGranted()
		{
			var catName = new Dictionary<string, object>();
			catName.Add("category_name", GetDisplayNameByLevelID(TryingToUnlockLevelData.Id));
			catName.Add("pictureName", TryingToUnlockLevelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
			AnalyticEvents.ReportEvent("rewarded_unlock", catName);

			UnlockAndStart(GameManager.Instance.TryingToUnlockLevelData);
		}

		#endregion
	}
}
