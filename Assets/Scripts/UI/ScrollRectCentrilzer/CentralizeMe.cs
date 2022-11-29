using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralizeMe : MonoBehaviour
{
	public RectTransform RectT { get { return transform as RectTransform; } }

	public void CentralizeInRectTransform()
	{
		transform.parent.parent.parent.GetComponent<ScrollRectCentralizer>().SnapTo(RectT);
	}
}

