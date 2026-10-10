using System;
using TMPro;
using UnityEngine;

public sealed class BeaconFaceDownDurationTracker : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField]
    private BeaconPostureBridge beaconPostureBridge;

    [SerializeField]
    private UnityFeatureBridge unityFeatureBridge;

    [Header("Display")]
    [SerializeField]
    private TMP_Text conditionStatusText;

    [SerializeField]
    private TMP_Text currentSessionText;

    [SerializeField]
    private TMP_Text todayDurationText;

    [SerializeField]
    private TMP_Text totalDurationText;

    [Header("Timing")]
    [SerializeField]
    [Min(0.1f)]
    private float pollingIntervalSeconds =
        0.25f;

    [SerializeField]
    [Min(1.0f)]
    private float automaticSaveIntervalSeconds =
        5.0f;

    [SerializeField]
    [Min(0.1f)]
    private float maximumAcceptedDeltaSeconds =
        1.0f;

    [Header("Debug")]
    [SerializeField]
    private bool verboseLogging =
        false;

    private double nextPollingTime;

    private double lastPollingTime;

    private double currentSessionSeconds;

    private double unsavedDurationSeconds;

    private long storedTodaySeconds;

    private long storedTotalSeconds;

    private bool conditionWasMet;

    private bool initialized;

    public bool IsAccumulating { get; private set; }

    public double CurrentSessionSeconds =>
        currentSessionSeconds;

    public double UnsavedDurationSeconds =>
        unsavedDurationSeconds;

    private void Start()
    {
        InitializeTracker();
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        double now =
            Time.realtimeSinceStartupAsDouble;

        if (now < nextPollingTime)
        {
            return;
        }

        double elapsed =
            now -
            lastPollingTime;

        lastPollingTime =
            now;

        nextPollingTime =
            now +
            pollingIntervalSeconds;

        elapsed =
            Math.Max(
                0.0,
                Math.Min(
                    elapsed,
                    maximumAcceptedDeltaSeconds
                )
            );

        EvaluateCondition(
            elapsed
        );

        UpdateDisplay();
    }

    public void InitializeTracker()
    {
        if (!ValidateReferences())
        {
            initialized =
                false;

            return;
        }

        double now =
            Time.realtimeSinceStartupAsDouble;

        nextPollingTime =
            now;

        lastPollingTime =
            now;

        currentSessionSeconds =
            0.0;

        unsavedDurationSeconds =
            0.0;

        conditionWasMet =
            false;

        IsAccumulating =
            false;

        RefreshStoredDurations();

        initialized =
            true;

        UpdateDisplay();

        Debug.Log(
            "[DurationTracker] Initialized."
        );
    }

    public void RefreshStoredDurations()
    {
        if (unityFeatureBridge == null)
        {
            storedTodaySeconds =
                0L;

            storedTotalSeconds =
                0L;

            return;
        }

        storedTodaySeconds =
            unityFeatureBridge
                .GetTodayDurationSeconds();

        storedTotalSeconds =
            unityFeatureBridge
                .GetTotalDurationSeconds();
    }

    public void FlushPendingDurationFromButton()
    {
        FlushPendingDuration(
            true
        );

        UpdateDisplay();
    }

    public void LogDurationStateFromButton()
    {
        bool monitoring =
            GetMonitoringState();

        bool beaconNear =
            GetBeaconNearState();

        bool faceDown =
            GetFaceDownState();

        bool conditionMet =
            GetConditionMetState();

        Debug.Log(
            "[DurationTrackerState] " +
            "Monitoring=" +
            monitoring +
            " | BeaconNear=" +
            beaconNear +
            " | FaceDown=" +
            faceDown +
            " | ConditionMet=" +
            conditionMet +
            " | IsAccumulating=" +
            IsAccumulating +
            " | CurrentSessionSeconds=" +
            currentSessionSeconds.ToString("F2") +
            " | UnsavedSeconds=" +
            unsavedDurationSeconds.ToString("F2") +
            " | StoredTodaySeconds=" +
            storedTodaySeconds +
            " | DisplayTodaySeconds=" +
            GetDisplayedTodaySeconds() +
            " | StoredTotalSeconds=" +
            storedTotalSeconds
        );
    }

    public void ClearDurationHistoryFromButton()
    {
        FlushPendingDuration(
            true
        );

        bool succeeded =
            unityFeatureBridge != null &&
            unityFeatureBridge
                .ClearDurationHistory();

        if (succeeded)
        {
            storedTodaySeconds =
                0L;

            storedTotalSeconds =
                0L;

            currentSessionSeconds =
                0.0;

            unsavedDurationSeconds =
                0.0;

            conditionWasMet =
                false;

            IsAccumulating =
                false;
        }

        UpdateDisplay();

        Debug.Log(
            "[DurationTrackerClear] " +
            "Result=" +
            succeeded
        );
    }

    private void EvaluateCondition(
        double elapsed
    )
    {
        bool monitoring =
            GetMonitoringState();

        bool conditionMet =
            monitoring &&
            GetConditionMetState();

        if (conditionMet)
        {
            IsAccumulating =
                true;

            currentSessionSeconds +=
                elapsed;

            unsavedDurationSeconds +=
                elapsed;

            if (
                unsavedDurationSeconds >=
                automaticSaveIntervalSeconds
            ) {
                FlushPendingDuration(
                    false
                );
            }
        } else {
            IsAccumulating =
                false;

            if (conditionWasMet)
            {
                FlushPendingDuration(
                    true
                );

                currentSessionSeconds =
                    0.0;
            }
        }

        if (
            verboseLogging &&
            conditionMet != conditionWasMet
        ) {
            Debug.Log(
                "[DurationTrackerTransition] " +
                "ConditionMet=" +
                conditionMet +
                " | Monitoring=" +
                monitoring +
                " | BeaconNear=" +
                GetBeaconNearState() +
                " | FaceDown=" +
                GetFaceDownState()
            );
        }

        conditionWasMet =
            conditionMet;
    }

    private void FlushPendingDuration(
        bool force
    )
    {
        if (unityFeatureBridge == null)
        {
            return;
        }

        int wholeSeconds =
            (int)Math.Floor(
                unsavedDurationSeconds
            );

        if (wholeSeconds <= 0)
        {
            if (force)
            {
                RefreshStoredDurations();
            }

            return;
        }

        bool saved =
            unityFeatureBridge
                .AddDurationSeconds(
                    wholeSeconds
                );

        if (!saved)
        {
            Debug.LogWarning(
                "[DurationTracker] " +
                "Pending duration could not be saved. " +
                "Seconds=" +
                wholeSeconds
            );

            return;
        }

        unsavedDurationSeconds -=
            wholeSeconds;

        storedTodaySeconds +=
            wholeSeconds;

        storedTotalSeconds +=
            wholeSeconds;

        if (verboseLogging || force)
        {
            Debug.Log(
                "[DurationTrackerSave] " +
                "SavedSeconds=" +
                wholeSeconds +
                " | StoredTodaySeconds=" +
                storedTodaySeconds +
                " | RemainingFraction=" +
                unsavedDurationSeconds.ToString("F3")
            );
        }
    }

    private bool ValidateReferences()
    {
        bool valid =
            true;

        if (beaconPostureBridge == null)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "BeaconPostureBridgeが設定されていません。"
            );

            valid =
                false;
        }

        if (unityFeatureBridge == null)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "UnityFeatureBridgeが設定されていません。"
            );

            valid =
                false;
        }

        return valid;
    }

    private bool GetMonitoringState()
    {
        if (beaconPostureBridge == null)
        {
            return false;
        }
    
        try
        {
            return beaconPostureBridge.IsMonitoring;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "監視状態を取得できませんでした。\n" +
                exception
            );
    
            return false;
        }
    }

    private bool GetBeaconNearState()
    {
        if (beaconPostureBridge == null)
        {
            return false;
        }

        try
        {
            return beaconPostureBridge
                .IsNearTargetBeacon();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "ビーコン接近状態を取得できませんでした。\n" +
                exception
            );

            return false;
        }
    }

    private bool GetFaceDownState()
    {
        if (beaconPostureBridge == null)
        {
            return false;
        }

        try
        {
            return beaconPostureBridge
                .IsFaceDown();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "伏せ状態を取得できませんでした。\n" +
                exception
            );

            return false;
        }
    }

    private bool GetConditionMetState()
    {
        if (beaconPostureBridge == null)
        {
            return false;
        }

        try
        {
            return beaconPostureBridge
                .IsConditionMet();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[DurationTracker] " +
                "積算条件を取得できませんでした。\n" +
                exception
            );

            return false;
        }
    }

    private long GetDisplayedTodaySeconds()
    {
        return storedTodaySeconds +
            (long)Math.Floor(
                unsavedDurationSeconds
            );
    }

    private long GetDisplayedTotalSeconds()
    {
        return storedTotalSeconds +
            (long)Math.Floor(
                unsavedDurationSeconds
            );
    }

    private void UpdateDisplay()
    {
        bool monitoring =
            GetMonitoringState();

        bool beaconNear =
            GetBeaconNearState();

        bool faceDown =
            GetFaceDownState();

        bool conditionMet =
            monitoring &&
            GetConditionMetState();

        if (conditionStatusText != null)
        {
            conditionStatusText.text =
                BuildConditionStatus(
                    monitoring,
                    beaconNear,
                    faceDown,
                    conditionMet
                );
        }

        if (currentSessionText != null)
        {
            currentSessionText.text =
                "現在の連続時間: " +
                FormatDuration(
                    (long)Math.Floor(
                        currentSessionSeconds
                    )
                );
        }

        if (todayDurationText != null)
        {
            todayDurationText.text =
                FormatDuration(
                    GetDisplayedTodaySeconds()
                );
        }

        if (totalDurationText != null)
        {
            totalDurationText.text =
                "総累積時間: " +
                FormatDuration(
                    GetDisplayedTotalSeconds()
                );
        }
    }

    private static string BuildConditionStatus(
        bool monitoring,
        bool beaconNear,
        bool faceDown,
        bool conditionMet
    )
    {
        if (!monitoring)
        {
            return
                "積算停止中\n" +
                "理由: ビーコン・姿勢監視が開始されていません。";
        }

        if (!beaconNear && !faceDown)
        {
            return
                "積算停止中\n" +
                "理由: ビーコンが近距離になく、" +
                "スマホも伏せ状態ではありません。";
        }

        if (!beaconNear)
        {
            return
                "積算停止中\n" +
                "理由: 対象ビーコンが近距離にありません。";
        }

        if (!faceDown)
        {
            return
                "積算停止中\n" +
                "理由: スマホが伏せ状態ではありません。";
        }

        if (conditionMet)
        {
            return
                "積算中\n" +
                "ビーコン接近: はい\n" +
                "スマホ伏せ: はい";
        }

        return
            "積算停止中\n" +
            "条件確定待ちです。";
    }

    private static string FormatDuration(
        long totalSeconds
    )
    {
        if (totalSeconds < 0L)
        {
            totalSeconds =
                0L;
        }

        long hours =
            totalSeconds /
            3600L;

        long minutes =
            (
                totalSeconds %
                3600L
                ) /
                60L;

        long seconds =
            totalSeconds %
            60L;

        return string.Format(
            "{0:00}:{1:00}:{2:00}",
            hours,
            minutes,
            seconds
        );
    }

    private void OnApplicationPause(
        bool pauseStatus
    )
    {
        if (!pauseStatus)
        {
            double now =
                Time.realtimeSinceStartupAsDouble;

            lastPollingTime =
                now;

            nextPollingTime =
                now;

            RefreshStoredDurations();
            UpdateDisplay();

            return;
        }

        IsAccumulating =
            false;

        FlushPendingDuration(
            true
        );

        conditionWasMet =
            false;

        currentSessionSeconds =
            0.0;

        UpdateDisplay();
    }

    private void OnApplicationFocus(
        bool hasFocus
    )
    {
        if (hasFocus)
        {
            double now =
                Time.realtimeSinceStartupAsDouble;

            lastPollingTime =
                now;

            nextPollingTime =
                now;
        } else {
            FlushPendingDuration(
                true
            );
        }
    }

    private void OnDisable()
    {
        IsAccumulating =
            false;

        FlushPendingDuration(
            true
        );

        conditionWasMet =
            false;

        currentSessionSeconds =
            0.0;
    }

    private void OnDestroy()
    {
        FlushPendingDuration(
            true
        );
    }

    private void OnApplicationQuit()
    {
        FlushPendingDuration(
            true
        );
    }
}