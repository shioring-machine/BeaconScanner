using UnityEngine;

public sealed class HealthHistoryPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject mainPanel;

    [SerializeField]
    private GameObject healthHistoryPanel;

    [Header("Graph")]
    [SerializeField]
    private HealthHistoryGraph healthHistoryGraph;

    private void Awake()
    {
        Debug.Log(
            "[PanelController] Awake called."
        );

        ValidateReferences();
    }

    private void Start()
    {
        Debug.Log(
            "[PanelController] Start called."
        );

        ShowMainPanel();
    }

    public void ShowHistoryPanel()
    {
        Debug.Log(
            "[PanelController] ShowHistoryPanel called."
        );

        if (!ValidateReferences())
        {
            return;
        }

        mainPanel.SetActive(false);
        healthHistoryPanel.SetActive(true);

        Debug.Log(
            "[PanelController] " +
            "MainPanel=False | " +
            "HealthHistoryPanel=True"
        );

        if (healthHistoryGraph != null)
        {
            healthHistoryGraph.RefreshGraph();
        }
        else
        {
            Debug.LogWarning(
                "[PanelController] " +
                "HealthHistoryGraph is not assigned."
            );
        }
    }

    public void ShowMainPanel()
    {
        Debug.Log(
            "[PanelController] ShowMainPanel called."
        );

        if (!ValidateReferences())
        {
            return;
        }

        healthHistoryPanel.SetActive(false);
        mainPanel.SetActive(true);

        Debug.Log(
            "[PanelController] " +
            "MainPanel=True | " +
            "HealthHistoryPanel=False"
        );
    }

    private bool ValidateReferences()
    {
        bool valid =
            true;

        if (mainPanel == null)
        {
            Debug.LogError(
                "[PanelController] " +
                "MainPanel is not assigned."
            );

            valid =
                false;
        }

        if (healthHistoryPanel == null)
        {
            Debug.LogError(
                "[PanelController] " +
                "HealthHistoryPanel is not assigned."
            );

            valid =
                false;
        }

        if (
            mainPanel != null &&
            healthHistoryPanel != null &&
            mainPanel == healthHistoryPanel
        )
        {
            Debug.LogError(
                "[PanelController] " +
                "MainPanel and HealthHistoryPanel " +
                "refer to the same GameObject."
            );

            valid =
                false;
        }

        return valid;
    }
}