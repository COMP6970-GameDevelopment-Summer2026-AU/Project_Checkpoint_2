// HazardWarning.cs — shows an on-screen warning when the player approaches a
// hazard (cursed mist), escalating to "GET OUT" once they're inside taking damage.
// Fully self-contained: it boots itself and builds its own overlay canvas + label,
// so no builder/scene setup is required. Hazards call HazardWarning.Report(...).

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HazardWarning : MonoBehaviour
{
    public static HazardWarning Instance { get; private set; }

    public TextMeshProUGUI label;

    bool reported;
    bool inside;

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
        if (label == null) BuildUI();
        if (label != null) label.gameObject.SetActive(false);
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
    }

    public static void Report(bool insideDamage)
    {
        if (Instance == null) return;
        Instance.reported = true;
        Instance.inside |= insideDamage;
    }

    void LateUpdate()
    {
        if (label == null) return;

        if (reported)
        {
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (inside ? 11f : 5f));
            if (inside)
            {
                label.text = "!!  TAKING DAMAGE — GET OUT NOW  !!";
                label.color = new Color(1f, 0.2f, 0.2f, pulse);
            }
            else
            {
                label.text = "!  DANGER AHEAD — cursed ground, take action  !";
                label.color = new Color(1f, 0.72f, 0.2f, pulse);
            }
        }
        else if (label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(false);
        }

        reported = false;
        inside = false;
    }
}