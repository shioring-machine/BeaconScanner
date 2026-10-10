using System;
using UnityEngine;

public sealed class BeaconPostureBridge : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PluginClassName =
        "com.example.sensorbeaconlibrary.BeaconPosturePlugin";

    private AndroidJavaObject pluginInstance;
#endif

    public bool IsInitialized
    {
        get;
        private set;
    }

    public bool IsMonitoring
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return IsInitialized &&
                   pluginInstance != null &&
                   CallBooleanMethod(
                       "isMonitoring"
                   );
#else
            return false;
#endif
        }
    }

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
            DisposePluginInstance();

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
                    "UnityのAndroid Activityを取得できませんでした。"
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

            string status =
                CallStringMethodDirectly(
                    "getLatestStatus",
                    "プラグイン状態を取得できませんでした。"
                );

            Debug.Log(
                "[BeaconPluginInitialize] " +
                "Result=" +
                IsInitialized +
                " | Status=" +
                status
            );

            if (!IsInitialized)
            {
                DisposePluginInstance();
            }

            return IsInitialized;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "BeaconPosturePluginの初期化に失敗しました。\n" +
                exception
            );

            DisposePluginInstance();

            IsInitialized =
                false;

            return false;
        }
#else
        Debug.Log(
            "BeaconPosturePluginはAndroid実機上でのみ利用できます。"
        );

        return false;
#endif
    }

    public void InitializePluginFromButton()
    {
        bool initialized =
            InitializePlugin();

        Debug.Log(
            "[BeaconPluginInitializeButton] " +
            "Result=" +
            initialized +
            " | Initialized=" +
            IsInitialized +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public bool HasRequiredPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "hasRequiredPermissions"
               );
#else
        return false;
#endif
    }

    public bool IsBluetoothEnabled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "isBluetoothEnabled"
               );
#else
        return false;
#endif
    }

    public bool RequestEnableBluetooth()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        try
        {
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
                    "UnityのAndroid Activityを取得できませんでした。"
                );

                return false;
            }

            bool requested =
                pluginInstance.Call<bool>(
                    "requestEnableBluetooth",
                    currentActivity
                );

            Debug.Log(
                "[BeaconBluetoothRequest] " +
                "Result=" +
                requested +
                " | BluetoothEnabled=" +
                IsBluetoothEnabled() +
                " | Status=" +
                GetLatestStatus()
            );

            return requested;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Bluetooth有効化画面の表示に失敗しました。\n" +
                exception
            );

            return false;
        }
#else
        Debug.LogWarning(
            "Bluetooth有効化要求はAndroid実機上でのみ実行できます。"
        );

        return false;
#endif
    }

    public void RequestEnableBluetoothFromButton()
    {
        bool requested =
            RequestEnableBluetooth();

        Debug.Log(
            "[BeaconBluetoothRequestButton] " +
            "Result=" +
            requested +
            " | BluetoothEnabled=" +
            IsBluetoothEnabled() +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public bool StartMonitoring()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return false;
        }

        bool started =
            CallBooleanMethod(
                "startMonitoring"
            );

        Debug.Log(
            "[BeaconMonitoringStartCore] " +
            "Result=" +
            started +
            " | Monitoring=" +
            IsMonitoring +
            " | Status=" +
            GetLatestStatus()
        );

        return started;
#else
        Debug.LogWarning(
            "ビーコン・姿勢監視はAndroid実機上でのみ開始できます。"
        );

        return false;
#endif
    }

    public void StartMonitoringFromButton()
    {
        if (!IsInitialized)
        {
            bool initialized =
                InitializePlugin();

            if (!initialized)
            {
                Debug.LogError(
                    "[BeaconMonitoringStart] " +
                    "プラグインを初期化できなかったため、" +
                    "監視を開始できません。"
                );

                return;
            }
        }

        bool permissionsGranted =
            HasRequiredPermissions();

        bool bluetoothEnabled =
            IsBluetoothEnabled();

        if (!permissionsGranted)
        {
            Debug.LogWarning(
                "[BeaconMonitoringStart] " +
                "必要なAndroid権限が許可されていません。"
            );
        }

        if (!bluetoothEnabled)
        {
            Debug.LogWarning(
                "[BeaconMonitoringStart] " +
                "Bluetoothが有効ではありません。"
            );
        }

        bool started =
            StartMonitoring();

        Debug.Log(
            "[BeaconMonitoringStart] " +
            "Result=" +
            started +
            " | Initialized=" +
            IsInitialized +
            " | Monitoring=" +
            IsMonitoring +
            " | Permissions=" +
            permissionsGranted +
            " | BluetoothEnabled=" +
            bluetoothEnabled +
            " | LastScanErrorCode=" +
            GetLastScanErrorCode() +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public void StopMonitoring()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsInitialized || pluginInstance == null)
        {
            return;
        }

        try
        {
            pluginInstance.Call(
                "stopMonitoring"
            );

            Debug.Log(
                "[BeaconMonitoringStopCore] " +
                "Monitoring=" +
                IsMonitoring +
                " | Status=" +
                GetLatestStatus()
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ビーコン・姿勢監視の停止に失敗しました。\n" +
                exception
            );
        }
#else
        Debug.Log(
            "Unity Editor上では監視停止処理を実行しません。"
        );
#endif
    }

    public void StopMonitoringFromButton()
    {
        StopMonitoring();

        Debug.Log(
            "[BeaconMonitoringStop] " +
            "Initialized=" +
            IsInitialized +
            " | Monitoring=" +
            IsMonitoring +
            " | Status=" +
            GetLatestStatus()
        );
    }

    public bool IsTargetBeaconReceived()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "isTargetBeaconReceived"
               );
#else
        return false;
#endif
    }

    public bool IsNearTargetBeacon()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "isNearTargetBeacon"
               );
