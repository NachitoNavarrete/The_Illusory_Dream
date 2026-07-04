using UnityEngine;
/* EndGamePortal.cs: portal final del juego. Al entrar, muestra un diálogo misterioso
   (mismo estilo que CrowBoss) y termina con una pantalla de FIN / fin de la demo. */
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EndGamePortal : MonoBehaviour
{
    private readonly string[] dialogueLines =
    {
        "interesante...",
        "has logrado pasar por tus sueños...",
        "aun no sabes nada sobre este mundo y pronto lo sabras...",
        "pero ahora...debes luchar contra la realidad...",
        "despierta y ve...a tu verdadero...yo..."
    };

    private bool triggered = false;
    private bool showingDialogue = false;
    private bool showingEnd = false;
    private int currentIndex = 0;

    private GameObject dialoguePanel;
    private TMPro.TextMeshProUGUI dialogueText;
    private TMPro.TextMeshProUGUI speakerText;
    private Coroutine typewriterCoroutine;

    private GameObject endPanel;

    private TMPro.TMP_FontAsset RetroFont()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
#else
        return null;
#endif
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.CompareTag("Player") || other.GetComponent<RedMovement>() != null || other.GetComponentInParent<RedMovement>() != null)
        {
            triggered = true;
            BeginSequence(other.GetComponentInParent<RedMovement>() ?? other.GetComponent<RedMovement>());
        }
    }

    private void BeginSequence(RedMovement player)
    {
        // Congelar al jugador y pausar el tiempo (el diálogo usa tiempo real).
        if (player != null)
        {
            player.enabled = false;
            var prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = Vector2.zero;
        }
        MusicManager.StopBackgroundMusicStatic();
        Time.timeScale = 0f;

        CreateDialogueUI();
        showingDialogue = true;
        currentIndex = 0;
        ShowLine(dialogueLines[currentIndex]);
    }

    private void Update()
    {
        if (showingDialogue)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                if (typewriterCoroutine != null)
                {
                    // Completar la línea actual de golpe.
                    StopCoroutine(typewriterCoroutine);
                    typewriterCoroutine = null;
                    if (dialogueText != null && currentIndex < dialogueLines.Length)
                        dialogueText.text = dialogueLines[currentIndex];
                }
                else
                {
                    AdvanceLine();
                }
            }
        }
        else if (showingEnd)
        {
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Menu");
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                Time.timeScale = 1f;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }

    private void AdvanceLine()
    {
        currentIndex++;
        if (currentIndex < dialogueLines.Length)
        {
            ShowLine(dialogueLines[currentIndex]);
        }
        else
        {
            showingDialogue = false;
            if (dialoguePanel != null) Destroy(dialoguePanel);
            ShowEndScreen();
        }
    }

    private void ShowLine(string line)
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(line));
    }

    private IEnumerator TypewriterRoutine(string line)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.045f);
        }
        typewriterCoroutine = null;
    }

    private void CreateDialogueUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = canvasGo;
        }

        // dialoguePanel actúa como fondo negro de pantalla completa
        dialoguePanel = new GameObject("EndGameDialoguePanel", typeof(RectTransform));
        dialoguePanel.transform.SetParent(canvas.transform, false);

        var rect = dialoguePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bgImg = dialoguePanel.AddComponent<Image>();
        bgImg.color = Color.black;

        // Caja de diálogo centrada en la parte superior, sobre el fondo negro
        var boxGo = new GameObject("DialogueBox", typeof(RectTransform));
        boxGo.transform.SetParent(dialoguePanel.transform, false);
        var boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 1f);
        boxRect.anchorMax = new Vector2(0.5f, 1f);
        boxRect.pivot = new Vector2(0.5f, 1f);
        boxRect.sizeDelta = new Vector2(650f, 120f);
        boxRect.anchoredPosition = new Vector2(0f, -40f);

        var boxImg = boxGo.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.12f, 0.12f, 1.0f); // Caja gris oscuro elegante

        // Nombre del hablante (misterioso: "???")
        var speakerGo = new GameObject("SpeakerText", typeof(RectTransform));
        speakerGo.transform.SetParent(boxGo.transform, false);
        var speakerRect = speakerGo.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(0f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.sizeDelta = new Vector2(200f, 30f);
        speakerRect.anchoredPosition = new Vector2(25f, -12f);

        speakerText = speakerGo.AddComponent<TMPro.TextMeshProUGUI>();
        speakerText.text = "???";
        speakerText.fontSize = 20f;
        speakerText.fontStyle = TMPro.FontStyles.Bold;
        speakerText.color = Color.white;

        var textGo = new GameObject("DialogueText", typeof(RectTransform));
        textGo.transform.SetParent(boxGo.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-50f, -55f);
        textRect.anchoredPosition = new Vector2(0f, -15f);

        dialogueText = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        dialogueText.text = "";
        dialogueText.fontSize = 17f;
        dialogueText.color = Color.white;

        var promptGo = new GameObject("PromptText", typeof(RectTransform));
        promptGo.transform.SetParent(boxGo.transform, false);
        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(1f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(1f, 0f);
        promptRect.sizeDelta = new Vector2(250f, 20f);
        promptRect.anchoredPosition = new Vector2(-20f, 12f);

        var promptTmp = promptGo.AddComponent<TMPro.TextMeshProUGUI>();
        promptTmp.text = "Presiona [Z] para avanzar";
        promptTmp.fontSize = 11f;
        promptTmp.fontStyle = TMPro.FontStyles.Italic;
        promptTmp.color = new Color(0.8f, 0.8f, 0.8f);
        promptTmp.alignment = TMPro.TextAlignmentOptions.Right;

        var font = RetroFont();
        if (font != null)
        {
            speakerText.font = font;
            dialogueText.font = font;
            promptTmp.font = font;
        }

        dialoguePanel.transform.SetAsLastSibling();
    }

    private void ShowEndScreen()
    {
        showingEnd = true;

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = canvasGo;
        }

        endPanel = new GameObject("EndGamePanel", typeof(RectTransform));
        endPanel.transform.SetParent(canvas.transform, false);
        var rect = endPanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = endPanel.AddComponent<Image>();
        img.color = Color.black;

        var font = RetroFont();

        // "FIN"
        var finGo = new GameObject("FinText", typeof(RectTransform));
        finGo.transform.SetParent(endPanel.transform, false);
        var finRect = finGo.GetComponent<RectTransform>();
        finRect.anchorMin = new Vector2(0.5f, 0.5f);
        finRect.anchorMax = new Vector2(0.5f, 0.5f);
        finRect.pivot = new Vector2(0.5f, 0.5f);
        finRect.sizeDelta = new Vector2(800f, 160f);
        finRect.anchoredPosition = new Vector2(0f, 40f);
        var finTmp = finGo.AddComponent<TMPro.TextMeshProUGUI>();
        finTmp.text = "FIN";
        finTmp.fontSize = 96f;
        finTmp.fontStyle = TMPro.FontStyles.Bold;
        finTmp.alignment = TMPro.TextAlignmentOptions.Center;
        finTmp.color = Color.white;
        if (font != null) finTmp.font = font;

        // Subtítulo demo
        var subGo = new GameObject("SubText", typeof(RectTransform));
        subGo.transform.SetParent(endPanel.transform, false);
        var subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.sizeDelta = new Vector2(800f, 60f);
        subRect.anchoredPosition = new Vector2(0f, -40f);
        var subTmp = subGo.AddComponent<TMPro.TextMeshProUGUI>();
        subTmp.text = "...? La demo termina aquí.";
        subTmp.fontSize = 28f;
        subTmp.alignment = TMPro.TextAlignmentOptions.Center;
        subTmp.color = new Color(0.85f, 0.85f, 0.85f);
        if (font != null) subTmp.font = font;

        // Prompt de opciones
        var optGo = new GameObject("OptionsText", typeof(RectTransform));
        optGo.transform.SetParent(endPanel.transform, false);
        var optRect = optGo.GetComponent<RectTransform>();
        optRect.anchorMin = new Vector2(0.5f, 0f);
        optRect.anchorMax = new Vector2(0.5f, 0f);
        optRect.pivot = new Vector2(0.5f, 0f);
        optRect.sizeDelta = new Vector2(800f, 50f);
        optRect.anchoredPosition = new Vector2(0f, 60f);
        var optTmp = optGo.AddComponent<TMPro.TextMeshProUGUI>();
        optTmp.text = "[M] Volver al menú     [Q] Salir del juego";
        optTmp.fontSize = 22f;
        optTmp.alignment = TMPro.TextAlignmentOptions.Center;
        optTmp.color = new Color(0.7f, 0.9f, 1f);
        if (font != null) optTmp.font = font;

        endPanel.transform.SetAsLastSibling();
    }
}
