using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HealthHistoryGraph : MonoBehaviour
{
    [Serializable]
    private sealed class HealthHistoryResponse
    {
        public bool success;
        public int count;
        public string message;
        public HealthHistoryEntry[] history;
    }

    [Serializable]
    private sealed class HealthHistoryEntry
    {
        public string date;
        public int score;
        public string label;
    }

    [Header("Android Bridge")]
    [SerializeField]
    private UnityFeatureBridge unityFeatureBridge;

    [Header("Graph")]
    [SerializeField]
    private RectTransform graphArea;

    [SerializeField]
    private Color lineColor =
        new Color(
            0.18f,
            0.65f,
            0.32f,
            1.0f
        );

    [SerializeField]
    private Color pointColor =
        new Color(
            0.10f,
            0.45f,
            0.20f,
            1.0f
        );

    [SerializeField]
    private float lineWidth =
        6.0f;

    [SerializeField]
    private float pointSize =
        18.0f;

    [Header("Text")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text historyText;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private TMP_Text minimumScoreText;

    [SerializeField]
    private TMP_Text maximumScoreText;

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    public void RefreshGraphFromButton()
    {
        RefreshGraph();
    }

    public void RefreshGraph()
    {
        ClearGeneratedGraph();

        if (unityFeatureBridge == null)
        {
            SetStatus(
                "UnityFeatureBridgeが設定されていません。"
            );

            return;
        }

        if (!unityFeatureBridge.IsInitialized)
        {
            SetStatus(
                "体調記録プラグインが初期化されていません。"
            );

            return;
        }

        string json =
            unityFeatureBridge.GetHistoryJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            SetStatus(
                "体調履歴を取得できませんでした。"
            );

            return;
        }

        HealthHistoryResponse response;

        try
        {
            response =
                JsonUtility.FromJson<HealthHistoryResponse>(
                    json
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "体調履歴JSONの解析に失敗しました。\n" +
                exception +
                "\nJSON=" +
                json
            );

            SetStatus(
                "体調履歴を解析できませんでした。"
            );

            return;
        }

        if (response == null)
        {
            SetStatus(
                "体調履歴の解析結果がありません。"
            );

            return;
        }

        if (!response.success)
        {
            string message =
                string.IsNullOrWhiteSpace(response.message)
                    ? "体調履歴を取得できませんでした。"
                    : response.message;

            SetStatus(
                message
            );

            return;
        }

        HealthHistoryEntry[] history =
            response.history ??
            Array.Empty<HealthHistoryEntry>();

        Array.Sort(
            history,
            CompareHistoryByDate
        );

        UpdateTextDisplay(
            history
        );

        if (history.Length == 0)
        {
            SetStatus(
                "体調履歴がまだありません。"
            );

            return;
        }

        DrawGraph(
            history
        );

        SetStatus(
            history.Length +
            "件の体調履歴を表示しました。"
        );

        Debug.Log(
            "[HealthGraph] " +
            "Count=" +
            history.Length +
            " | JSON=" +
            json
        );
    }

    public void ClearGraphFromButton()
    {
        ClearGeneratedGraph();

        if (historyText != null)
        {
            historyText.text =
                "履歴は表示されていません。";
        }

        SetStatus(
            "グラフ表示を消去しました。"
        );
    }

    private void DrawGraph(
        HealthHistoryEntry[] history
    )
    {
        if (graphArea == null)
        {
            SetStatus(
                "Graph Areaが設定されていません。"
            );

            return;
        }

        float width =
            graphArea.rect.width;

        float height =
            graphArea.rect.height;

        if (width <= 0.0f || height <= 0.0f)
        {
            SetStatus(
                "Graph Areaの大きさが不正です。"
            );

            return;
        }

        const float horizontalPadding =
            36.0f;

        const float verticalPadding =
            30.0f;

        float drawableWidth =
            Mathf.Max(
                1.0f,
                width -
                horizontalPadding * 2.0f
            );

        float drawableHeight =
            Mathf.Max(
                1.0f,
                height -
                verticalPadding * 2.0f
            );

        Vector2[] positions =
            new Vector2[history.Length];

        for (
            int index = 0;
            index < history.Length;
            index++
        )
        {
            int validatedScore =
                Mathf.Clamp(
                    history[index].score,
                    1,
                    5
                );

            float normalizedX =
                history.Length == 1
                    ? 0.5f
                    : (float)index /
                      (history.Length - 1);

            float normalizedY =
                (validatedScore - 1.0f) /
                4.0f;

            float x =
                -width * 0.5f +
                horizontalPadding +
                normalizedX * drawableWidth;

            float y =
                -height * 0.5f +
                verticalPadding +
                normalizedY * drawableHeight;

            positions[index] =
                new Vector2(
                    x,
                    y
                );
        }

        for (
            int index = 0;
            index < positions.Length - 1;
            index++
        )
        {
            CreateLineSegment(
                positions[index],
                positions[index + 1]
            );
        }

        for (
            int index = 0;
            index < positions.Length;
            index++
        )
        {
            CreatePoint(
                positions[index],
                history[index]
            );
        }
    }

    private void CreateLineSegment(
        Vector2 start,
        Vector2 end
    )
    {
        GameObject lineObject =
            new GameObject(
                "HealthGraphLine",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        lineObject.transform.SetParent(
            graphArea,
            false
        );

        RectTransform rectTransform =
            lineObject.GetComponent<RectTransform>();

        Image image =
            lineObject.GetComponent<Image>();

        image.color =
            lineColor;

        image.raycastTarget =
            false;

        Vector2 difference =
            end - start;

        float length =
            difference.magnitude;

        rectTransform.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rectTransform.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rectTransform.pivot =
            new Vector2(
                0.0f,
                0.5f
            );

        rectTransform.anchoredPosition =
            start;

        rectTransform.sizeDelta =
            new Vector2(
                length,
                lineWidth
            );

        float angle =
            Mathf.Atan2(
                difference.y,
                difference.x
            ) * Mathf.Rad2Deg;

        rectTransform.localEulerAngles =
            new Vector3(
                0.0f,
                0.0f,
                angle
            );

        generatedObjects.Add(
            lineObject
        );
    }

    private void CreatePoint(
        Vector2 position,
        HealthHistoryEntry entry
    )
    {
        GameObject pointObject =
            new GameObject(
                "HealthGraphPoint_" +
                entry.date,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        pointObject.transform.SetParent(
            graphArea,
            false
        );

        RectTransform rectTransform =
            pointObject.GetComponent<RectTransform>();

        Image image =
            pointObject.GetComponent<Image>();

        image.color =
            pointColor;

        image.raycastTarget =
            false;

        rectTransform.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rectTransform.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rectTransform.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rectTransform.anchoredPosition =
            position;

        rectTransform.sizeDelta =
            new Vector2(
                pointSize,
                pointSize
            );

        generatedObjects.Add(
            pointObject
        );
    }

    private void UpdateTextDisplay(
        HealthHistoryEntry[] history
    )
    {
        if (titleText != null)
        {
            titleText.text =
                "体調履歴";
        }

        if (minimumScoreText != null)
        {
            minimumScoreText.text =
                "1";
        }

        if (maximumScoreText != null)
        {
            maximumScoreText.text =
                "5";
        }

        if (historyText == null)
        {
            return;
        }

        if (history.Length == 0)
        {
            historyText.text =
                "履歴はまだありません。";

            return;
        }

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        for (
            int index = 0;
            index < history.Length;
            index++
        )
        {
            HealthHistoryEntry entry =
                history[index];

            builder.Append(
                entry.date
            );

            builder.Append(
                "  "
            );

            builder.Append(
                entry.score
            );

            builder.Append(
                "/5"
            );

            if (
                !string.IsNullOrWhiteSpace(
                    entry.label
                )
            )
            {
                builder.Append(
                    "  "
                );

                builder.Append(
                    entry.label
                );
            }

            if (
                index <
                history.Length - 1
            )
            {
                builder.AppendLine();
            }
        }

        historyText.text =
            builder.ToString();
    }

    private void ClearGeneratedGraph()
    {
        for (
            int index =
                generatedObjects.Count - 1;
            index >= 0;
            index--
        )
        {
            GameObject generatedObject =
                generatedObjects[index];

            if (generatedObject != null)
            {
                Destroy(
                    generatedObject
                );
            }
        }

        generatedObjects.Clear();
    }

    private void SetStatus(
        string message
    )
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }

        Debug.Log(
            "[HealthGraphStatus] " +
            message
        );
    }

    private static int CompareHistoryByDate(
        HealthHistoryEntry left,
        HealthHistoryEntry right
    )
    {
        string leftDate =
            left?.date ??
            string.Empty;

        string rightDate =
            right?.date ??
            string.Empty;

        return string.CompareOrdinal(
            leftDate,
            rightDate
        );
    }

    private void OnDestroy()
    {
        ClearGeneratedGraph();
    }
}