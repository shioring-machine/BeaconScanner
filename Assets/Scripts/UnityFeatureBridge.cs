using System;
using UnityEngine;

public sealed class UnityFeatureBridge : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PluginClassName =
        "com.example.aninterface.UnityFeaturePlugin";

    private AndroidJavaObject pluginInstance;
#endif

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        InitializePlugin();
    }

    public bool InitializePlugin()
    {
        if (IsInitialized)
        {
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            ReleasePlugin();

            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                );

            using AndroidJavaObject currentActivity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );

            if (currentActivity == null)
            {
                Debug.LogError(
                    "Android Activityを取得できませんでした。"
                );

                return false;
            }

            pluginInstance =
                new AndroidJavaObject(
                    PluginClassName
                );

            IsInitialized =
                pluginInstance.Call<bool>(
                    "initialize",
                    currentActivity
                );

            Debug.Log(
                "[UnityFeatureBridge] " +
                "Initialized=" +
                IsInitialized +
                " | Status=" +
                GetLatestStatus()
            );

            if (!IsInitialized)
            {
                pluginInstance.Dispose();
                pluginInstance = null;
            }

            return IsInitialized;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Androidプラグインの初期化に失敗しました。\n" +
                exception
            );

            if (pluginInstance != null)
            {
                pluginInstance.Dispose();
                pluginInstance = null;
            }

            IsInitialized =
                false;

            return false;
        }
#else
        Debug.Log(
            "AndroidプラグインはAndroid実機上でのみ動作します。"
        );

        return false;
#endif
    }

    public bool SaveCondition(
        int score
    )
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        if (score < 1 || score > 5)
        {
            Debug.LogError(
                "体調スコアは1以上5以下で指定してください。"
            );

            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                "saveCondition",
                score
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "体調記録の保存に失敗しました。\n" +
                exception
            );

            return false;
        }
#else
        return false;
#endif
    }

    public int GetTodayScore()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return 0;
        }

        try
        {
            return pluginInstance.Call<int>(
                "getTodayScore"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "本日の体調記録を取得できませんでした。\n" +
                exception
            );

            return 0;
        }
#else
        return 0;
#endif
    }

    public string GetHistoryJson()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return CreateEmptyHistoryJson(
                "プラグインが初期化されていません。"
            );
        }

        try
        {
            string json =
                pluginInstance.Call<string>(
                    "getHistoryJson"
                );

            return string.IsNullOrWhiteSpace(json)
                ? CreateEmptyHistoryJson(
                    "履歴データが空です。"
                )
                : json;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "体調履歴を取得できませんでした。\n" +
                exception
            );

            return CreateEmptyHistoryJson(
                "体調履歴を取得できませんでした。"
            );
        }
#else
        return CreateEmptyHistoryJson(
            "Unity Editorでは利用できません。"
        );
#endif
    }

    public bool ClearHistory()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                "clearHistory"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "体調履歴を削除できませんでした。\n" +
                exception
            );

            return false;
        }
#else
        return false;
#endif
    }

    public bool AddDurationSeconds(
        int seconds
    )
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        if (seconds <= 0)
        {
            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                "addDurationSeconds",
                seconds
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "継続時間の保存に失敗しました。\n" +
                exception
            );

            return false;
        }
#else
        return false;
#endif
    }

    public long GetTodayDurationSeconds()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return 0L;
        }

        try
        {
            return pluginInstance.Call<long>(
                "getTodayDurationSeconds"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "本日の累積時間を取得できませんでした。\n" +
                exception
            );

            return 0L;
        }
#else
        return 0L;
#endif
    }

    public long GetTotalDurationSeconds()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return 0L;
        }

        try
        {
            return pluginInstance.Call<long>(
                "getTotalDurationSeconds"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "総累積時間を取得できませんでした。\n" +
                exception
            );

            return 0L;
        }
#else
        return 0L;
#endif
    }

    public string GetDurationHistoryJson()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return CreateEmptyDurationHistoryJson(
                "プラグインが初期化されていません。"
            );
        }

        try
        {
            string json =
                pluginInstance.Call<string>(
                    "getDurationHistoryJson"
                );

            return string.IsNullOrWhiteSpace(json)
                ? CreateEmptyDurationHistoryJson(
                    "継続時間履歴が空です。"
                )
                : json;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "継続時間履歴を取得できませんでした。\n" +
                exception
            );

            return CreateEmptyDurationHistoryJson(
                "継続時間履歴を取得できませんでした。"
            );
        }
#else
        return CreateEmptyDurationHistoryJson(
            "Unity Editorでは利用できません。"
        );
#endif
    }

    public bool ClearDurationHistory()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                "clearDurationHistory"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "継続時間履歴を削除できませんでした。\n" +
                exception
            );

            return false;
        }
#else
        return false;
#endif
    }

    public bool GetAndroidInitializedState()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pluginInstance == null)
        {
            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                "isInitialized"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Android側の初期化状態を取得できませんでした。\n" +
                exception
            );

            return false;
        }
