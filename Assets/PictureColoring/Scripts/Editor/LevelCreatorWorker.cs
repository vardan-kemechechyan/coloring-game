using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace BBG.PictureColoring
{
	public class LevelCreatorWorker : Worker
	{
		#region Classes

		public class Settings
		{
			public Color[]		lineTexturePixels;
			public Color[]		colorTexturePixels;
			public Vector2		imageSize;
			public float		lineThreshold;
			public bool			ignoreWhiteRegions;
			public float		regionSizeThreshold;
			public float		colorMergeThreshold;
			public string		outPath;
		}

		private class LineImage
		{
			public int					width;
			public int					height;
			public List<List<Pixel>>	pixels;

			public Pixel GetPixel(int x, int y)
			{
				return (x < 0 || y < 0 || x >= width || y >= height) ? null : pixels[x][y];
			}

			public int GetLineAlpha(int x, int y)
			{
				return (x < 0 || y < 0 || x >= width || y >= height) ? 0 : pixels[x][y].alpha;
			}

			public void SetAlpha(int x, int y, int alpha)
			{
				if (!(x < 0 || y < 0 || x >= width || y >= height)) pixels[x][y].alpha = alpha;
			}
		}

		public class Pixel
		{
			public int		x;
			public int		y;
			public int		alpha;
			public bool		marked;
		}

		public class Region
		{
			public int					regionIndex;
			public int					colorIndex;
			public int					width;
			public int					height;
			public int					minX;
			public int					minY;
			public List<List<Pixel>>	pixels;
			public int[]				numberAreaBounds;
			public int					atlasIndex;
			public float[]				atlasUvs;
		}
		
		private class Cell
		{
			public int			x;
			public int			y;
			public bool			containsPixel;
			public CellLocation	location;
			public ulong		marker;
			public int			insideAreaIndex;
		}

		public class AlgoProgress
		{
			public enum Step
			{
				LoadingTextures,
				GatheringRegions,
				PackingRegions,
				CreateFiles
			}

			public int	totalRegions;
			public int	curRegion;
			public Step	regionStep;
			public int	totalPoints;
			public int	numPointsLeft;

			public string	batchModeFilename;
			public int		curBatchFile;
		}



		public class PackedRegion
		{
			public Region region;
			public int startX;
			public int startY;
		}

		public class TextureAtlasInfo
		{
			public int width;
			public int height;
			public List<PackedRegion> packedRegions;
		}

		#endregion // Classes

		#region Enums

		public enum CellLocation
		{
			Unknown,
			Outside,
			Inside
		}

		#endregion // Enums

		#region Member Variables
		
		public Settings			settings;
		private bool			batchMode;
		private int				curBatchIndex;
		private List<string>	coloredFiles;
		private List<string>	lineFiles;

		private readonly object	batchOperationsLock = new object();
		private bool			waitingForFilesToCreate;
		private bool			batchNeedImagePixels;
		private string			batchLineFilePath;
		private string			batchColoredFilePath;
		private Color[]			batchLineTexturePixels;
		private Color[]			batchColorTexturePixels;
		private Vector2			batchImageSize;
		private string			bacthLoadTextureError;

		private readonly object	algoProgressLock = new object();
		private AlgoProgress 	algoProgress;

		#endregion // Member Variables

		#region Properties

		public bool WaitingForFilesToCreate
		{
			get { lock (batchOperationsLock) return waitingForFilesToCreate; }
			set { lock (batchOperationsLock) waitingForFilesToCreate = value; }
		}

		public bool BatchNeedImagePixels
		{
			get { lock (batchOperationsLock) return batchNeedImagePixels; }
			set { lock (batchOperationsLock) batchNeedImagePixels = value; }
		}
		
		public string BatchLineFilePath
		{
			get { lock (batchOperationsLock) return batchLineFilePath; }
			set { lock (batchOperationsLock) batchLineFilePath = value; }
		}
		
		public string BatchColoredFilePath
		{
			get { lock (batchOperationsLock) return batchColoredFilePath; }
			set { lock (batchOperationsLock) batchColoredFilePath = value; }
		}
		
		public Color[] BatchLineTexturePixels
		{
			get { lock (batchOperationsLock) return batchLineTexturePixels; }
			set { lock (batchOperationsLock) batchLineTexturePixels = value; }
		}
		
		public Color[] BatchColorTexturePixels
		{
			get { lock (batchOperationsLock) return batchColorTexturePixels; }
			set { lock (batchOperationsLock) batchColorTexturePixels = value; }
		}
		
		public Vector2 BatchImageSize
		{
			get { lock (batchOperationsLock) return batchImageSize; }
			set { lock (batchOperationsLock) batchImageSize = value; }
		}
		
		public string BatchLoadTextureError
		{
			get { lock (batchOperationsLock) return bacthLoadTextureError; }
			set { lock (batchOperationsLock) bacthLoadTextureError = value; }
		}
		
		public AlgoProgress.Step ProgressStep
		{
			get { lock (algoProgressLock) return algoProgress.regionStep; }
			set { lock (algoProgressLock) algoProgress.regionStep = value; }
		}

		public string ProgressBatchFilename
		{
			get { lock (algoProgressLock) return algoProgress.batchModeFilename; }
			set { lock (algoProgressLock) algoProgress.batchModeFilename = value; }
		}

		public int ProgressCurBatchFile
		{
			get { lock (algoProgressLock) return algoProgress.curBatchFile; }
			set { lock (algoProgressLock) algoProgress.curBatchFile = value; }
		}

		public List<Region>				OutRegions		{ get; private set; }
		public List<Color>				OutColors		 { get; private set; }
		public List<TextureAtlasInfo>	OutAtlasInfos	 { get; private set; }

		#endregion // Properties

		public LevelCreatorWorker(Settings settings)
		{
			this.settings		= settings;
			this.algoProgress	= new AlgoProgress();
		}

		public LevelCreatorWorker(Settings settings, List<string> coloredFiles, List<string> lineFiles)
		{
			this.settings		= settings;
			this.batchMode		= true;
			this.coloredFiles	= coloredFiles;
			this.lineFiles		= lineFiles;
			this.algoProgress	= new AlgoProgress();
		}

		protected override void Begin()
		{

		}

		protected override void DoWork()
		{
			if (batchMode)
			{
				// We need to load the png into a Texture2D and get the pixels since thats the only way we can get the images pixels in a readable format.
				// However that can only be done on the main thread but we don't want to load all pngs at once before the algo starts because if there are
				// alot of images it could freeze Unity for a while not to mention the amount of memory it could take. So we set some properties which
				// signal the LevelCreatorWindow on the main thread to load the png and set the corresponding pixels.
				BatchLineFilePath		= lineFiles[curBatchIndex];
				BatchColoredFilePath	= coloredFiles[curBatchIndex];
				BatchNeedImagePixels	= true;

				ProgressStep			= AlgoProgress.Step.LoadingTextures;
				ProgressBatchFilename	= System.IO.Path.GetFileNameWithoutExtension(BatchColoredFilePath);
				ProgressCurBatchFile	= curBatchIndex;

				// Wait for the main thread to set BatchNeedImagePixels back to false, that is when we know it has set the pixels and image size
				while (BatchNeedImagePixels)
				{
					System.Threading.Thread.Sleep(100);
				}

				string loadError = BatchLoadTextureError;

				if (!string.IsNullOrEmpty(loadError))
				{
					Debug.LogError(loadError);
					curBatchIndex++;
					return;
				}

				// Set the values in settings
				settings.lineTexturePixels	= BatchLineTexturePixels;
				settings.colorTexturePixels	= BatchColorTexturePixels;
				settings.imageSize			= BatchImageSize;
			}

			List<Region>			regions;
			List<Color>				colors;
			List<TextureAtlasInfo>	atlasInfo;

			ProgressStep = AlgoProgress.Step.GatheringRegions;

			ProcessTextures(out regions, out colors);

			ProgressStep = AlgoProgress.Step.PackingRegions;

			atlasInfo = PackRegions(new List<Region>(regions));

			OutRegions		= regions;
			OutColors		= colors;
			OutAtlasInfos	= atlasInfo;
			WaitingForFilesToCreate = true;

			ProgressStep = AlgoProgress.Step.CreateFiles;

			while (WaitingForFilesToCreate)
			{
				System.Threading.Thread.Sleep(100);
			}

			if (!batchMode || curBatchIndex == coloredFiles.Count)
			{
				Stop();
				return;
			}

			curBatchIndex++;
		}
		
		/// <summary>
		/// Processes the line and color textures, splitting the images pixels into regions seperated by line pixels and getting the colors of all the regions
		/// </summary>
		private void ProcessTextures(out List<Region> regions, out List<Color> colors)
		{
			regions		= new List<Region>();
			colors		= new List<Color>();

			LineImage lineImage	= CreateLineImage();

			for (int x = 0; x < lineImage.width; x++)
			{
				for (int y = 0; y < lineImage.height; y++)
				{
					Pixel pixel = lineImage.GetPixel(x, y);

					// Check if the pixel has been marked
					if (!pixel.marked && !IsLinePixel(pixel.alpha))
					{
						List<Pixel> pixelsInRegion = new List<Pixel>();

						// Get the regio of pixels
						int numTransparent = GetPixelsInRegion(pixel, lineImage, pixelsInRegion);

						// Check if the region is big enough to be consider part of the final picture
						if (numTransparent < settings.regionSizeThreshold)
						{
							// Region is to small
							continue;
						}

						Region	region		= CreateRegion(pixelsInRegion);
						Color	regionColor	= GetRegionColor(region);

						bool noColorRegion = (regionColor.a < 1 || (settings.ignoreWhiteRegions && regionColor == Color.white));

						region.regionIndex = regions.Count;
						region.colorIndex = noColorRegion ? -1 : GetColorIndex(colors, regionColor);

						regions.Add(region);
					}
				}
			}
		}

		private LineImage CreateLineImage()
		{
			LineImage lineImage = new LineImage();

			lineImage.width		= (int)settings.imageSize.x;
			lineImage.height	= (int)settings.imageSize.y;
			lineImage.pixels	= new List<List<Pixel>>();

			for (int x = 0; x < lineImage.width; x++)
			{
				lineImage.pixels.Add(new List<Pixel>());

				for (int y = 0; y < lineImage.height; y++)
				{
					Color color = GetPixel(x, y, settings.lineTexturePixels, lineImage.width);
					Pixel pixel = new Pixel();

					pixel.x		= x;
					pixel.y		= y;

					if (color.a < 1)
					{
						pixel.alpha = Mathf.RoundToInt(color.a * 255f);
					}
					else
					{
						pixel.alpha	= 255 - Mathf.RoundToInt(color.grayscale * 255f);
					}

					lineImage.pixels[x].Add(pixel);
				}
			}

			return lineImage;
		}

		private Color GetPixel(int x, int y, Color[] pixels, int width)
		{
			return pixels[x + y * width];
		}

		private int GetPixelsInRegion(Pixel startPixel, LineImage workingImage, List<Pixel> pixelsInRegion)
		{
			int numTransparent = 0;

			Stack<Pixel> pixelsToCheck = new Stack<Pixel>();

			pixelsToCheck.Push(startPixel);

			while (pixelsToCheck.Count > 0)
			{
				Pixel pixel = pixelsToCheck.Pop();

				if (pixel != null && !pixel.marked)
				{
					pixel.marked = true;

					if (!IsLinePixel(pixel.alpha))
					{
						pixelsInRegion.Add(pixel);

						if (pixel.alpha == 0)
						{
							numTransparent++;
						}

						pixelsToCheck.Push(workingImage.GetPixel(pixel.x + 1, pixel.y));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x - 1, pixel.y));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x, pixel.y + 1));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x, pixel.y - 1));
					}
				}
			}

			return numTransparent;
		}

		private bool IsLinePixel(int alpha)
		{
			return alpha >= settings.lineThreshold;
		}

		private Region CreateRegion(List<Pixel> pixelsInRegion)
		{
			Region region = new Region();

			region.pixels = new List<List<Pixel>>();

			int minX = int.MaxValue;
			int maxX = int.MinValue;
			int minY = int.MaxValue;
			int maxY = int.MinValue;

			// Get the min/max x and y for the pixels in the region
			for (int i = 0; i < pixelsInRegion.Count; i++)
			{
				Pixel pixel = pixelsInRegion[i];

				minX = Mathf.Min(minX, pixel.x);
				maxX = Mathf.Max(maxX, pixel.x);
				minY = Mathf.Min(minY, pixel.y);
				maxY = Mathf.Max(maxY, pixel.y);
			}

			int regionWidth		= maxX - minX + 1;
			int regionHeight	= maxY - minY + 1;

			// Create the new region pixels matrix
			for (int x = 0; x < regionWidth; x++)
			{
				region.pixels.Add(new List<Pixel>());

				for (int y = 0; y < regionHeight; y++)
				{
					region.pixels[x].Add(null);
				}
			}

			// Add all the pixels to the matrix in their proper location
			for (int i = 0; i < pixelsInRegion.Count; i++)
			{
				Pixel pixel = pixelsInRegion[i];

				int regionX = pixel.x - minX;
				int regionY = pixel.y - minY;

				region.pixels[regionX][regionY] = pixel;
			}

			region.width	= regionWidth;
			region.height	= regionHeight;
			region.minX		= minX;
			region.minY		= minY;
			region.numberAreaBounds	= FindNumberLocation(region.pixels, regionWidth, regionHeight);

			return region;
		}

		private int[] FindNumberLocation(List<List<Pixel>> regionPixels, int regionWidth, int regionHeight)
		{
			int[] histogram = new int[regionWidth];

			int[] maxAreaBounds = new int[4];

			for (int row = 0; row < regionHeight; row++)
			{
				// Update heights in histogram
				for (int col = 0; col < regionWidth; col++)
				{
					Pixel	pixel = regionPixels[col][row];
					bool	isOne = pixel != null && !IsLinePixel(pixel.alpha);

					histogram[col] = isOne ? histogram[col] + 1 : 0;
				}

				// Calculate new largest rectangle using histogram
				Stack<int>	stack	= new Stack<int>(); 
				int			i		= 0; 

				while (i < regionWidth) 
				{ 
					if (stack.Count == 0 || histogram[stack.Peek()] <= histogram[i]) 
					{ 
						stack.Push(i++); 
					} 
					else
					{ 
						int topIndex = stack.Pop();

						int[] areaBounds =
						{
							(stack.Count == 0) ? 0 : stack.Peek() + 1,
							row - histogram[topIndex] + 1,
							i - 1,
							row
						};

						maxAreaBounds = MaxBounds(maxAreaBounds, areaBounds);
					} 
				} 

				while (stack.Count > 0) 
				{ 
					int topIndex = stack.Pop();

					int[] areaBounds =
					{
						(stack.Count == 0) ? 0 : stack.Peek() + 1,
						row - histogram[topIndex] + 1,
						i - 1,
						row
					};

					maxAreaBounds = MaxBounds(maxAreaBounds, areaBounds);
				}
			}

			return maxAreaBounds;
		}

		private int[] MaxBounds(int[] maxBounds, int[] newBounds)
		{
			return Mathf.Min(newBounds[2] - newBounds[0], newBounds[3] - newBounds[1]) > Mathf.Min(maxBounds[2] - maxBounds[0], maxBounds[3] - maxBounds[1])
				        ? newBounds
					    : maxBounds;
		}

		private Color GetRegionColor(Region region)
		{
			int numAreaWidth	= (region.numberAreaBounds[2] - region.numberAreaBounds[0]);
			int numAreaHeight	= (region.numberAreaBounds[3] - region.numberAreaBounds[1]);

			// Get the color of the region
			int middleX = region.minX + region.numberAreaBounds[0] + Mathf.FloorToInt(numAreaWidth / 2f);
			int middleY = region.minY + region.numberAreaBounds[1] + Mathf.FloorToInt(numAreaHeight / 2f);

			return GetPixel(middleX, middleY, settings.colorTexturePixels, (int)settings.imageSize.x);
		}

		private int GetColorIndex(List<Color> colors, Color newColor)
		{
			if (colors.Count == 0)
			{
				colors.Add(newColor);

				return 0;
			}

			float colorDiff;

			int colorIndex = GetClosestColor(newColor, colors, out colorDiff);

			if (colorDiff <= settings.colorMergeThreshold)
			{
				return colorIndex;
			}

			colors.Add(newColor);

			return colors.Count - 1;
		}
        
        /// <summary>
        /// Gets the PaletteItem that is closest to the given PaletteItem
        /// </summary>
		public int GetClosestColor(Color toColor, List<Color> colors, out float diff)
        {
			int		closestColorIndex	= 0;
            float	minDiff				= float.MaxValue;

            for (int i = 0; i < colors.Count; i++)
            {
				Color color = colors[i];

                if (toColor == color)
                {
                    diff = 0f;

                    return i;
                }

				float colorDiff = ColorUtils.GetColorDiff(toColor, color);

                if (colorDiff < minDiff)
                {
					closestColorIndex	= i;
                    minDiff				= colorDiff;
                }
            }

            diff = minDiff;

			return closestColorIndex;
        }

		int maxPackSize = 11;
		int packSizeStart = 9;
		int packPadding = 4;
		
		private List<TextureAtlasInfo> PackRegions(List<Region> regions)
		{
			regions.Sort((Region r1, Region r2) => { return r2.width * r2.height - r1.width * r1.height; });

			List<TextureAtlasInfo> textureAtlases = new List<TextureAtlasInfo>();

			int index = 0;
			while (index < regions.Count)
			{
				int packedWidth;
				int packedHeight;
				List<PackedRegion> packedRegions;

				PackRegions(regions, index, out packedRegions, out packedWidth, out packedHeight);

				textureAtlases.Add(new TextureAtlasInfo()
				{
					width = packedWidth,
					height = packedHeight,
					packedRegions = packedRegions
				});

				index += packedRegions.Count;
			}

			return textureAtlases;
		}

		private bool PackRegions(List<Region> regions, int index, out List<PackedRegion> packedRegions, out int packedWidth, out int packedHeight)
		{
			int curPackWidth = packSizeStart;
			int curPackHeight = packSizeStart;

			packedWidth = 0;
			packedHeight = 0;
			packedRegions = new List<PackedRegion>();

			while (curPackWidth <= maxPackSize && curPackHeight <= maxPackSize)
			{
				packedWidth = (int)Mathf.Pow(2, curPackWidth);
				packedHeight = (int)Mathf.Pow(2, curPackHeight);

				packedRegions.Clear();

				if (PackRegions(packedWidth, packedHeight, regions, index, packedRegions))
				{
					return true;
				}
				else
				{
					if (curPackWidth != curPackHeight)
					{
						curPackHeight = curPackWidth;
					}
					else
					{
						curPackWidth++;
					}
				}
			}

			return false;
		}

		private bool PackRegions(int packWidth, int packHeight, List<Region> regions, int index, List<PackedRegion> inPackedRegions)
		{
			for (int i = index; i < regions.Count; i++)
			{
				var region = regions[i];
				bool packed = false;

				for (int y = 0; y < packHeight; y++)
				{
					if (y + region.height > packHeight) break;

					for (int x = 0; x < packWidth; )
					{
						if (x + region.width > packWidth) break;

						int result = CanPackRegion(x, y, region, inPackedRegions);

						if (result == -1)
						{
							Debug.LogFormat("region:{0} startX:{1} startY:{2} inPackedRegions.Count:{3}", region.regionIndex, x, y, inPackedRegions.Count);

							result = CanPackRegion(x, y, region, inPackedRegions);

							inPackedRegions.Add(new PackedRegion()
							{
								region = region,
								startX = x,
								startY = y
							});

							packed = true;

							break;
						}

						x = result;
					}

					if (packed) break;
				}

				if (!packed)
				{
					return false;
				}
			}

			return true;
		}

		private int CanPackRegion(int startX, int startY, Region region, List<PackedRegion> packedRegions)
		{
			int endX = startX + region.width + packPadding;
			int endY = startY + region.height + packPadding;

			for (int i = 0; i < packedRegions.Count; i++)
			{
				var packRegion = packedRegions[i];

				int prStartX = packRegion.startX - packPadding;
				int prStartY = packRegion.startY - packPadding;
				int prEndX = prStartX + packRegion.region.width + packPadding;
				int prEndY = prStartY + packRegion.region.height + packPadding;

				if (RegionsOverlap(startX, endX, startY, endY, prStartX, prEndX, prStartY, prEndY))
				{
					return prEndX + 1;
				}
			}

			return -1;
		}

		private bool RegionsOverlap(int r1StartX, int r1EndX, int r1StartY, int r1EndY, int r2StartX, int r2EndX, int r2StartY, int r2EndY)
		{
			if (r1StartX >= r2EndX || r2StartX >= r1EndX)
			{
				return false;
			}

			if (r1StartY >= r2EndY || r2StartY >= r1EndY)
			{
				return false;
			}

			return true;
		}
	}
}
