#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*using BBG.PictureColoring;
using BBG;*/

public class AnalyticEvents : Singleton<AnalyticEvents>
{
    public void Initialize()
    {
        StartCoroutine("InitializeCoroutine");
    }

    IEnumerator InitializeCoroutine() 
    {
        #if GAMEANALYTICS
                GameAnalytics.Initialize();
        #endif

        yield return new WaitUntil(()=> IsInitialized() == true);

        Debug.Log("Initialized analytics SDK");

        if(!PlayerPrefs.HasKey("initialLaunch"))
        {
            PlayerPrefs.SetInt("initialLaunch", 1);

            ReportEvent("install_app");
            ReportEvent("first_open");
            PlayerPrefs.SetString("app_version", Application.version);
            PlayerPrefs.Save();
        }
        else
        {
            ReportEvent("start_app");

            string appVersion = PlayerPrefs.GetString("app_version");

            if(!appVersion.Equals(Application.version))
            {
                PlayerPrefs.SetString("app_version", Application.version);
                ReportEvent("app_update");
                PlayerPrefs.Save();
            }
        }
    }

    public static bool IsInitialized() 
    {
#if FACEBOOK
        if (!FacebookManager.IsInitialized())
            return false;
#endif

        if(!FirebaseManager.IsInitialized)
            return false;

        return true;
    }

    public static void ReportEvent(string name) 
    {
        if(!IsInitialized()) { print("Analytics not ready!"); return; }

        //TenjinManager.ReportEvent(name);

        FirebaseManager.ReportEvent(name);

#if FACEBOOK
        FacebookManager.ReportEvent(name);
#endif

        AppMetrica.Instance?.ReportEvent(name);

#if GAMEANALYTICS
        GameAnalytics.NewDesignEvent(name);
#endif
        Debug.Log($"Report event: {name}");
    }

    public static void ReportEvent(string name, Dictionary<string, object> parameters)
    {
        if(!IsInitialized()) { print("Analytics not ready!"); return; }

        FirebaseManager.ReportEvent(name, parameters);

#if FACEBOOK
        FacebookManager.ReportEvent(name);
#endif

        AppMetrica.Instance?.ReportEvent(name, parameters);

#if GAMEANALYTICS
        GameAnalytics.NewDesignEvent(name);
#endif

        string str = "( ";

        foreach(var p in parameters)
            str += $" {p.Key} = {p.Value} ";

        str += " )";

        Debug.Log($"Report event: {name} {str}");
    }

    /*void OnApplicationFocus(bool focus)
    {
        if(ScreenManager.Instance.CurrentScreenId.Equals("library")) return;

        if(IsInitialized() && ScreenManager.Instance.CurrentScreenShowing)
        {
            if(focus)
            {
                var catName = new Dictionary<string, object>();
                catName.Add("category_name", ScreenManager.Instance.CurrentScreenId);

                if(ScreenManager.Instance.CurrentScreenId == "game")
                {
					LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

                    catName.Add("pictureCategoryName", GameManager.Instance.GetDisplayNameByLevelID(activeLevelData.Id));
                    catName.Add("pictureName", activeLevelData.AssetPath.Replace("Assets/Resources/Weave Custom/", ""));
                }

                ReportEvent("foregroup_app", catName);
            }
            else
                ReportEvent("background_app");
        }
    }*/
}
