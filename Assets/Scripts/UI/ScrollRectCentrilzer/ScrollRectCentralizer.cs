using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollRectCentralizer : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public RectTransform viewport;

    public bool Center_X;
    public bool Center_Y;

    float spacing = -99999f;

    public void SnapTo(RectTransform target)
    {
        CancelInvoke("ReturnElastic");

        int siblingIndex = target.transform.GetSiblingIndex();

        int activeChildCount = 0;

		for(int i = 0; i < contentPanel.transform.childCount; i++)
		{
            if(contentPanel.transform.GetChild(i).gameObject.activeSelf) 
                activeChildCount++;
        }

        if (siblingIndex == 0 || siblingIndex == activeChildCount - 1) return;

        if (spacing == -99999) spacing = contentPanel.GetComponent<HorizontalOrVerticalLayoutGroup>().spacing;

        int visibleChildernCount = Mathf.RoundToInt(viewport.rect.width / (target.rect.width + spacing / 2));

        Vector2 viewportLocalPosition = viewport.localPosition;
        Vector2 childLocalPosition = target.localPosition;

        Vector2 result = new Vector2();

        Canvas.ForceUpdateCanvases();

       if ( (siblingIndex + visibleChildernCount / 2) > activeChildCount - 1 ||
            (siblingIndex - visibleChildernCount / 2) < 0)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        result = new Vector2(
            Center_X ? 0 - (viewportLocalPosition.x + childLocalPosition.x) : contentPanel.localPosition.x,
            Center_Y ? 0 - (viewportLocalPosition.y + childLocalPosition.y) : contentPanel.localPosition.y
        );

        contentPanel.localPosition = result;

        Invoke("ReturnElastic", 0.25f);
    }

    void ReturnElastic()
    {
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
    }
}