#else
        return false;
#endif
    }

    public bool IsFaceDown()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "isFaceDown"
               );
#else
        return false;
#endif
    }

    public bool IsConditionMet()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return IsInitialized &&
               pluginInstance != null &&
               CallBooleanMethod(
                   "isConditionMet"
               );
#else
        return false;
#endif
    }

    public int GetMedianRssi()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return int.MinValue;
        }

        try
        {
            return pluginInstance.Call<int>(
                "getMedianRssi"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "RSSI中央値を取得できませんでした。\n" +
                exception
            );

            return int.MinValue;
        }
#else
        return int.MinValue;
#endif
    }

    public float GetCurrentGravityZ()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return float.NaN;
        }

        try
        {
            return pluginInstance.Call<float>(
                "getCurrentGravityZ"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "重力センサーのZ値を取得できませんでした。\n" +
                exception
            );

            return float.NaN;
        }
#else
        return float.NaN;
#endif
    }

    public int GetLastScanErrorCode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsurePluginIsReady())
        {
            return 0;
        }

        try
        {
            return pluginInstance.Call<int>(
                "getLastScanErrorCode"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "BLEスキャンエラーコードを取得できませんでした。\n" +
                exception
            );

            return 0;
        }
#else
        return 0;
#endif
    }

    public string GetLatestStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsInitialized || pluginInstance == null)
        {
            return "プラグインが初期化されていません。";
        }

        return CallStringMethodDirectly(
            "getLatestStatus",
            "プラグイン状態を取得できませんでした。"
        );
#else
        return "Unity EditorではAndroidプラグインを利用できません。";
#endif
    }

    public void LogCurrentState()
    {
        int medianRssi =
            GetMedianRssi();

        float gravityZ =
            GetCurrentGravityZ();

        string rssiText =
            medianRssi == int.MinValue
                ? "未取得"
                : medianRssi + " dBm";

        string gravityText =
            float.IsNaN(gravityZ)
                ? "未取得"
                : gravityZ.ToString("F2") + " m/s²";

        Debug.Log(
            "BeaconPosturePlugin state\n" +
            "Initialized=" +
            IsInitialized +
            "\nMonitoring=" +
            IsMonitoring +
            "\nPermissions=" +
            HasRequiredPermissions() +
            "\nBluetoothEnabled=" +
            IsBluetoothEnabled() +
            "\nBeaconReceived=" +
            IsTargetBeaconReceived() +
            "\nBeaconNear=" +
            IsNearTargetBeacon() +
            "\nFaceDown=" +
            IsFaceDown() +
            "\nConditionMet=" +
            IsConditionMet() +
            "\nMedianRSSI=" +
            rssiText +
            "\nGravityZ=" +
            gravityText +
            "\nLastScanErrorCode=" +
            GetLastScanErrorCode() +
            "\nStatus=" +
            GetLatestStatus()
        );
    }

    public void LogCurrentStateSingleLine()
    {
        int medianRssi =
            GetMedianRssi();

        float gravityZ =
            GetCurrentGravityZ();

        string rssiText =
            medianRssi == int.MinValue
                ? "Unavailable"
                : medianRssi.ToString();

        string gravityText =
            float.IsNaN(gravityZ)
                ? "Unavailable"
                : gravityZ.ToString("F2");

        Debug.Log(
            "[BeaconPostureState] " +
            "Initialized=" +
            IsInitialized +
            " | Monitoring=" +
            IsMonitoring +
            " | Permissions=" +
            HasRequiredPermissions() +
            " | BluetoothEnabled=" +
            IsBluetoothEnabled() +
            " | BeaconReceived=" +
            IsTargetBeaconReceived() +
            " | BeaconNear=" +
            IsNearTargetBeacon() +
            " | FaceDown=" +
            IsFaceDown() +
            " | ConditionMet=" +
            IsConditionMet() +
            " | MedianRSSI=" +
            rssiText +
            " | GravityZ=" +
            gravityText +
            " | LastScanErrorCode=" +
            GetLastScanErrorCode() +
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
                "BeaconPosturePluginが初期化されていません。"
            );

            return false;
        }

        if (pluginInstance == null)
        {
            Debug.LogError(
                "BeaconPosturePluginのインスタンスがありません。"
            );

            IsInitialized =
                false;

            return false;
        }

        return true;
    }

    private bool CallBooleanMethod(
        string methodName
    )
    {
        if (pluginInstance == null)
        {
            return false;
        }

        try
        {
            return pluginInstance.Call<bool>(
                methodName
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Android真偽値メソッドの呼び出しに失敗しました。\n" +
                "Method=" +
                methodName +
                "\n" +
                exception
            );

            return false;
        }
    }

    private string CallStringMethodDirectly(
        string methodName,
        string fallbackValue
    )
    {
        if (pluginInstance == null)
        {
            return fallbackValue;
        }

        try
        {
            string result =
                pluginInstance.Call<string>(
                    methodName
                );

            return string.IsNullOrEmpty(result)
                ? fallbackValue
                : result;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Android文字列メソッドの呼び出しに失敗しました。\n" +
                "Method=" +
                methodName +
                "\n" +
                exception
            );

            return fallbackValue;
        }
    }

    private void DisposePluginInstance()
    {
        if (pluginInstance == null)
        {
            return;
        }

        pluginInstance.Dispose();
        pluginInstance =
            null;
    }
#endif

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
                if (IsMonitoring)
                {
                    pluginInstance.Call(
                        "stopMonitoring"
                    );
                }

                pluginInstance.Call(
                    "release"
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "BeaconPosturePluginの解放処理に失敗しました。\n" +
                    exception
                );
            }

            DisposePluginInstance();
        }
#endif

        IsInitialized =
            false;
    }
}