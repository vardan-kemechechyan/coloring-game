using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.PictureColoring
{
	public class LoadLevelFileDataWorker : Worker
	{
		byte[] levelFileBytes;

		public LevelFileData outLevelFileData;

		public LoadLevelFileDataWorker(byte[] levelFileBytes)
		{
			this.levelFileBytes = levelFileBytes;
		}

		protected override void Begin()
		{
		}

		protected override void DoWork()
		{
			outLevelFileData = ParseLevelFileContents(levelFileBytes);
			Stop();
		}

		/// <summary>
		/// Parses the files contents
		/// </summary>
		private LevelFileData ParseLevelFileContents(byte[] levelFileBytes)
		{
			int byteIndex = 0;

			LevelFileData levelFileData = new LevelFileData();

			// Get the images width/height
			levelFileData.imageWidth = GetNextInt(levelFileBytes, ref byteIndex);
			levelFileData.imageHeight = GetNextInt(levelFileBytes, ref byteIndex);

			// Parse the colors in the level
			ParseColors(levelFileData, levelFileBytes, ref byteIndex);

			// Add a region list for each color in the level
			ParseRegions(levelFileData, levelFileBytes, ref byteIndex);

			return levelFileData;
		}

		/// <summary>
		/// Parses the colors from the contents
		/// </summary>
		private void ParseColors(LevelFileData levelFileData, byte[] levelFileBytes, ref int byteIndex)
		{
			// Get the number of colors in the level
			int numColors = GetNextInt(levelFileBytes, ref byteIndex);

			// Get all the colors
			levelFileData.colors = new List<UnityEngine.Color>(numColors);

			for (int i = 0; i < numColors; i++)
			{
				float r = (float)GetNextInt(levelFileBytes, ref byteIndex) / 255f;
				float g = (float)GetNextInt(levelFileBytes, ref byteIndex) / 255f;
				float b = (float)GetNextInt(levelFileBytes, ref byteIndex) / 255f;

				levelFileData.colors.Add(new UnityEngine.Color(r, g, b, 1f));
			}
		}

		/// <summary>
		/// Parses the regions from the contents
		/// </summary>
		private void ParseRegions(LevelFileData levelFileData, byte[] levelFileBytes, ref int byteIndex)
		{
			int numRegions = GetNextInt(levelFileBytes, ref byteIndex);

			levelFileData.regions = new List<Region>();

			int maxAtlasIndex = 0;

			for (int i = 0; i < numRegions; i++)
			{
				var region = ParseRegion(i, levelFileBytes, ref byteIndex);

				levelFileData.regions.Add(region);

				maxAtlasIndex = Mathf.Max(maxAtlasIndex, region.atlasIndex);
			}

			levelFileData.atlases = maxAtlasIndex + 1;
		}

		/// <summary>
		/// Parses the region.
		/// </summary>
		private Region ParseRegion(int id, byte[] levelFileBytes, ref int byteIndex)
		{
			int colorIndex = GetNextInt(levelFileBytes, ref byteIndex);
			int minX = GetNextInt(levelFileBytes, ref byteIndex);
			int minY = GetNextInt(levelFileBytes, ref byteIndex);
			int regionWidth = GetNextInt(levelFileBytes, ref byteIndex);
			int regionHeight = GetNextInt(levelFileBytes, ref byteIndex);
			int numberX = GetNextInt(levelFileBytes, ref byteIndex);
			int numberY = GetNextInt(levelFileBytes, ref byteIndex);
			int numberSize = GetNextInt(levelFileBytes, ref byteIndex);

			Region region = new Region();

			region.id = id;
			region.colorIndex = colorIndex;
			region.bounds = new RegionBounds(minX, minY, minX + regionWidth - 1, minY + regionHeight - 1);
			region.numberX = numberX;
			region.numberY = numberY;
			region.numberSize = numberSize;
			region.pixelsByX = regionWidth < regionHeight;
			region.pixelsInRegion = new List<List<int[]>>();

			int len = region.pixelsByX ? regionWidth : regionHeight;

			for (int i = 0; i < len; i++)
			{
				int numSubValues = GetNextInt(levelFileBytes, ref byteIndex);

				region.pixelsInRegion.Add(new List<int[]>());

				for (int j = 0; j < numSubValues; j += 2)
				{
					int start = GetNextInt(levelFileBytes, ref byteIndex);
					int end = GetNextInt(levelFileBytes, ref byteIndex);

					region.pixelsInRegion[i].Add(new int[] { start, end });
				}
			}

			region.atlasIndex = GetNextInt(levelFileBytes, ref byteIndex);
			region.atlasUvs = new float[4];

			for (int i = 0; i < 4; i++)
			{
				region.atlasUvs[i] = GetNextFloat(levelFileBytes, ref byteIndex);
			}

			return region;
		}

		private int GetNextInt(byte[] levelFileBytes, ref int byteIndex)
		{
			int val = System.BitConverter.ToInt32(levelFileBytes, byteIndex);

			byteIndex += sizeof(int);

			return val;
		}

		private float GetNextFloat(byte[] levelFileBytes, ref int byteIndex)
		{
			float val = (float)System.BitConverter.ToSingle(levelFileBytes, byteIndex);

			byteIndex += sizeof(float);

			return val;
		}
	}
}
