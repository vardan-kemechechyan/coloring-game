using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.PictureColoring
{
    public class PictureImage : Image
    {
		private List<Region> regions;
		private string levelId;
		private int selectedColorIndex;

		public void Setup(List<Region> regions, string levelId)
		{
			this.regions = regions;
			this.levelId = levelId;
			this.SetAllDirty();
		}

		public List<Region> GetRegions()
		{
			return regions;
		}

		public void Clear()
		{
			levelId = null;
			regions = null;
		}

		public void SetSelectedIndex(int selectedColorIndex)
		{
			this.selectedColorIndex = selectedColorIndex;
			this.SetAllDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (string.IsNullOrEmpty(levelId))
			{
				return;
			}

			var levelFileData = LoadManager.Instance.GetLevelFileData(levelId);
			var levelFileSave = GameManager.Instance.GetLevelSaveData(levelId);

			for (int i = 0; i < regions.Count; i++)
			{
				var region = regions[i];
				var color = Color.white;

				bool isRegionColored = levelFileSave.coloredRegions.Contains(region.id);

				Vector2 vMin = new Vector2(region.bounds.minX, region.bounds.minY);
				Vector2 vMax = new Vector2(region.bounds.maxX, region.bounds.maxY);
				Vector2 uvMin = new Vector2(region.atlasUvs[0], region.atlasUvs[1]);
				Vector2 uvMax = new Vector2(region.atlasUvs[2], region.atlasUvs[3]);
				Vector2 uv1Min = Vector2.zero;
				Vector2 uv1Max = Vector2.zero;

				if (isRegionColored && region.colorIndex >= 0 && region.colorIndex < levelFileData.colors.Count)
				{
					// If the region is colored in then set the image to the regions color
					color = levelFileData.colors[region.colorIndex];
				}
				else if (selectedColorIndex == region.colorIndex)
				{
					uv1Min = new Vector2(region.bounds.minX / rectTransform.rect.width, region.bounds.minY / rectTransform.rect.width);
					uv1Max = new Vector2(region.bounds.maxX / rectTransform.rect.width, region.bounds.maxY / rectTransform.rect.width);
				}

				vh.AddVert(new Vector3(vMin.x, vMin.y), color, new Vector2(uvMin.x, uvMin.y), new Vector2(uv1Min.x, uv1Min.y), Vector3.zero, Vector3.zero);
				vh.AddVert(new Vector3(vMin.x, vMax.y), color, new Vector2(uvMin.x, uvMax.y), new Vector2(uv1Min.x, uv1Max.y), Vector3.zero, Vector3.zero);
				vh.AddVert(new Vector3(vMax.x, vMax.y), color, new Vector2(uvMax.x, uvMax.y), new Vector2(uv1Max.x, uv1Max.y), Vector3.zero, Vector3.zero);
				vh.AddVert(new Vector3(vMax.x, vMin.y), color, new Vector2(uvMax.x, uvMin.y), new Vector2(uv1Max.x, uv1Min.y), Vector3.zero, Vector3.zero);

				int index = i * 4;

				vh.AddTriangle(index + 0, index + 1, index + 2);
				vh.AddTriangle(index + 2, index + 3, index + 0);
			}
		}
	}
}
