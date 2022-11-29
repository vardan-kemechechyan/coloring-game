using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintAnimationManager : MonoBehaviour
{
	#region Inspector Variables

	public Transform hintObject;
	public Transform hintAwardObject;
	public static bool cancelAnimation = false;
	public int hintAnimationDelay;

	#endregion

	#region Public Properties

	public RectTransform hintRectTransform { get { return hintObject as RectTransform; } }
	public RectTransform hintAwardRectTransform { get { return hintAwardObject as RectTransform; } }
	public DateTime LastTimeHinted { get; private set; }

	#endregion

	public void InitializeHint()
	{
		hintRectTransform.localScale = Vector3.one * 1.3f;
		hintAwardRectTransform.localScale = Vector3.one * 1.3f;

		cancelAnimation = false;

		UpdateLastTimeTimer();
		StopAllCoroutines();

		StartCoroutine(StartHintTimer());
	}

	public void StartAnimation()
	{
		UpdateLastTimeTimer();
		cancelAnimation = false;

		StartCoroutine(ScaleAnimation());
	}

	public void UpdateLastTimeTimer() 
	{
		LastTimeHinted = DateTime.Now;
	}

	IEnumerator ScaleAnimation()
	{
		var t = new WaitForSeconds(0.02f);
		float direction = 1f;

		RectTransform whoToAnimate = hintAwardObject.gameObject.activeSelf ? hintAwardRectTransform : hintRectTransform;

		while( !cancelAnimation )
		{
			if( (whoToAnimate.localScale.x > 1.4f && direction == 1 ) || (whoToAnimate.localScale.x < 1.2f && direction == -1))
				direction *= -1;

			whoToAnimate.localScale += Vector3.one * .005f * direction;

			yield return t;
		}

		hintRectTransform.localScale = Vector3.one * 1.3f;
		hintAwardRectTransform.localScale = Vector3.one * 1.3f;

		UpdateLastTimeTimer();

		StartCoroutine(StartHintTimer());
	}

	IEnumerator StartHintTimer()
	{
		var t = new WaitForSeconds(0.02f); 

		while((DateTime.Now - LastTimeHinted).TotalSeconds < hintAnimationDelay)
			yield return t;

		StartAnimation();
	}

	public void SendToDefault()
	{
		StopAllCoroutines();

		hintRectTransform.localScale = Vector3.one * 1.3f;
		hintAwardRectTransform.localScale = Vector3.one * 1.3f;

		cancelAnimation = false;
	}
}
