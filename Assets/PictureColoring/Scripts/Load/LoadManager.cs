using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BBG.PictureColoring
{
	public class LoadManager : SingletonComponent<LoadManager>
	{
		#region Classes

		private class LevelLoadHandler
		{
			public enum State
			{
				Loading,
				Loaded,
				Released
			}

			public string				web_or_local;
			public string				levelId;
			public string				assetPath;
			public string				BytesInWeb;
			public string[]				atlases_in_web;
			public int					refCount;
			public State				state;
			public LevelFileData		levelFileData;
			public Sprite[]				atlasSprites;
			public List<LoadComplete>	loadCompleteCallbacks;
		}

		private class LevelFileOptimised
		{
			public byte[] bytesArray;
			public List<Sprite> atlases;
		}

		#endregion

		#region Member Variables

		private Dictionary<string, LevelLoadHandler> levelLoadHandlers;
		private Dictionary<string, LevelFileOptimised> InGameInfo = new Dictionary<string, LevelFileOptimised>();

		#endregion

		#region Delegates

		public delegate void LoadComplete(string levelId, bool success);

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			levelLoadHandlers = new Dictionary<string, LevelLoadHandler>();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Loads the level file for the level.
		/// </summary>
		public bool LoadLevel(LevelData levelData, LoadComplete loadCompleteCallback, bool forceLoad = false)
		{
			if (levelLoadHandlers.TryGetValue(levelData.Id, out var levelLoadHandler))
			{
				if(levelData.WebOrLocal == "web" && forceLoad)
					StartCoroutine(GetBytesFileAndLoad(levelLoadHandler));
					//StartCoroutine(ForceLoad(levelData, loadCompleteCallback, levelLoadHandler));

				levelLoadHandler.refCount++;
				
				if (levelLoadHandler.state == LevelLoadHandler.State.Loaded)
				{
					// Level already successfully loaded
					return false;
				}

				levelLoadHandler.loadCompleteCallbacks.Add(loadCompleteCallback);
			}
			else
			{
				// Start loading the level
				//TODO: Start loading levels here

				/*
					byte[] bytesArray = bytes.ToArray();
					System.IO.File.WriteAllBytes(outPath, bytesArray);
				*/

				LevelLoadHandler llh = CreateLoadHandler(levelData, loadCompleteCallback);

				if(llh.web_or_local == "web")
				{
					if((levelData.LevelSaveData.isCompleted || levelData.LevelSaveData.coloredRegions.Count == 0) && !forceLoad )
					{
						print( $"this level is either Complete: {levelData.LevelSaveData.isCompleted} or has never been colored:  {levelData.LevelSaveData.coloredRegions.Count == 0}");
						//return false;
						//StartCoroutine(GetBytesFileAndLoad(llh));
					}
					else
						StartCoroutine(GetBytesFileAndLoad(llh));
				}
				else
					Load(llh);
			}

			return true;
		}

		IEnumerator ForceLoad(LevelData levelData, LoadComplete loadCompleteCallback, LevelLoadHandler levelLoadHandler)
		{
			StartCoroutine(GetBytesFileAndLoad(levelLoadHandler));

			yield return new WaitForSeconds(3f);

			levelLoadHandler.refCount++;

			if(levelLoadHandler.state == LevelLoadHandler.State.Loaded)
			{
				// Level already successfully loaded
				yield break;
			}

			levelLoadHandler.loadCompleteCallbacks.Add(loadCompleteCallback);
		}

		public void ReleaseLevel(string levelId)
		{
			if (levelLoadHandlers.TryGetValue(levelId, out var levelLoadHandler))
			{
				levelLoadHandler.refCount--;

				if (levelLoadHandler.refCount == 0)
				{
					Release(levelLoadHandler);
					levelLoadHandlers.Remove(levelId);
				}
			}
		}

		public LevelFileData GetLevelFileData(string levelId)
		{
			if (!levelLoadHandlers.TryGetValue(levelId, out var levelLoadHandler))
			{
				Debug.LogErrorFormat("[LoadManager] GetLevelFileData: Level {0} is not loaded", levelId);
				return null;
			}

			if (levelLoadHandler.state != LevelLoadHandler.State.Loaded)
			{
				Debug.LogErrorFormat("[LoadManager] GetLevelFileData: Level {0} has not finished loading - state: {1}", levelId, levelLoadHandler.state);
				return null;
			}

			return levelLoadHandler.levelFileData;
		}

		public Sprite GetRegionSprite(string levelId, int atlasIndex)
		{
			if (!levelLoadHandlers.TryGetValue(levelId, out var levelLoadHandler))
			{
				Debug.LogErrorFormat("[LoadManager] GetRegionSprite: Level {0} is not loading", levelId);
				return null;
			}

			if (levelLoadHandler.state != LevelLoadHandler.State.Loaded)
			{
				Debug.LogErrorFormat("[LoadManager] GetRegionSprite: Level {0} has not finished loading - state: {1}", levelId, levelLoadHandler.state);
				return null;
			}

			if (atlasIndex < 0 || atlasIndex >= levelLoadHandler.atlasSprites.Length)
			{
				Debug.LogErrorFormat("[LoadManager] GetRegionSprite: Invalid region index {0} for level {1} which has {2} region sprites", atlasIndex, levelId, levelLoadHandler.atlasSprites.Length);
			}

			return levelLoadHandler.atlasSprites[atlasIndex];
		}

		#endregion

		#region Private Methods

		private LevelLoadHandler CreateLoadHandler(LevelData levelData, LoadComplete loadCompleteCallback)
		{
			LevelLoadHandler levelLoadHandler = new LevelLoadHandler()
			{
				web_or_local = levelData.WebOrLocal,
				atlases_in_web = levelData.Atlases_In_Web,
				levelId = levelData.Id,
				assetPath = levelData.AssetPath,
				BytesInWeb = levelData.BytesInWeb,
				state = LevelLoadHandler.State.Loading,
				refCount = 1,
				loadCompleteCallbacks = new List<LoadComplete>() { loadCompleteCallback }
			};

			levelLoadHandlers.Add(levelLoadHandler.levelId, levelLoadHandler);

			return levelLoadHandler;
		}

		private void SaveTexture(Texture2D texture, string folderName, string fileName)
		{
			byte[] bytes = texture.EncodeToPNG();
			var dirPath = Application.persistentDataPath + "/" + folderName;

			if(!System.IO.Directory.Exists(dirPath))
			{
				System.IO.Directory.CreateDirectory(dirPath);
			}

			System.IO.File.WriteAllBytes(dirPath + "/" + fileName + ".png", bytes);

			Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + dirPath);

			if(!InGameInfo.ContainsKey(folderName))
				InGameInfo.Add(folderName, new LevelFileOptimised());

			if(InGameInfo[folderName].atlases == null )
				InGameInfo[folderName].atlases = new List<Sprite>();

#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh();
#endif
		}

		private void SaveTextFile(byte[] bytesArray, string folderName, string fileName)
		{
			var dirPath = Application.persistentDataPath + "/" + folderName;

			if(!System.IO.Directory.Exists(dirPath))
			{
				System.IO.Directory.CreateDirectory(dirPath);
			}

			System.IO.File.WriteAllBytes(dirPath + "/" + fileName, bytesArray);

			if(!InGameInfo.ContainsKey(folderName))
				InGameInfo.Add(folderName, new LevelFileOptimised());

			InGameInfo[folderName].bytesArray = new byte[bytesArray.Length]; 
			InGameInfo[folderName].bytesArray = bytesArray; 

#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh();
#endif
		}

		private  IEnumerator LoadCoroutine(LevelLoadHandler levelLoadHandler, LevelFileData lfd)
		{
			for(int i = 0; i < lfd.atlases; i++)
			{
				string spriteAssetPath = levelLoadHandler.atlases_in_web[i];

				Sprite sprite;

				byte[] fileData;
				Texture2D tex = null;

				string folderName = levelLoadHandler.assetPath.Split('/')[3];
				string filePath = Application.persistentDataPath + "/" + folderName + "/" + "atlas_" + i + ".png";

				if( System.IO.File.Exists( filePath ))
				{
					if(!InGameInfo.ContainsKey(folderName))
						InGameInfo.Add(folderName, new LevelFileOptimised());

					if(InGameInfo[folderName].atlases == null)
						InGameInfo[folderName].atlases = new List<Sprite>();

					if(InGameInfo[folderName].atlases.Any(atlases => atlases.name == "atlas_" + i))
					{
						sprite = InGameInfo[folderName].atlases.First(atlases => atlases.name == "atlas_" + i);
					}
					else
					{
						fileData = System.IO.File.ReadAllBytes(filePath);
						tex = new Texture2D(2, 2);
						tex.LoadImage(fileData);

						sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
						sprite.name = "atlas_" + i;

						if(!InGameInfo.ContainsKey(folderName))
							InGameInfo.Add(folderName, new LevelFileOptimised());

						if(InGameInfo[folderName].atlases == null)
							InGameInfo[folderName].atlases = new List<Sprite>();

						InGameInfo[folderName].atlases.Add(sprite);
					}
				}	
				else
				{
					UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(spriteAssetPath);
					//UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(spriteAssetPath);
					yield return webRequest.SendWebRequest();

					if(webRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
					{
						var catName = new Dictionary<string, object>();
						catName.Add(webRequest.result.ToString(), spriteAssetPath + ": Error: " + webRequest.error);
						AnalyticEvents.ReportEvent("atlases_not_loaded", catName);
					}

					switch(webRequest.result)
					{
						case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
							Debug.LogError(spriteAssetPath + ": Error: " + webRequest.error + ": Error Type: " + webRequest.result);
							break;
						case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
							Debug.LogError(spriteAssetPath + ": Error: " + webRequest.error + ": Error Type: " + webRequest.result);
							break;
						case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
							Debug.LogError(spriteAssetPath + ": HTTP Error: " + webRequest.error + ": Error Type: " + webRequest.result);
							break;
						case UnityEngine.Networking.UnityWebRequest.Result.Success:
							Debug.Log(spriteAssetPath + ":\nReceived: " + webRequest.downloadHandler.text);
							break;
					}

					tex = ((UnityEngine.Networking.DownloadHandlerTexture)webRequest.downloadHandler).texture;
					SaveTexture(tex, folderName, "atlas_" + i);

					sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
					sprite.name = "atlas_" + i;

					InGameInfo[folderName].atlases.Add(sprite);
				}

				if( sprite == null )
				{
					LoadFinished(levelLoadHandler, "Failed to sprite at " + spriteAssetPath);
					yield break;
				}

				levelLoadHandler.atlasSprites[i] = sprite;

				if(levelLoadHandler.refCount == 0)
				{
					// If refCount is 0 now then the callers no longer need this levels assets, release any loaded sprites and return
					Release(levelLoadHandler);
					yield break;
				}
			}

			LoadFinished(levelLoadHandler);
		}

		private IEnumerator GetBytesFileAndLoad( LevelLoadHandler llh )
		{
			byte[] bytesArray;

			string folderName = llh.assetPath.Split('/')[3];
			string filePath = Application.persistentDataPath + "/" + folderName + "/bytes.bytes";

			//bytesArray = System.IO.File.ReadAllBytes(filePath);

			if(System.IO.File.Exists(filePath))
			{
				if(!InGameInfo.ContainsKey(folderName))
					InGameInfo.Add(folderName, new LevelFileOptimised());

				if(InGameInfo[folderName].bytesArray == null || InGameInfo[folderName].bytesArray.Length == 0 )
				{
					bytesArray = System.IO.File.ReadAllBytes(filePath);
					InGameInfo[folderName].bytesArray = bytesArray;
				}
				else
					bytesArray = InGameInfo[folderName].bytesArray;
			}
			else
			{
				UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Get(llh.BytesInWeb);
				yield return webRequest.SendWebRequest();

				switch(webRequest.result)
				{
					case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
					case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
						Debug.LogError(llh.BytesInWeb + ": Error: " + webRequest.error);
						break;
					case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
						Debug.LogError(llh.BytesInWeb + ": HTTP Error: " + webRequest.error);
						break;
					case UnityEngine.Networking.UnityWebRequest.Result.Success:
						Debug.Log(llh.BytesInWeb + ":\nReceived: " + webRequest.downloadHandler.text);
						break;
				}

				bytesArray = webRequest.downloadHandler.data;

				SaveTextFile(bytesArray, folderName, "/bytes.bytes");
			}

			Load(llh, bytesArray);
		}

		private async void Load(LevelLoadHandler levelLoadHandler, byte[] _bytes = null )
		{
			//Debug.Log("[LoadManager] Loading level " + levelLoadHandler.levelId + " AssetPath: " + levelLoadHandler.assetPath);

			byte[] bytes;
			TextAsset bytesFile = new TextAsset();

			if(_bytes == null)
			{
				bytesFile = await Addressables.LoadAssetAsync<TextAsset>(levelLoadHandler.assetPath + "/bytes.bytes").Task;
				bytes = new byte[bytesFile.bytes.Length];
				bytes = bytesFile.bytes;

			}
			else
			{
				bytes = new byte[_bytes.Length];
				bytes = _bytes;
			}

			//if(bytesFile == null)
			if(bytes.Length == 0 )
			{
				LoadFinished(levelLoadHandler, "Failed to load bytes.bytes file");
				return;
			}

			//var worker = new LoadLevelFileDataWorker(bytesFile.bytes);
			var worker = new LoadLevelFileDataWorker(bytes);

			worker.StartWorker();

			while (!worker.Stopped)
			{
				await Task.Delay(100);
			}

			LevelFileData lfd = worker.outLevelFileData;

			levelLoadHandler.levelFileData = lfd;

			if(bytesFile.bytes == null && bytesFile.bytes.Length == 0)
				Addressables.Release(bytesFile);

			if (levelLoadHandler.refCount == 0)
			{
				// If refCount is 0 now then the callers no longer need this levels assets, just return now
				return;
			}

			levelLoadHandler.atlasSprites = new Sprite[lfd.atlases];

			if(levelLoadHandler.web_or_local != "web")
			{
				for (int i = 0; i < lfd.atlases; i++)
				{
					//Debug.Log("[LoadManager] Loading sprite " + spriteAssetPath);

					Sprite sprite;

					string spriteAssetPath = levelLoadHandler.assetPath + string.Format("/atlas_" + i + ".png", i);

					sprite = await Addressables.LoadAssetAsync<Sprite>(spriteAssetPath).Task;

					if(sprite == null)
					{
						LoadFinished(levelLoadHandler, "Failed to sprite at " + spriteAssetPath);
						return;
					}

					levelLoadHandler.atlasSprites[i] = sprite;

					if (levelLoadHandler.refCount == 0)
					{
						// If refCount is 0 now then the callers no longer need this levels assets, release any loaded sprites and return
						Release(levelLoadHandler);
						return;
					}
				}

				LoadFinished(levelLoadHandler);
			}
			else
			{
				StartCoroutine(LoadCoroutine(levelLoadHandler, lfd));
			}
		}

		private void LoadFinished(LevelLoadHandler levelLoadHandler, string errorMessage = null)
		{
			bool success = string.IsNullOrEmpty(errorMessage);

			if (success)
			{
				levelLoadHandler.state = LevelLoadHandler.State.Loaded;
			}
			else
			{
				Debug.LogErrorFormat("[LoadManager] Error loading level: Id {0}, AssetPath {1}, Error: {2}", levelLoadHandler.levelId, levelLoadHandler.assetPath, errorMessage);
				levelLoadHandlers.Remove(levelLoadHandler.levelId);
				Release(levelLoadHandler);
			}

			for (int i = 0; i < levelLoadHandler.loadCompleteCallbacks.Count; i++)
			{
				levelLoadHandler.loadCompleteCallbacks[i].Invoke(levelLoadHandler.levelId, success);
			}
		}

		private void Release(LevelLoadHandler levelLoadHandler)
		{
			if (levelLoadHandler.atlasSprites != null)
			{
				for (int i = 0; i < levelLoadHandler.atlasSprites.Length; i++)
				{
					Sprite sprite = levelLoadHandler.atlasSprites[i];

					if (sprite != null)
					{
						if(levelLoadHandler.web_or_local != "web" )
							Addressables.Release(sprite);
						/*else
							Destroy(sprite);*/

						levelLoadHandler.atlasSprites[i] = null;
					}
				}
			}

			levelLoadHandler.state = LevelLoadHandler.State.Released;
		}

		#endregion
	}
}
