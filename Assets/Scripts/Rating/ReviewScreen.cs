using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//using UI;

public class ReviewScreen : MonoBehaviour
{
    [SerializeField] AppReview appReview;
    [SerializeField] GameObject[] ratingStars;
    [SerializeField] GameObject ratingWindow;

    int rating;

    int levelsCompleted;
    int levelsStarted;
    bool firstColoredPicture;

    bool canShow = true;


    private void Start()
	{
        Open();
    }

	public void Open()
    {
        rating = 5;

        UpdateRatingStars();
    }

    public void Rate(int rate)
    {
        rating = rate;
        UpdateRatingStars();
    }

    public void Rate() 
    {
        appReview.Rate(rating);
    }

    public void CheckIfToShow()
    {
        if(!canShow) return;

        levelsCompleted = PlayerPrefs.GetInt("levelsCompleted");
        levelsStarted = PlayerPrefs.GetInt("levelsStarted");
        firstColoredPicture = levelsCompleted == 1;

        if(firstColoredPicture)
            Show();
        else if(levelsCompleted != 0 && levelsCompleted % 3 == 0)
            Show();
        else if(levelsStarted != 0 && levelsStarted % 3 == 0)
            Show();           
	}

    public void Show()
    {
        canShow = false;

        ratingWindow.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Later()
    {
        appReview.Rate(0);

        canShow = true;
    }

    private void UpdateRatingStars()
    {
        foreach (var star in ratingStars)
            star.transform.GetChild(1).gameObject.SetActive(false);

        for (int i = 0; i < rating; i++)
            ratingStars[i].transform.GetChild(1).gameObject.SetActive(true);
    }
}
