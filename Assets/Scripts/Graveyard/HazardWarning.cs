// HazardWarning.cs — on-screen hazard warning + a live DEBUG panel.
// Self-contained: boots itself and builds its own overlay canvas, a center warning
// banner, and a small top-left debug readout so you can see whether the system is
// working (instance alive, hazard count, nearest distance, warning state).
// Toggle the debug panel with F3.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HazardWarning : MonoBehaviour
{
    public static HazardWarning Instance { get; private set; }

    public TextMeshProUGUI label;    // center warning banner
    TextMeshProUGUI debug;           // top-left debug readout

    bool reported;
    bool inside;
    float nearest = 9999f;
    bool wasShowing;
    bool debugOn = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("~HazardWarning");
        go.AddComponent<HazardWarning>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        if (label != null) label.gameObject.SetActive(false);
        Debug.Log("[HazardWarning] Ready (self-built UI). Press F3 to toggle the debug panel.");
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HazardWarningCanvas", typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Center warning banner
        var go = new GameObject("HazardWarningLabel");
        go.transform.SetParent(canvasGO.transform, false);
        label = go.AddComponent<TextMeshProUGUI>();
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 150f);
        rt.sizeDelta = new Vector2(1100f, 70f);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 34;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;

        // Top-left debug panel background
        var bgGO = new GameObject("HazardDebugBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;
        var brt = bg.rectTransform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(20f, -260f);
        brt.sizeDelta = new Vector2(420f, 150f);

        // Debug text
        var dGO = new GameObject("HazardDebugText");
        dGO.transform.SetParent(bgGO.transform, false);
        debug = dGO.AddComponent<TextMeshProUGUI>();
        var drt = debug.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = new Vector2(10f, 8f); drt.offsetMax = new Vector2(-10f, -8f);
        debug.alignment = TextAlignmentOptions.TopLeft;
        debug.fontSize = 18;
        debug.color = new Color(0.6f, 1f, 0.7f);
        debug.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) debug.font = TMP_Settings.defaultFontAsset;
    }

    public static void Report(bool insideDamage)
    {
        if (Instance == null) return;
        Instance.reported = true;
        Instance.inside |= insideDamage;
    }

    public static void ReportDistance(float d)
    {
        if (Instance == null) return;
        if (d < Instance.nearest) Instance.nearest = d;
    }

    void Update()
    {
        if (GKInput.DebugTogglePressed() && debug != null)
        {
            debugOn = !debugOn;
            debug.transform.parent.gameObject.SetActive(debugOn);
        }
    }

    void LateUpdate()
    {
        // Warning banner
        bool showing = reported;
        if (label != null)
        {
            if (showing)
            {
                if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (inside ? 11f : 5f));
                if (inside) { label.text = "!!  TAKING DAMAGE — GET OUT NOW  !!"; label.color = new Color(1f, 0.2f, 0.2f, pulse); }
                else { label.text = "!  DANGER AHEAD — cursed ground, take action  !"; label.color = new Color(1f, 0.72f, 0.2f, pulse); }
            }
            else if (label.gameObject.activeSelf) label.gameObject.SetActive(false);
        }

        // Console log on state change
        if (showing && !wasShowing) Debug.Log($"[HazardWarning] SHOWING (inside={inside}, nearest={nearest:F1}m)");
        else if (!showing && wasShowing) Debug.Log("[HazardWarning] hidden");
        wasShowing = showing;

        // Debug panel
        if (debug != null && debugOn)
        {
            bool playing = GraveyardManager.Instance != null && GraveyardManager.Instance.IsPlaying;
            bool playerFound = GameObject.FindWithTag("Player") != null;
            string state = showing ? (inside ? "<color=#FF6060>SHOWING (inside)</color>" : "<color=#FFB84D>SHOWING (approach)</color>") : "hidden";
            debug.text =
                "<b>HAZARD DEBUG (F3)</b>\n" +
                $"Instance: OK\n" +
                $"Playing: {playing}   Player: {(playerFound ? "found" : "<color=#FF6060>NULL</color>")}\n" +
                $"Active hazards: {HazardZone.ActiveCount}\n" +
                $"Nearest: {(nearest > 9000f ? "—" : nearest.ToString("F1") + " m")}  (warn < 6.5)\n" +
                $"Warning: {state}";
        }

        reported = false;
        inside = false;
        nearest = 9999f;
    }
}