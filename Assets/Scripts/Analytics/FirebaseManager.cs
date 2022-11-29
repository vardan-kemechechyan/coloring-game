using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.Messaging;
using Firebase.Crashlytics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.RemoteConfig;

/*#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif*/

public class FirebaseManager : Singleton<FirebaseManager>
{
	static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
	static CancellationToken token = cancelTokenSource.Token;

	public static bool IsInitialized { get; private set; }

	public static bool IsFetchedRemoteConfig { get; private set; }

	public static event OnInitializeFirebase OnInitialize;
	public delegate void OnInitializeFirebase();

	public static event OnFetchRemoteConfig OnFetch;
	public delegate void OnFetchRemoteConfig();

	static List<DelayedEvent> delayedEvents;

	public void Initialize()
	{
		StartCoroutine(DelayedEvents());

		/*
			// IOS 14 App Tracking Transparency
	#if UNITY_IOS
			if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
				ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
				ATTrackingStatusBinding.RequestAuthorizationTracking();
	#endif*/

		if (!IsInitialized)
		{
			FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
			{
				FirebaseMessaging.TokenReceived += FirebaseMessagingTokenReceived;
				FirebaseMessaging.MessageReceived += FirebaseMessagingMessageReceived;

				//FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
				var app = FirebaseApp.DefaultInstance;

				IsInitialized = true;
				
				SetupRemoteConfig();
			});
		}
	}

	IEnumerator DelayedEvents()
	{
		delayedEvents = new List<DelayedEvent>();

		yield return new WaitUntil(() => IsInitialized);

		foreach(var e in delayedEvents)
			ReportEvent(e.message, e.parameters);

		delayedEvents.Clear();
	}

	private async void GetTokenAsync()
	{
		var task = FirebaseMessaging.GetTokenAsync();

		await task;

		if(task.IsCompleted)
		{
			Debug.Log("Firebase messaging token: " + task.Result);
		}
	}

	private void AuthorizationTrackingReceived(int status)
	{
		Debug.LogFormat("Tracking status received: {0}", status);
	}

	public static void SetCustomKey(string key, string value)
	{
		if(!IsInitialized)
			return;

		Crashlytics.SetCustomKey(key, value);
	}

	#region Cloud messaging

	private void FirebaseMessagingTokenReceived(object sender, TokenReceivedEventArgs e)
	{
		Debug.Log($"Received token {e.Token}");
	}

	private void FirebaseMessagingMessageReceived(object sender, MessageReceivedEventArgs e)
	{
		Debug.Log($"Received message {e.Message} from {e.Message.From}");
	}

	#endregion /Cloud messaging

	#region Analytics

	public static void ReportEvent(string message, Dictionary<string, object> parameters = null)
	{
		if(!IsInitialized)
		{
			if(delayedEvents == null) return;

			delayedEvents.Add(new DelayedEvent
			{
				message = message,
				parameters = parameters
			});

			return;
		}

		if(parameters == null)
		{
			FirebaseAnalytics.LogEvent(message);
		}
		else
		{
			var values = new List<Parameter>();

			foreach(var v in parameters)
			{
				values.Add(new Parameter(v.Key, v.Value.ToString()));
			}

			FirebaseAnalytics.LogEvent(message, values.ToArray());
		}
	}

	public static void ReportRevenue(string adUnit, double value, string currency)
	{
		if(!IsInitialized)
			return;

		var values = new List<Parameter>();

		values.Add(new Parameter("source", "AdMob"));
		values.Add(new Parameter("ad_format", adUnit));
		values.Add(new Parameter("value", value));
		values.Add(new Parameter("currency", currency));

		FirebaseAnalytics.LogEvent("ad_revenue", values.ToArray());
	}

	#endregion /Analytics

	#region Remote config
	private void SetupRemoteConfig()
	{
		//Firebase Remote Config Defaults
		Dictionary<string, object> configDefaults = new Dictionary<string, object>();

		configDefaults.Add("Timeout_10sec", false);
		configDefaults.Add("Timeout_30sec", false);
		configDefaults.Add("Timeout_45sec", true);
		configDefaults.Add("Timeout_60sec", false);

		configDefaults.Add("no_internet", false);

		//Set Default Config Values
		FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(configDefaults).ContinueWith(task =>
		{
			//Fetch Remote Config Values
			FirebaseRemoteConfig.DefaultInstance.FetchAsync().ContinueWith(innerTask =>
			{
				//Activate Fetched Values
				FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

				IsFetchedRemoteConfig = true;

				Debug.Log("Fetched remote config");

				OnFetch.Invoke();
			});
		});
	}

	public static string GetRemoteConfigString(string key)
	{
		return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
	}

	public static int GetRemoteConfigInteger(string key)
	{
		return (int)FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
	}

	public static bool GetRemoteConfigBoolean(string key)
	{
		return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
	}

	#endregion /Remote config

	private class DelayedEvent
	{
		public string message;
		public Dictionary<string, object> parameters;
	}
}
