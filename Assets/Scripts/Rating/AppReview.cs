using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class AppReview : MonoBehaviour
{
    [SerializeField] bool feedbackIfLowRate;
    //[SerializeField] UIManager ui;

    [SerializeField] private string androidStoreUrl;
    [SerializeField] private string iosStoreUrl;
    [SerializeField] private string reviewEmail;

    //UI.UIScreen[] previousScreens;

    public string GetStoreUrl() 
    {
#if UNITY_ANDROID
        return androidStoreUrl;
#endif

#if UNITY_IOS
        return iosStoreUrl;
#endif

        return "";
    }

    int GetRepeatRate() 
    {
        int rate = PlayerPrefs.GetInt("appReviewRate");
        return rate > 0 ? rate : 2; 
    }

    int repeatCounter;
    public int RepeatCounter 
    {
        get => repeatCounter;
        set 
        {
            if (value > 0 && value >= GetRepeatRate()) 
            {
                value = 0;

                if (PlayerPrefs.GetInt("rateApp") <= 0) 
                    RemindToRate();
            }

            repeatCounter = value;
        }
    }

    private void RemindToRate()
    {
        PlayerPrefs.SetInt("appReviewRate", 3);

        //previousScreens = ui.ActiveScreens.ToArray();
        //ui.ShowScreen<ReviewScreen>();
    }

    public void Rate(int rate) 
    {
        //if (previousScreens != null && previousScreens.Length > 0)
            //ui.ShowScreen(previousScreens);

        if (rate <= 0)
        {
            //RepeatCounter = 0;
            AnalyticEvents.ReportEvent("rate_popup_closed");
            return;
        }    

        //AnalyticEvents.ReportEvent("rate_popup_success");
        //PlayerPrefs.SetInt("rateApp", 1);

        if (rate < 3 && feedbackIfLowRate)
        {
            //AnalyticEvents.ReportEvent("rate_popup_success");
            Extensions.MailTo(reviewEmail, Application.productName, "");
        }
        else 
        {
            Application.OpenURL(GetStoreUrl());
            AnalyticEvents.ReportEvent("rate_popup_success");
        }
    }
}
