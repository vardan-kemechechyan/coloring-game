using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace BBG.PictureColoring
{
	public class LevelCreatorWindow : EditorWindow
	{
		#region Enums

		private enum ImportMode
		{
			Single,
			Batch
		}

		#endregion

		#region Member Variables

		private ImportMode	importMode;

		private Texture2D						levelLineTexture;
		private Texture2D						levelColoredTexture;

		private string							batchModeInputFolder;

		private bool							ignoreWhiteRegions;
		private int								lineDarknessThreshold	= 200;
		private int								regionSizeThreshold		= 24;
		private float							colorMergeThreshold		= 0.1f;

		private AddressableAssetGroup			assetGroup;
		private Object							outputFolder;

		private GameManager						gameManagerReference;
		private bool							addToGameManager = true;
		private int								selectedCategoryIndex;

		private string							errorMessage;

		private List<string>					batchColoredFiles;
		private List<string>					batchLineFiles;
		private LevelCreatorWorker				levelCreatorWorker;

		#endregion

		#region Properties

		private string OutputFolderAssetPath
		{
			get { return EditorPrefs.GetString("OutputFolderAssetPath", ""); }
			set { EditorPrefs.SetString("OutputFolderAssetPath", value); }
		}

		private string AddressablesAssetGroupAssetPath
		{
			get { return EditorPrefs.GetString("AddressablesAssetGroupAssetPath", ""); }
			set { EditorPrefs.SetString("AddressablesAssetGroupAssetPath", value); }
		}

		#endregion

		#region Unity Methods

		[MenuItem("Tools/Bizzy Bee Games/Level Creator Window", priority = 200)]
		public static void Init()
		{
			EditorWindow.GetWindow<LevelCreatorWindow>("Level Creator");
		}

		private void OnEnable()
		{
			if (outputFolder == null && !string.IsNullOrEmpty(OutputFolderAssetPath))
			{
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolderAssetPath);
			}

			if (assetGroup == null && !string.IsNullOrEmpty(AddressablesAssetGroupAssetPath))
			{
				assetGroup = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(AddressablesAssetGroupAssetPath);
			}

			if (assetGroup == null)
			{
				FindAddressableAssetGroup();
			}

			// Set the reference to the GameManager in the current open scene
			gameManagerReference = FindObjectOfType<GameManager>();
		}

		private void Update()
		{
			if (gameManagerReference == null)
			{
				gameManagerReference = FindObjectOfType<GameManager>();
			}

			if (levelCreatorWorker != null)
			{
				if (levelCreatorWorker.Stopped)
				{
					Debug.Log("Level creator finished");

					EditorUtility.ClearProgressBar();

					AssetDatabase.Refresh();

					levelCreatorWorker = null;

					return;
				}

				if (importMode == ImportMode.Batch && levelCreatorWorker.BatchNeedImagePixels)
				{
					LoadAndSetWorkerBatchPixels();
				}

				if (levelCreatorWorker.WaitingForFilesToCreate)
				{
					CreateLevelFiles(levelCreatorWorker.OutRegions, levelCreatorWorker.OutColors, levelCreatorWorker.OutAtlasInfos);
					levelCreatorWorker.WaitingForFilesToCreate = false;
				}

				bool cancelled = DisplayProgressBar();

				if (cancelled)
				{
					Debug.Log("Cancelling");

					levelCreatorWorker.Stop();
					levelCreatorWorker = null;

					EditorUtility.ClearProgressBar();
				}
			}
		}

		#endregion

		#region Draw Methods

		private void OnGUI()
		{
			EditorGUILayout.Space();

			BeginBox();

			GUI.enabled = levelCreatorWorker == null;

			GUILayout.Space(2);

			importMode = (ImportMode)EditorGUILayout.EnumPopup("Import Mode", importMode);

			if (importMode == ImportMode.Single)
			{
				EditorGUILayout.Space();

				levelColoredTexture		= EditorGUILayout.ObjectField("Colored Texture", levelColoredTexture, typeof(Texture2D), false, GUILayout.Height(16)) as Texture2D;
				levelLineTexture		= EditorGUILayout.ObjectField("Line Texture", levelLineTexture, typeof(Texture2D), false, GUILayout.Height(16)) as Texture2D;
			}
			else
			{
				EditorGUILayout.HelpBox("To use batch mode, select the folder with your colored and line images. The name of your line image file should be the name of the colored image file with \"-lines\" append to the end. For example if the colored images name is mandala.png then the line image files name should be mandala-lines.png", MessageType.Info);

				EditorGUILayout.Space();

				if (GUILayout.Button("Choose Input Folder"))
				{
					string folder			= string.IsNullOrEmpty(batchModeInputFolder) ? Application.dataPath : batchModeInputFolder;
					string inputFolder		= EditorUtility.OpenFolderPanel("Choose Input Folder", folder, "");

					if (!string.IsNullOrEmpty(inputFolder) && inputFolder != batchModeInputFolder)
					{
						errorMessage = "";

						batchModeInputFolder = inputFolder;

						List<string> missingLineFiles = UpdateBatchFiles();

						if (missingLineFiles != null)
						{
							string errorMessage = "Could not find line files for the following colored image files:";

							for (int i = 0; i < missingLineFiles.Count; i++)
							{
								errorMessage += "\n" + missingLineFiles[i];
							}

							Debug.LogError(errorMessage);
						}
					}
				}

				string message = "";

				if (string.IsNullOrEmpty(batchModeInputFolder))
				{
					message = "<Please choose an input folder>";
				}
				else
				{
					message = batchModeInputFolder + "\n\nImage files found: " + batchColoredFiles.Count;
				}

				EditorGUILayout.HelpBox(message, MessageType.None);
			}

			EditorGUILayout.Space();

			lineDarknessThreshold	= EditorGUILayout.IntSlider("Line Darkness Threshold", lineDarknessThreshold, 255, 0);
			ignoreWhiteRegions		= EditorGUILayout.Toggle("Ignore White Regions", ignoreWhiteRegions);
			regionSizeThreshold		= EditorGUILayout.IntField("Region Size Threshold", regionSizeThreshold);
			colorMergeThreshold		= EditorGUILayout.FloatField("Color Merge Threshold", colorMergeThreshold);

			EditorGUILayout.Space();

			assetGroup = EditorGUILayout.ObjectField("Addressables Asset Group", assetGroup, typeof(AddressableAssetGroup), false) as AddressableAssetGroup;
			outputFolder = EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(Object), false);

			AddressablesAssetGroupAssetPath = (assetGroup != null) ? AssetDatabase.GetAssetPath(assetGroup) : null;
			OutputFolderAssetPath = (outputFolder != null) ? AssetDatabase.GetAssetPath(outputFolder) : null;

			EditorGUILayout.HelpBox("Files will be placed in the folder: " + GetOutputFolderPath(outputFolder).Remove(0, Application.dataPath.Length - "Assets".Length), MessageType.None);

			EditorGUILayout.Space();

			if (gameManagerReference == null)
			{
				EditorGUILayout.HelpBox("Could not find a GameManager in the current open scene.", MessageType.Warning);

				gameManagerReference = EditorGUILayout.ObjectField("Game Manager", gameManagerReference, typeof(GameManager), true) as GameManager;
			}
			else
			{
				if (gameManagerReference.Categories == null || gameManagerReference.Categories.Count == 0)
				{
					EditorGUILayout.HelpBox("GameManager has no categories, create categories on the GameManagers inspector to assign levels to categories.", MessageType.Warning);
				}
				else
				{
					addToGameManager = EditorGUILayout.Toggle("Add To GameManager", addToGameManager);

					if (addToGameManager)
					{
						string[] categoryNames = new string[gameManagerReference.Categories.Count];

						for (int i = 0; i < gameManagerReference.Categories.Count; i++)
						{
							categoryNames[i] = gameManagerReference.Categories[i].displayName;
						}

						selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categoryNames);
					}
					else
					{
						GUI.enabled = false;
						selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, new string[] { "" });
						GUI.enabled = true;
					}
				}
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Create Level Files") && Check())
			{
				if (importMode == ImportMode.Single)
				{
					Process(levelColoredTexture, levelLineTexture);
				}
				else
				{
					ProcessBatch();
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Sync Level Files will go through all the levels added to the GameManager and make sure the asset path is correct in both the level.txt file and Addressable Group.", MessageType.Info);
			if (GUILayout.Button("Sync Level Files"))
			{
				if (assetGroup == null)
				{
					errorMessage = "Addressables Asset Group has not been set";
				}
				if (gameManagerReference == null)
				{
					errorMessage = "No GameManager found in the current scene";
				}
				else
				{
					SyncLevelFiles();
				}
			}

			if (!string.IsNullOrEmpty(errorMessage))
			{
				EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
			}

			GUI.enabled = true;

			EndBox();

			EditorGUILayout.Space();
		}

		/// <summary>
		/// Begins a new box, must call EndBox
		/// </summary>
		private void BeginBox()
		{
			GUILayout.BeginVertical(GUI.skin.box);
		}

		/// <summary>
		/// Ends the box.
		/// </summary>
		private void EndBox()
		{
			GUILayout.EndVertical();
		}

		#endregion

		#region Private Methods

		private bool Check()
		{
			errorMessage = "";

			if (importMode == ImportMode.Batch)
			{
				if (string.IsNullOrEmpty(batchModeInputFolder))
				{
					errorMessage = "Please choose an import folder";
					return false;
				}

				if (batchColoredFiles.Count == 0)
				{
					errorMessage = "There are no images in the selected input folder";
					return false;
				}

				return true;
			}

			if (levelColoredTexture == null && levelLineTexture == null)
			{
				errorMessage = "Please specify a Colored Texture and a Line Texture";
				return false;
			}
			else if (levelColoredTexture == null)
			{
				errorMessage = "Please specify a Colored Texture";
				return false;
			}
			else if (levelLineTexture == null)
			{
				errorMessage = "Please specify a Line Texture";
				return false;
			}

			if (levelLineTexture.width != levelColoredTexture.width ||
			    levelLineTexture.height != levelColoredTexture.height)
			{
				errorMessage = "The Colored Texture and Line Texture are not the same size. The images need to be the same width/height.";
				return false;
			}

			bool isColoredTextureReadable	= CheckIsReadWriteEnabled(levelColoredTexture);
			bool isLineTextureReadable		= CheckIsReadWriteEnabled(levelLineTexture);

			if (!isColoredTextureReadable && !isLineTextureReadable)
			{
				errorMessage = "The Colored Texture and Line Texture are not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}
			else if (!isColoredTextureReadable)
			{
				errorMessage = "The Colored Texture is not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}
			else if (!isLineTextureReadable)
			{
				errorMessage = "The Line Texture is not read/write enabled. Please select the texture in the Project window then in the Inspector window select Read/Write Enabled and click Apply.";
				return false;
			}

			if (assetGroup == null)
			{
				errorMessage = "Addressables Asset Group has not been set";
				return false;
			}

			return true;
		}

		private void Process(Texture2D coloredTexture, Texture2D lineTexture)
		{
			LevelCreatorWorker.Settings settings = new LevelCreatorWorker.Settings();

			settings.lineTexturePixels		= levelLineTexture.GetPixels();
			settings.colorTexturePixels		= levelColoredTexture.GetPixels();
			settings.imageSize				= new Vector2(levelColoredTexture.width, levelColoredTexture.height);
			settings.lineThreshold			= lineDarknessThreshold;
			settings.ignoreWhiteRegions		= ignoreWhiteRegions;
			settings.regionSizeThreshold	= regionSizeThreshold;
			settings.colorMergeThreshold	= colorMergeThreshold;

			levelCreatorWorker = new LevelCreatorWorker(settings);
			levelCreatorWorker.StartWorker();
		}

		private void ProcessBatch()
		{
			LevelCreatorWorker.Settings settings = new LevelCreatorWorker.Settings();

			settings.lineThreshold			= lineDarknessThreshold;
			settings.ignoreWhiteRegions		= ignoreWhiteRegions;
			settings.regionSizeThreshold	= regionSizeThreshold;
			settings.colorMergeThreshold	= colorMergeThreshold;

			levelCreatorWorker = new LevelCreatorWorker(settings, batchColoredFiles, batchLineFiles);
			levelCreatorWorker.StartWorker();
		}
        
		/// <summary>
		/// Gets the full path to the output folder
		/// </summary>
		private string GetOutputFolderPath(Object outputFolder)
		{
			string folderPath = GetFolderAssetPath(outputFolder);

			// If the folder path is null then set the path to the Asset folder
			if (string.IsNullOrEmpty(folderPath))
			{
				return Application.dataPath;
			}

			return Application.dataPath + "/" + folderPath;
		}

		/// <summary>
		/// Gets the folder path.
		/// </summary>
		private string GetFolderAssetPath(Object folderObject)
		{
			if (folderObject != null)
			{
				// Get the full system path to the folder
				string fullPath = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length) + UnityEditor.AssetDatabase.GetAssetPath(folderObject);

				// If it's not a folder then set the path to null so the default path is choosen
				if (!System.IO.Directory.Exists(fullPath))
				{
					return "";
				}

				return UnityEditor.AssetDatabase.GetAssetPath(folderObject).Remove(0, "Assets/".Length);
			}

			return "";
		}

		/// <summary>
		/// Checks if the given texture is read/write enabled in its settings
		/// </summary>
		private bool CheckIsReadWriteEnabled(Texture2D texture)
		{
			if (texture == null)
			{
				return false;
			}

			string			assetPath	= AssetDatabase.GetAssetPath(texture);
			TextureImporter	importer	= AssetImporter.GetAtPath(assetPath) as TextureImporter;

			return importer.isReadable;
		}

		/// <summary>
		/// Updates the list of batch file paths
		/// </summary>
		private List<string> UpdateBatchFiles()
		{
			batchColoredFiles	= new List<string>();
			batchLineFiles		= new List<string>();

			if (System.IO.Directory.Exists(batchModeInputFolder))
			{
				string[] files = System.IO.Directory.GetFiles(batchModeInputFolder, "*.png");

				List<string> coloredFiles	= new List<string>();
				List<string> lineFiles		= new List<string>();

				// Gather all the colored and line files
				for (int i = 0; i < files.Length; i++)
				{
					string filePath = files[i];

					if (System.IO.Path.GetFileNameWithoutExtension(filePath).EndsWith("-lines"))
					{
						lineFiles.Add(filePath);
					}
					else
					{
						coloredFiles.Add(filePath);
					}
				}

				List<string> missingLineFiles = new List<string>();

				// Check that each colored file has a line file
				for (int i = 0; i < coloredFiles.Count; i++)
				{
					string coloredFile		= coloredFiles[i];
					string coloredFileName	= System.IO.Path.GetFileNameWithoutExtension(coloredFile);

					bool foundLineFile = false;

					for (int j = 0; j < lineFiles.Count; j++)
					{
						string lineFile		= lineFiles[j];
						string lineFileName = System.IO.Path.GetFileNameWithoutExtension(lineFile);

						if (coloredFileName + "-lines" == lineFileName)
						{
							batchColoredFiles.Add(coloredFile);
							batchLineFiles.Add(lineFile);
							foundLineFile = true;
							break;
						}
					}

					if (!foundLineFile)
					{
						missingLineFiles.Add(coloredFileName);
					}
				}

				return missingLineFiles.Count == 0 ? null : missingLineFiles;
			}
			else
			{
				batchModeInputFolder = "";
			}

			return null;
		}

		private bool DisplayProgressBar()
		{
			string title	= "Creating Level Files";
			string message	= "";

			if (importMode == ImportMode.Batch)
			{
				title = string.Format("Process image {0} of {1}: {2}", levelCreatorWorker.ProgressCurBatchFile + 1, batchColoredFiles.Count, levelCreatorWorker.ProgressBatchFilename);
			}

			LevelCreatorWorker.AlgoProgress.Step step = levelCreatorWorker.ProgressStep;

			ulong	time = ((ulong)Utilities.SystemTimeInMilliseconds / 300);
			int		dots = (int)(time % 3) + 1;

			switch (step)
			{
				case LevelCreatorWorker.AlgoProgress.Step.LoadingTextures:
				{
					message = AddDots("Loading images", dots);
					break;
				}
				case LevelCreatorWorker.AlgoProgress.Step.GatheringRegions:
				{
					message = AddDots("Parsing textures into pixel regions", dots);
					break;
					}
				case LevelCreatorWorker.AlgoProgress.Step.PackingRegions:
					{
						message = AddDots("Packing regions into texture atlas", dots);
						break;
					}
				case LevelCreatorWorker.AlgoProgress.Step.CreateFiles:
				{
					message = AddDots("Creating level files", dots);
					break;
				}
			}

			float progress = 0;

			return EditorUtility.DisplayCancelableProgressBar(title, message, progress);
		}

		private string AddDots(string message, int dots)
		{
			for (int i = 0; i < dots; i++)
			{
				message += ".";
			}

			return message;
		}

		private void LoadAndSetWorkerBatchPixels()
		{
			string lineFile		= levelCreatorWorker.BatchLineFilePath;
			string coloredFile	= levelCreatorWorker.BatchColoredFilePath;

			Texture2D lineTexture		= new Texture2D(1, 1);
			Texture2D coloredTexture	= new Texture2D(1, 1);

			ImageConversion.LoadImage(lineTexture, System.IO.File.ReadAllBytes(lineFile));
			ImageConversion.LoadImage(coloredTexture, System.IO.File.ReadAllBytes(coloredFile));

			if (lineTexture.width != coloredTexture.width || lineTexture.height != coloredTexture.height)
			{
				levelCreatorWorker.BatchLoadTextureError = string.Format("Error loading {0}, the colored and line images are not the same size.", System.IO.Path.GetFileNameWithoutExtension(coloredFile));
			}
			else
			{
				levelCreatorWorker.BatchLineTexturePixels	= lineTexture.GetPixels();
				levelCreatorWorker.BatchColorTexturePixels	= coloredTexture.GetPixels();
				levelCreatorWorker.BatchImageSize			= new Vector2(lineTexture.width, lineTexture.height);
			}

			DestroyImmediate(lineTexture);
			DestroyImmediate(coloredTexture);

			levelCreatorWorker.BatchNeedImagePixels = false;
		}

		private void CreateLevelFiles(List<LevelCreatorWorker.Region> regions, List<Color> colors, List<LevelCreatorWorker.TextureAtlasInfo> atlasInfos)
		{
			string folderPath = GetOutputFolderPath(outputFolder);

			if (importMode == ImportMode.Batch)
			{
				folderPath += "/" + levelCreatorWorker.ProgressBatchFilename;
			}

			if (!System.IO.Directory.Exists(folderPath))
			{
				System.IO.Directory.CreateDirectory(folderPath);
			}

			WriteSpriteAtlas(folderPath, atlasInfos);

			string id = WriteLevelByteFile(levelCreatorWorker.settings.imageSize, regions, colors, folderPath + "/bytes.bytes");

			WriteLevelTxtFile(folderPath, id);

			AssetDatabase.Refresh();

			AddToAddressablesGroup(folderPath, atlasInfos.Count);

			AddToGameManager(folderPath);
		}

		private Dictionary<int, float[]> WriteSpriteAtlas(string folderPath, List<LevelCreatorWorker.TextureAtlasInfo> atlasInfos)
		{
			Dictionary<int, float[]> regionUvs = new Dictionary<int, float[]>();

			for (int i = 0; i < atlasInfos.Count; i++)
			{
				var atlasInfo = atlasInfos[i];

				Texture2D atlasTexture = new Texture2D(atlasInfo.width, atlasInfo.height, TextureFormat.RGBA32, false);

				for (int x = 0; x < atlasInfo.width; x++)
				{
					for (int y = 0; y < atlasInfo.height; y++)
					{
						atlasTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
					}
				}

				for (int j = 0; j < atlasInfo.packedRegions.Count; j++)
				{
					var packedRegion = atlasInfo.packedRegions[j];
					var region = packedRegion.region;
					
					region.atlasIndex = i;
					region.atlasUvs = new float[4]
					{
						packedRegion.startX / (float)atlasInfo.width,
						packedRegion.startY / (float)atlasInfo.height,
						(packedRegion.startX + region.width) / (float)atlasInfo.width,
						(packedRegion.startY + region.height) / (float)atlasInfo.height
					};

					for (int x = 0; x < region.pixels.Count; x++)
					{
						for (int y = 0; y < region.pixels[x].Count; y++)
						{
							LevelCreatorWorker.Pixel pixel = region.pixels[x][y];

							if (pixel != null)
							{
								atlasTexture.SetPixel(packedRegion.startX + x, packedRegion.startY + y, new Color(1, 1, 1, 1f - pixel.alpha / 255f));
							}
						}
					}
				}

				System.IO.File.WriteAllBytes(folderPath + "/atlas_" + i + ".png", atlasTexture.EncodeToPNG());
			}

			return regionUvs;
		}

		private void WriteLevelTxtFile(string outFolder, string id)
		{
			string fileContents = "";

			fileContents += id;
			fileContents += "\n" + "Assets" + outFolder.Remove(0, Application.dataPath.Length);

			System.IO.File.WriteAllText(outFolder + "/level.txt", fileContents);
		}

		private string WriteLevelByteFile(Vector2 imageSize, List<LevelCreatorWorker.Region> regions, List<Color> colors, string outPath)
		{
			List<byte> bytes = new List<byte>();

			// Add the size of the image then the number of colors to the values list
			bytes.AddRange(System.BitConverter.GetBytes((int)imageSize.x));
			bytes.AddRange(System.BitConverter.GetBytes((int)imageSize.y));
			bytes.AddRange(System.BitConverter.GetBytes(colors.Count));

			// Add each color to the values list
			for (int i = 0; i < colors.Count; i++)
			{
				Color color = colors[i];

				int r = Mathf.RoundToInt(color.r * 255f);
				int g = Mathf.RoundToInt(color.g * 255f);
				int b = Mathf.RoundToInt(color.b * 255f);

				bytes.AddRange(System.BitConverter.GetBytes(r));
				bytes.AddRange(System.BitConverter.GetBytes(g));
				bytes.AddRange(System.BitConverter.GetBytes(b));
			}

			bytes.AddRange(System.BitConverter.GetBytes(regions.Count));

			for (int i = 0; i < regions.Count; i++)
			{
				var region = regions[i];

				bytes.AddRange(System.BitConverter.GetBytes(region.colorIndex));
				bytes.AddRange(System.BitConverter.GetBytes(region.minX));
				bytes.AddRange(System.BitConverter.GetBytes(region.minY));
				bytes.AddRange(System.BitConverter.GetBytes(region.width));
				bytes.AddRange(System.BitConverter.GetBytes(region.height));

				int numAreaWidth = (region.numberAreaBounds[2] - region.numberAreaBounds[0]);
				int numAreaHeight = (region.numberAreaBounds[3] - region.numberAreaBounds[1]);
				int numX = region.minX + region.numberAreaBounds[0] + Mathf.FloorToInt(numAreaWidth / 2f);
				int numY = region.minY + region.numberAreaBounds[1] + Mathf.FloorToInt(numAreaHeight / 2f);
				int numSize = Mathf.Min(numAreaWidth, numAreaHeight);

				bytes.AddRange(System.BitConverter.GetBytes(numX));
				bytes.AddRange(System.BitConverter.GetBytes(numY));
				bytes.AddRange(System.BitConverter.GetBytes(numSize));

				GetRegionBytes(region, bytes);

				bytes.AddRange(System.BitConverter.GetBytes(region.atlasIndex));

				for (int j = 0; j < 4; j++)
				{
					bytes.AddRange(System.BitConverter.GetBytes(region.atlasUvs[j]));
				}
			}

			byte[] bytesArray = bytes.ToArray();

			System.IO.File.WriteAllBytes(outPath, bytesArray);

			return GetId(bytesArray);
		}

		private void GetRegionBytes(LevelCreatorWorker.Region region, List<byte> bytes)
		{
			bool iterateOverWidth = region.width < region.height;

			int len1 = iterateOverWidth ? region.width : region.height;
			int len2 = iterateOverWidth ? region.height : region.width;

			for (int i = 0; i < len1; i++)
			{
				int start = -1;

				LevelCreatorWorker.Pixel lastPixel = null;

				List<byte> subValues = new List<byte>();
				int numSubValues = 0;

				for (int j = 0; j < len2 + 1; j++)
				{
					int x = iterateOverWidth ? i : j;
					int y = iterateOverWidth ? j : i;

					LevelCreatorWorker.Pixel pixel = (j < len2 ? region.pixels[x][y] : null);

					if (pixel != null && start == -1)
					{
						start = iterateOverWidth ? pixel.y : pixel.x;
					}
					else if (pixel == null && start != -1)
					{
						int end = (iterateOverWidth ? lastPixel.y : lastPixel.x);

						subValues.AddRange(System.BitConverter.GetBytes(start));
						subValues.AddRange(System.BitConverter.GetBytes(end));
						numSubValues += 2;

						start = -1;
					}

					lastPixel = pixel;
				}

				bytes.AddRange(System.BitConverter.GetBytes(numSubValues));
				bytes.AddRange(subValues);
			}
		}

		private void AddToAddressablesGroup(string outFolder, int numAtlases)
		{
			string assetPath = "Assets" + outFolder.Remove(0, Application.dataPath.Length);

			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

			settings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(assetPath + "/bytes.bytes").ToString(), assetGroup);

			for (int i = 0; i < numAtlases; i++)
			{
				settings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(assetPath + "/atlas_" + i + ".png").ToString(), assetGroup);
			}
		}

		private void AddToGameManager(string outFolder)
		{
			if (!addToGameManager || gameManagerReference == null)
			{
				return;
			}

			CategoryData categoryData = gameManagerReference.Categories[selectedCategoryIndex];

			if (categoryData.levels == null)
			{
				categoryData.levels = new List<LevelData>();
			}

			string assetPath = "Assets" + outFolder.Remove(0, Application.dataPath.Length);
			TextAsset levelFileAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath + "/level.txt");

			bool alreadyExists = false;

			for (int j = 0; j < categoryData.levels.Count; j++)
			{
				if (categoryData.levels[j].levelFile == levelFileAsset)
				{
					alreadyExists = true;
					break;
				}
			}

			if (!alreadyExists)
			{
				LevelData levelData = new LevelData();
				levelData.levelFile = levelFileAsset;

				categoryData.levels.Add(levelData);
			}
		}

		/// <summary>
		/// Gets a hash value for the given Texture2D
		/// </summary>
		private string GetId(byte[] bytes)
		{
			// encrypt bytes
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			byte[] hashBytes = md5.ComputeHash(bytes);

			// Convert the encrypted bytes back to a string (base 16)
			string hashString = "";

			for (int i = 0; i < hashBytes.Length; i++)
			{
				hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			}

			return hashString.PadLeft(32, '0');
		}

		private void FindAddressableAssetGroup()
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(AddressableAssetGroup)));

			for (int i = 0; i < guids.Length; i++)
			{
				var group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(AssetDatabase.GUIDToAssetPath(guids[i]));

				if (group != null && !group.ReadOnly)
				{
					assetGroup = group;
					return;
				}
			}
		}

		private void SyncLevelFiles()
		{
			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			var curAssets = new List<AddressableAssetEntry>(assetGroup.entries);

			// Remove everything from the Addressable group
			for (int i = 0; i < curAssets.Count; i++)
			{
				assetGroup.RemoveAssetEntry(curAssets[i]);
			}

			// Go through each level added to the GameManager
			for (int i = 0; i < gameManagerReference.Categories.Count; i++)
			{
				var category = gameManagerReference.Categories[i];

				for (int j = 0; j < category.levels.Count; j++)
				{
					var levelData = category.levels[j];
					var levelFileAssetPath = AssetDatabase.GetAssetPath(levelData.levelFile);

					var levelId = levelData.Id;
					var assetPath = levelFileAssetPath.Substring(0, levelFileAssetPath.LastIndexOf('/'));

					Debug.LogFormat("Write to:\n{0}\n{1}", GetFullPathFromAssetPath(levelFileAssetPath), levelId + "\n" + assetPath);

					//System.IO.File.WriteAllText(GetFullPathFromAssetPath(levelFileAssetPath), levelId + "\n" + assetPath);

					string bytesGuid = AssetDatabase.GUIDFromAssetPath(assetPath + "/bytes.bytes").ToString();

					if (!string.IsNullOrEmpty(bytesGuid))
					{
						settings.CreateOrMoveEntry(bytesGuid, assetGroup);
					}
					else
					{
						Debug.LogError("Could not find " + assetPath + "/bytes.bytes");
					}

					string[] atlasFiles = System.IO.Directory.GetFiles(GetFullPathFromAssetPath(assetPath), "atlas_*.png");

					for (int k = 0; k < atlasFiles.Length; k++)
					{
						string atlasAssetPath = GetAssetPathFromFullPath(atlasFiles[k]);
						string atlasGuid = AssetDatabase.GUIDFromAssetPath(atlasAssetPath).ToString();

						if (!string.IsNullOrEmpty(atlasGuid))
						{
							settings.CreateOrMoveEntry(atlasGuid, assetGroup);
						}
						else
						{
							Debug.LogError("Could not find " + atlasAssetPath);
						}
					}
				}
			}
		}

		private string GetAssetPathFromFullPath(string fullPath)
		{
			return fullPath.Substring(Application.dataPath.Length - "Assets".Length).Replace('\\', '/');
		}

		private string GetFullPathFromAssetPath(string assetPath)
		{
			string dataPath = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length);

			return System.IO.Path.GetFullPath(dataPath + assetPath);
		}

		#endregion
	}
}
