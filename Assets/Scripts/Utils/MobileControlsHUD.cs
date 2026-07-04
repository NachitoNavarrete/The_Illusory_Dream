using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobileControlsHUD : MonoBehaviour
{
    private GameObject hudContainer;

    private void Start()
    {
        // 1. Only instantiate if ControlMode is set to Mobile
        string controlMode = PlayerPrefs.GetString("ControlMode", "PC");
        if (controlMode != "Mobile")
        {
            return;
        }

        // 2. Find canvas in current scene
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            var canvasComp = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvasComp != null) canvasGo = canvasComp.gameObject;
        }

        if (canvasGo == null)
        {
            Debug.LogError("MobileControlsHUD: No Canvas found in the scene to attach touch controls!");
            return;
        }

        // 3. Create HUD container
        hudContainer = new GameObject("MobileTouchHUD", typeof(RectTransform));
        hudContainer.transform.SetParent(canvasGo.transform, false);

        var hudRect = hudContainer.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.anchoredPosition = Vector2.zero;
        hudRect.sizeDelta = Vector2.zero;

        // Find reference font if possible
        TMP_FontAsset referenceFont = null;
        var existingText = canvasGo.GetComponentInChildren<TextMeshProUGUI>();
        if (existingText != null)
        {
            referenceFont = existingText.font;
        }

        // Create Left Panel: Z, X, C, S
        CreateLeftPanel(referenceFont);

        // Create Right Panel: Arrows & Q
        CreateRightPanel(referenceFont);

        // Create Top Right Pause Button
        CreatePauseButton(referenceFont);

        Debug.Log("MobileControlsHUD: Successfully built mobile touch controls!");
    }

    private void CreateLeftPanel(TMP_FontAsset font)
    {
        var panelGo = new GameObject("LeftPanel", typeof(RectTransform));
        panelGo.transform.SetParent(hudContainer.transform, false);

        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(40f, 40f);
        rect.sizeDelta = new Vector2(250f, 250f);

        // Create Z, X, C, S buttons
        // Arrangements:
        // S (Dash)     X (Shoot)
        // Z (Jump)     C (Parry)
        float bSize = 80f;
        float offset = 50f;
        CreateTouchButton(panelGo.transform, "S", "S", new Vector2(-offset, offset), new Vector2(bSize, bSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white);
        CreateTouchButton(panelGo.transform, "X", "X", new Vector2(offset, offset), new Vector2(bSize, bSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white);
        CreateTouchButton(panelGo.transform, "Z", "Z", new Vector2(-offset, -offset), new Vector2(bSize, bSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white);
        CreateTouchButton(panelGo.transform, "C", "C", new Vector2(offset, -offset), new Vector2(bSize, bSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white);
    }

    private void CreateRightPanel(TMP_FontAsset font)
    {
        var panelGo = new GameObject("RightPanel", typeof(RectTransform));
        panelGo.transform.SetParent(hudContainer.transform, false);

        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 40f);
        rect.sizeDelta = new Vector2(250f, 250f);

        // Cross D-Pad
        float arrowSize = 70f;
        float arrowOffset = 65f;
        CreateTouchButton(panelGo.transform, "UpArrow", "Up", new Vector2(0f, arrowOffset), new Vector2(arrowSize, arrowSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white, "▲");
        CreateTouchButton(panelGo.transform, "DownArrow", "Down", new Vector2(0f, -arrowOffset), new Vector2(arrowSize, arrowSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white, "▼");
        CreateTouchButton(panelGo.transform, "LeftArrow", "Left", new Vector2(-arrowOffset, 0f), new Vector2(arrowSize, arrowSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white, "◀");
        CreateTouchButton(panelGo.transform, "RightArrow", "Right", new Vector2(arrowOffset, 0f), new Vector2(arrowSize, arrowSize), font, new Color(0.12f, 0.12f, 0.16f, 0.75f), Color.white, "▶");

        // Q (Weapon Change) next to the arrows: placed to the left of the D-Pad
        CreateTouchButton(panelGo.transform, "QBtn", "Q", new Vector2(-135f, 0f), new Vector2(70f, 70f), font, new Color(0.4f, 0.1f, 0.1f, 0.75f), Color.white, "Q");
    }

    private void CreatePauseButton(TMP_FontAsset font)
    {
        var btnGo = new GameObject("PauseBtn", typeof(RectTransform));
        btnGo.transform.SetParent(hudContainer.transform, false);

        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-25f, -25f);
        rect.sizeDelta = new Vector2(100f, 50f);

        CreateTouchButton(hudContainer.transform, "Pause", "Pause", new Vector2(-75f, -50f), new Vector2(100f, 50f), font, new Color(0.2f, 0.2f, 0.2f, 0.8f), Color.white, "|| PAUSA");
    }

    private void CreateTouchButton(Transform parent, string goName, string btnName, Vector2 localPos, Vector2 size, TMP_FontAsset font, Color bgColor, Color textColor, string label = null)
    {
        var btnGo = new GameObject(goName, typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localPos;
        rect.sizeDelta = size;

        // Image Background
        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.color = bgColor;
        
        // Find default UISprite
        var uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (uiSprite != null)
        {
            img.sprite = uiSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }

        // Add TouchButton script
        var tb = btnGo.AddComponent<TouchButton>();
        tb.buttonName = btnName;

        // Add Text child
        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label ?? btnName;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size.y * 0.4f;
        tmp.color = textColor;
        if (font != null)
        {
            tmp.font = font;
        }
    }

    private void OnDestroy()
    {
        if (hudContainer != null)
        {
            Destroy(hudContainer);
        }
    }
}