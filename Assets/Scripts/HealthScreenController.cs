using UnityEngine;

public sealed class HealthScreenController : MonoBehaviour
{
    [Header("Main Screens")]
    [SerializeField]
    private GameObject homePanel;

    [SerializeField]
    private GameObject healthScreen;

    [Header("Health Screen Panels")]
    [SerializeField]
    private GameObject healthInputPanel;

    [SerializeField]
    private GameObject healthHistoryPanel;

    [Header("History Graph")]
    [SerializeField]
    private HealthHistoryGraph healthHistoryGraph;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        ShowHomeScreen();
    }

    public void ShowHomeScreen()
    {
        Debug.Log(
            "[HealthScreenController] ShowHomeScreen"
        );

        if (!ValidateReferences())
        {
            return;
        }

        homePanel.SetActive(true);
        healthScreen.SetActive(false);

        Debug.Log(
            "[HealthScreenController] " +
            "HomePanel=True | HealthScreen=False"
        );
    }

    public void ShowHealthInputPanel()
    {
        Debug.Log(
            "[HealthScreenController] ShowHealthInputPanel"
        );

        if (!ValidateReferences())
        {
            return;
        }

        homePanel.SetActive(false);
        healthScreen.SetActive(true);

        healthInputPanel.SetActive(true);
        healthHistoryPanel.SetActive(false);

        Debug.Log(
            "[HealthScreenController] " +
            "HomePanel=False | " +
            "HealthScreen=True | " +
            "HealthInputPanel=True | " +
            "HealthHistoryPanel=False"
        );
    }

    public void ShowHealthHistoryPanel()
    {
        Debug.Log(
            "[HealthScreenController] ShowHealthHistoryPanel"
        );

        if (!ValidateReferences())
        {
            return;
        }

        homePanel.SetActive(false);
        healthScreen.SetActive(true);

        healthInputPanel.SetActive(false);
        healthHistoryPanel.SetActive(true);

        Debug.Log(
            "[HealthScreenController] " +
            "HomePanel=False | " +
            "HealthScreen=True | " +
            "HealthInputPanel=False | " +
            "HealthHistoryPanel=True"
        );

        if (healthHistoryGraph != null)
        {
            healthHistoryGraph.RefreshGraph();
        }
        else
        {
            Debug.LogWarning(
                "[HealthScreenController] " +
                "HealthHistoryGraphが設定されていません。"
            );
        }
    }

    public void RefreshHistoryGraph()
    {
        if (healthHistoryGraph == null)
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HealthHistoryGraphが設定されていません。"
            );

            return;
        }

        healthHistoryGraph.RefreshGraph();
    }

    private bool ValidateReferences()
    {
        bool valid =
            true;

        if (homePanel == null)
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HomePanelが設定されていません。"
            );

            valid =
                false;
        }

        if (healthScreen == null)
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HealthScreenが設定されていません。"
            );

            valid =
                false;
        }

        if (healthInputPanel == null)
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HealthInputPanelが設定されていません。"
            );

            valid =
                false;
        }

        if (healthHistoryPanel == null)
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HealthHistoryPanelが設定されていません。"
            );

            valid =
                false;
        }

        if (
            healthInputPanel != null &&
            healthHistoryPanel != null &&
            healthInputPanel == healthHistoryPanel
        )
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HealthInputPanelとHealthHistoryPanelが" +
                "同じGameObjectを参照しています。"
            );

            valid =
                false;
        }

        if (
            homePanel != null &&
            healthScreen != null &&
            homePanel == healthScreen
        )
        {
            Debug.LogError(
                "[HealthScreenController] " +
                "HomePanelとHealthScreenが" +
                "同じGameObjectを参照しています。"
            );

            valid =
                false;
        }

        return valid;
    }
}