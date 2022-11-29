using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.PictureColoring
{
	[System.Serializable]
	public class LevelData
	{
		#region Inspector Variables

		public TextAsset	levelFile;
		public int			coinsToAward;
		public bool			locked;
		public bool			unlockForAds;
		public int			coinsToUnlock;
		public Sprite		thumbnail_completed;
		public Sprite		thumbnail_empty;

		#endregion

		#region Member Variables

		private bool		levelFileParsed;
		private string		id;
		private string		assetPath;
		private string		pathToBytesInWeb;
		private string		web_or_local;
		private string[]	atlases_in_web;

		#endregion

		#region Properties

		public string WebOrLocal
		{
			get
			{
				if(!levelFileParsed)
				{
					ParseLevelFile();
				}

				return web_or_local;
			}
		}

		public string[] Atlases_In_Web
		{
			get
			{
				if(!levelFileParsed)
				{
					ParseLevelFile();
				}

				return atlases_in_web;
			}
		}

		public string Id
		{
			get
			{
				if (!levelFileParsed)
				{
					ParseLevelFile();
				}

				return id;
			}
		}

		public string AssetPath
		{
			get
			{
				if (!levelFileParsed)
				{
					ParseLevelFile();
				}

				return assetPath;
			}
		}
		public string BytesInWeb
		{
			get
			{
				if (!levelFileParsed)
				{
					ParseLevelFile();
				}

				return pathToBytesInWeb;
			}
		}

		public bool UnlockForAds
		{
			get
			{
				if(!levelFileParsed)
				{
					ParseLevelFile();
				}

				return unlockForAds;
			}
		}

		public LevelSaveData LevelSaveData
		{
			get
			{
				return GameManager.Instance.GetLevelSaveData(Id);
			}
		}

		/// <summary>
		/// Gets the level file data, should only call this if you know the level has been loaded
		/// </summary>
		public LevelFileData LevelFileData { get { return LoadManager.Instance.GetLevelFileData(Id); } }

		#endregion

		#region Public Methods

		public bool IsColorComplete(int colorIndex)
		{
			if (LevelFileData == null)
			{
				Debug.LogError("[LevelData] IsColorRegionComplete | LevelFileData has not been loaded.");

				return false;
			}

			if (colorIndex < 0 || colorIndex >= LevelFileData.regions.Count)
			{
				Debug.LogErrorFormat("[LevelData] IsColorComplete | Given colorIndex ({0}) is out of bounds for the regions list of size {1}.", colorIndex, LevelFileData.regions.Count);

				return false;
			}

			LevelSaveData	levelSaveData	= LevelSaveData;
			List<Region>	regions			= LevelFileData.regions;

			for (int i = 0; i < regions.Count; i++)
			{
				Region region = regions[i];

				if (region.colorIndex == colorIndex && !levelSaveData.coloredRegions.Contains(region.id))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if all regions have been colored
		/// </summary>
		public bool AllRegionsColored()
		{
			if (LevelFileData == null)
			{
				Debug.LogError("[LevelData] AllRegionsColored | LevelFileData has not been loaded.");

				return false;
			}

			LevelSaveData	levelSaveData	= LevelSaveData;
			List<Region>	regions			= LevelFileData.regions;

			for (int i = 0; i < regions.Count; i++)
			{
				Region region = regions[i];

				if (region.colorIndex > -1 && !levelSaveData.coloredRegions.Contains(region.id))
				{
					return false;
				}
			}

			return true;
		}

		public bool IsLevelInProgress()
		{
			LevelSaveData levelSaveData = LevelSaveData;
			List<Region> regions = LevelFileData.regions;

			for(int i = 0; i < regions.Count; i++)
			{
				Region region = regions[i];

				if(region.colorIndex > -1 && levelSaveData.coloredRegions.Contains(region.id))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets a random region index in the given ColorRegion that has not been colored in
		/// </summary>
		public int GetSmallestUncoloredRegion(int colorIndex)
		{
			if (LevelFileData == null)
			{
				Debug.LogError("[LevelData] GetRandomUncoloredRegion | LevelFileData has not been loaded.");

				return -1;
			}

			if (colorIndex < 0 || colorIndex >= LevelFileData.regions.Count)
			{
				Debug.LogErrorFormat("[LevelData] GetRandomUncoloredRegion | Given colorRegionIndex ({0}) is out of bounds for the colorRegions list of size {1}.", colorIndex, LevelFileData.regions.Count);

				return -1;
			}

			LevelSaveData	levelSaveData	= LevelSaveData;
			List<Region>	regions			= LevelFileData.regions;

			int minRegionSize	= int.MaxValue;
			int index			= -1;

			for (int i = 0; i < regions.Count; i++)
			{
				Region region = regions[i];

				if (colorIndex == region.colorIndex && !levelSaveData.coloredRegions.Contains(region.id))
				{
					if (minRegionSize > region.numberSize)
					{
						minRegionSize	= region.numberSize;
						index			= i;
					}
				}
			}

			return index;
		}

		#endregion

		#region Private Methods

		private void ParseLevelFile()
		{
			string[] fileContents = levelFile.text.Split('\n');

			id				= fileContents[0].Trim();
			assetPath		= fileContents[1].Trim();
			if(fileContents.Length == 2)
			{
				web_or_local = "local";
			}
			else
			{
				web_or_local = fileContents[2].Trim();
			}

			if(web_or_local == "web")
			{
				atlases_in_web = new string[fileContents.Length - 7];
				pathToBytesInWeb = fileContents[3].Trim();

				for(int i = 0; i < fileContents.Length - 7; i++)
				{
					atlases_in_web[i] = fileContents[i + 7];
				}

				int.TryParse(fileContents[4].Trim(), out coinsToAward);
				int.TryParse(fileContents[6].Trim(), out coinsToUnlock);
				locked = fileContents[5].Trim() == "true";

				if(locked && coinsToUnlock == -1)
					unlockForAds = true;
				else
					unlockForAds = false;

				thumbnail_completed = Resources.Load<Sprite>(assetPath.Replace("Assets/Resources/", string.Empty) + "/completed");
				thumbnail_empty = Resources.Load<Sprite>(assetPath.Replace("Assets/Resources/", string.Empty) + "/empty");

				/*if(thumbnail_empty == null)
				{
					byte[] fileData;
					Texture2D tex = new Texture2D(2, 2);
					string filePath = assetPath + "/empty.jpg";
					fileData = System.IO.File.ReadAllBytes(filePath);
					tex.LoadImage(fileData);
					thumbnail_empty = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
					thumbnail_empty.name = "empty";
				}*/

				/*thumbnail_completed = Resources.Load<Sprite>("Weave Custom/01-fish mec/completed");
				thumbnail_empty = Resources.Load<Sprite>("Weave Custom/01-fish mec/empty");*/
			}

			//Debug.LogWarning($"{levelFile.name} uses {web_or_local}");

			levelFileParsed = true;
		}

		#endregion
	}
}
