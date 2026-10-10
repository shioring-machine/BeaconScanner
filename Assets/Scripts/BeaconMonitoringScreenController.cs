using TMPro;
using UnityEngine;

public sealed class BeaconMonitoringScreenController :
    MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject homePanel;

    [SerializeField]
    private GameObject monitoringPanel;

    [Header("Plugin bridges")]
    [SerializeField]
    private BeaconPostureBridge beaconPostureBridge;

    [SerializeField]
    private BeaconFaceDownDurationTracker durationTracker;

    [Header("Monitoring texts")]
    [SerializeField]
    private TMP_Text monitoringStatusText;

    [SerializeField]
    private TMP_Text beaconReceivedText;

    [SerializeField]
    private TMP_Text beaconNearText;

    [SerializeField]
    private TMP_Text faceDownText;

    [SerializeField]
    private TMP_Text conditionStatusText;

    [SerializeField]
    private TMP_Text medianRssiText;

    [SerializeField]
    private TMP_Text gravityZText;

    [Header("Update")]
    [SerializeField]
    [Min(0.1f)]
    private float refreshIntervalSeconds =
        0.5f;

    private float nextRefreshTime;

    private void Awake()
    {
        ShowHomePanelInternal();

        nextRefreshTime =
            0.0f;
    }

    private void Update()
    {
        if (
            monitoringPanel == null ||
            !monitoringPanel.activeInHierarchy
        )
        {
            return;
        }

        if (
            Time.unscaledTime <
            nextRefreshTime
        )
        {
            return;
        }

        nextRefreshTime =
            Time.unscaledTime +
            refreshIntervalSeconds;

        RefreshMonitoringDisplay();
    }

    public void StartMonitoringAndShowPanel()
    {
        if (beaconPostureBridge == null)
        {
            Debug.LogError(
                "[MonitoringScreen] " +
                "BeaconPostureBridgeが設定されていません。"
            );

            return;
        }

        bool started =
            beaconPostureBridge.StartMonitoring();

        if (!started)
        {
            Debug.LogError(
                "[MonitoringScreen] " +
                "監視を開始できませんでした。 " +
                "Status=" +
                beaconPostureBridge.GetLatestStatus()
            );

            return;
        }

        ShowMonitoringPanelInternal();

        RefreshMonitoringDisplay();

        Debug.Log(
            "[MonitoringScreen] " +
            "監視を開始し、計測画面を表示しました。"
        );
    }

    public void ShowMonitoringPanel()
    {
        if (
            beaconPostureBridge == null ||
            !beaconPostureBridge.IsMonitoring
        )
        {
            Debug.LogWarning(
                "[MonitoringScreen] " +
                "監視が開始されていません。"
            );

            return;
        }

        ShowMonitoringPanelInternal();

        RefreshMonitoringDisplay();
    }

    public void BackToHomeWithoutStopping()
    {
        ShowHomePanelInternal();

        Debug.Log(
            "[MonitoringScreen] " +
            "監視を継続したままホームへ戻りました。"
        );
    }

    public void StopMonitoringAndReturnHome()
    {
        if (beaconPostureBridge != null)
        {
            beaconPostureBridge.StopMonitoring();
        }

        ShowHomePanelInternal();

        RefreshMonitoringDisplay();

        Debug.Log(
            "[MonitoringScreen] " +
            "監視を停止してホームへ戻りました。"
        );
    }

    public void RefreshMonitoringDisplay()
    {
        bool isMonitoring =
            beaconPostureBridge != null &&
            beaconPostureBridge.IsMonitoring;

        bool beaconReceived =
            beaconPostureBridge != null &&
            beaconPostureBridge
                .IsTargetBeaconReceived();

        bool beaconNear =
            beaconPostureBridge != null &&
            beaconPostureBridge
                .IsNearTargetBeacon();

        bool isFaceDown =
            beaconPostureBridge != null &&
            beaconPostureBridge
                .IsFaceDown();

        bool conditionMet =
            beaconPostureBridge != null &&
            beaconPostureBridge
                .IsConditionMet();

        int medianRssi =
            beaconPostureBridge != null
                ? beaconPostureBridge.GetMedianRssi()
                : int.MinValue;

        float gravityZ =
            beaconPostureBridge != null
                ? beaconPostureBridge.GetCurrentGravityZ()
                : float.NaN;

        SetText(
            monitoringStatusText,
            isMonitoring
                ? "監視状態：監視中"
                : "監視状態：停止中"
        );

        SetText(
            beaconReceivedText,
            beaconReceived
                ? "ビーコン受信：受信中"
                : "ビーコン受信：未受信"
        );

        SetText(
            beaconNearText,
            beaconNear
                ? "近接状態：近接"
                : "近接状態：範囲外"
        );

        SetText(
            faceDownText,
            isFaceDown
                ? "端末姿勢：伏せ状態"
                : "端末姿勢：通常"
        );

        SetText(
            conditionStatusText,
            GetConditionStatus(
                isMonitoring,
                beaconReceived,
                beaconNear,
                isFaceDown,
                conditionMet
            )
        );

        SetText(
            medianRssiText,
            medianRssi == int.MinValue
                ? "RSSI中央値：未取得"
                : "RSSI中央値：" +
                  medianRssi +
                  " dBm"
        );

        SetText(
            gravityZText,
            float.IsNaN(gravityZ)
                ? "重力Z：未取得"
                : "重力Z：" +
                  gravityZ.ToString("F2") +
                  " m/s²"
        );
    }

    private string GetConditionStatus(
        bool isMonitoring,
        bool beaconReceived,
        bool beaconNear,
        bool isFaceDown,
        bool conditionMet
    )
    {
        if (!isMonitoring)
        {
            return "停止中";
        }

        if (conditionMet)
        {
            return "計測中";
        }

        if (!beaconReceived)
        {
            return "ビーコンを確認しています";
        }

        if (!beaconNear)
        {
            return "ビーコンへ近づいてください";
        }

        if (!isFaceDown)
        {
            return "スマートフォンを伏せてください";
        }

        return "条件を確認しています";
    }

    private void ShowHomePanelInternal()
    {
        if (homePanel != null)
        {
            homePanel.SetActive(
                true
            );
        }

        if (monitoringPanel != null)
        {
            monitoringPanel.SetActive(
                false
            );
        }
    }

    private void ShowMonitoringPanelInternal()
    {
        if (homePanel != null)
        {
            homePanel.SetActive(
                false
            );
        }

        if (monitoringPanel != null)
        {
            monitoringPanel.SetActive(
                true
            );
        }

        nextRefreshTime =
            0.0f;
    }

    private void SetText(
        TMP_Text targetText,
        string value
    )
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text =
            value;
    }
}