#else
        return false;
#endif
    }

    public string GetLatestStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsInitialized || pluginInstance == null)
        {
            return "プラグインが初期化されていません。";
        }

        try
        {
            string status =
                pluginInstance.Call<string>(
                    "getLatestStatus"
                );

            return string.IsNullOrWhiteSpace(status)
                ? "状態メッセージがありません。"
                : status;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Android側の状態を取得できませんでした。\n" +
                exception
            );

            return "状態取得に失敗しました。";
        }
#else
        return "Unity EditorではAndroidプラグインを利用できません。";
#endif
    }

    public void SaveExcellent()
    {
        SaveConditionAndLog(
            5,
            "絶好調"
        );
    }

    public void SaveGood()
    {
        SaveConditionAndLog(
            4,
            "元気"
        );
    }

    public void SaveNormal()
    {
        SaveConditionAndLog(
            3,
            "普通"
        );
    }

    public void SaveSlightlyBlue()
    {
        SaveConditionAndLog(
            2,
            "少しブルー"
        );
    }

    public void SaveNotGood()
    {
        SaveConditionAndLog(
            1,
            "元気ない"
        );
    }

    public void LogHistory()
    {
        Debug.Log(
            "[HealthHistory] " +
            GetHistoryJson()
        );
    }

    public void LogDurationHistory()
    {
        Debug.Log(
            "[DurationHistory] " +
            GetDurationHistoryJson()
        );
    }

    public void LogCurrentState()
    {
        Debug.Log(
            "[UnityFeatureState] " +
            "UnityInitialized=" +
            IsInitialized +
            " | AndroidInitialized=" +
            GetAndroidInitializedState() +
            " | TodayScore=" +
            GetTodayScore() +
            " | TodayDurationSeconds=" +
            GetTodayDurationSeconds() +
            " | TotalDurationSeconds=" +
            GetTotalDurationSeconds() +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public void ClearHistoryFromButton()
    {
        bool succeeded =
            ClearHistory();

        Debug.Log(
            "[HealthHistoryClear] " +
            "Result=" +
            succeeded +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public void ClearDurationHistoryFromButton()
    {
        bool succeeded =
            ClearDurationHistory();

        Debug.Log(
            "[DurationHistoryClear] " +
            "Result=" +
            succeeded +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public void InitializePluginFromButton()
    {
        bool succeeded =
            InitializePlugin();

        Debug.Log(
            "[UnityFeatureInitialize] " +
            "Result=" +
            succeeded +
            " | Status=" +
            GetLatestStatus()
        );
    }

    private void SaveConditionAndLog(
        int score,
        string label
    )
    {
        bool succeeded =
            SaveCondition(
                score
            );

        Debug.Log(
            "[HealthSave] " +
            "Result=" +
            succeeded +
            " | Score=" +
            score +
            " | Label=" +
            label +
            " | TodayScore=" +
            GetTodayScore() +
            " | Status=" +
            GetLatestStatus()
        );
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool EnsurePluginIsReady()
    {
        if (!IsInitialized)
        {
            Debug.LogError(
                "Androidプラグインが初期化されていません。"
            );

            return false;
        }

        if (pluginInstance == null)
        {
            Debug.LogError(
                "Androidプラグインのインスタンスがありません。"
            );

            IsInitialized =
                false;

            return false;
        }

        return true;
    }
#endif

    private static string CreateEmptyHistoryJson(
        string message
    )
    {
        return
            "{\"success\":false," +
            "\"count\":0," +
            "\"message\":\"" +
            EscapeJsonString(message) +
            "\"," +
            "\"history\":[]}";
    }

    private static string CreateEmptyDurationHistoryJson(
        string message
    )
    {
        return
            "{\"success\":false," +
            "\"count\":0," +
            "\"todaySeconds\":0," +
            "\"totalSeconds\":0," +
            "\"message\":\"" +
            EscapeJsonString(message) +
            "\"," +
            "\"history\":[]}";
    }

    private static string EscapeJsonString(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace(
                "\\",
                "\\\\"
            )
            .Replace(
                "\"",
                "\\\""
            )
            .Replace(
                "\r",
                "\\r"
            )
            .Replace(
                "\n",
                "\\n"
            );
    }

    private void OnApplicationPause(
        bool pauseStatus
    )
    {
        if (pauseStatus)
        {
            Debug.Log(
                "[UnityFeatureBridge] Application paused."
            );
        }
    }

    private void OnDestroy()
    {
        ReleasePlugin();
    }

    private void OnApplicationQuit()
    {
        ReleasePlugin();
    }

    private void ReleasePlugin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pluginInstance != null)
        {
            try
            {
                pluginInstance.Call(
                    "release"
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Androidプラグインの解放に失敗しました。\n" +
                    exception
                );
            }

            pluginInstance.Dispose();
            pluginInstance = null;
        }
#endif

        IsInitialized =
            false;
    }
}