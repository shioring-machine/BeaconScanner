using UnityEngine;

public sealed class ScreenSleepController : MonoBehaviour
{
    [SerializeField]
    private BeaconPostureBridge beaconPostureBridge;

    [SerializeField]
    [Min(0.25f)]
    private float stateCheckIntervalSeconds =
        1.0f;

    public bool IsSleepPreventionEnabled
    {
        get;
        private set;
    }

    private float nextStateCheckTime;

    private bool applicationIsPaused;

    private void Awake()
    {
        ApplySystemSetting(
            false
        );
    }

    private void Start()
    {
        UpdateFromMonitoringState();
    }

    private void Update()
    {
        if (applicationIsPaused)
        {
            return;
        }

        if (
            Time.unscaledTime <
            nextStateCheckTime
        )
        {
            return;
        }

        nextStateCheckTime =
            Time.unscaledTime +
            stateCheckIntervalSeconds;

        UpdateFromMonitoringState();
    }

    public void UpdateFromMonitoringState()
    {
        bool monitoring =
            beaconPostureBridge != null &&
            beaconPostureBridge.IsMonitoring;

        if (monitoring)
        {
            ApplyNeverSleep();
        }
        else
        {
            ApplySystemSetting(
                false
            );
        }
    }

    public void PreventSleepFromButton()
    {
        ApplyNeverSleep();
    }

    public void RestoreSystemSettingFromButton()
    {
        ApplySystemSetting(
            true
        );
    }

    public void LogCurrentState()
    {
        bool monitoring =
            beaconPostureBridge != null &&
            beaconPostureBridge.IsMonitoring;

        Debug.Log(
            "[ScreenSleepControllerState] " +
            "Enabled=" +
            IsSleepPreventionEnabled +
            " | IsMonitoring=" +
            monitoring +
            " | ApplicationPaused=" +
            applicationIsPaused +
            " | SleepTimeout=" +
            Screen.sleepTimeout
        );
    }

    private void ApplyNeverSleep()
    {
        if (IsSleepPreventionEnabled)
        {
            return;
        }

        Screen.sleepTimeout =
            SleepTimeout.NeverSleep;

        IsSleepPreventionEnabled =
            true;

        Debug.Log(
            "[ScreenSleepController] " +
            "Screen sleep prevention enabled."
        );
    }

    private void ApplySystemSetting(
        bool forceLog
    )
    {
        bool stateChanged =
            IsSleepPreventionEnabled;

        if (!stateChanged && !forceLog)
        {
            return;
        }

        Screen.sleepTimeout =
            SleepTimeout.SystemSetting;

        IsSleepPreventionEnabled =
            false;

        Debug.Log(
            "[ScreenSleepController] " +
            "Screen sleep setting restored."
        );
    }

    private void OnApplicationPause(
        bool pauseStatus
    )
    {
        applicationIsPaused =
            pauseStatus;

        if (pauseStatus)
        {
            ApplySystemSetting(
                false
            );

            return;
        }

        nextStateCheckTime =
            0.0f;

        UpdateFromMonitoringState();
    }

    private void OnApplicationFocus(
        bool hasFocus
    )
    {
        if (!hasFocus)
        {
            return;
        }

        applicationIsPaused =
            false;

        nextStateCheckTime =
            0.0f;

        UpdateFromMonitoringState();
    }

    private void OnDisable()
    {
        ApplySystemSetting(
            false
        );
    }

    private void OnDestroy()
    {
        ApplySystemSetting(
            false
        );
    }

    private void OnApplicationQuit()
    {
        ApplySystemSetting(
            false
        );
    }
}
