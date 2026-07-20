// MainMenuBuilder.cs — builds the title-screen scene from the artwork and makes
// it the first scene, so the game boots to the menu and PLAY loads the game.
//
//   Tools ▸ Graveyard Keeper ▸ Build Main Menu
//
// Run Build World first (so the "Graveyard" scene exists to load into).

#if UNITY_EDITOR
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    const string TITLE_TEX = "Assets/Art/UI/GraveyardRun_Title.png";
    const string MENU_SCENE = "Assets/Scenes/MainMenu.unity";
    const string GAME_SCENE = "Assets/Scenes/Graveyard.unity";

    [MenuItem("Tools/Graveyard Keeper/Build Main Menu")]
    public static void BuildMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera (for an AudioListener + a solid backdrop).
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.AddComponent<AudioListener>();

        // Canvas
        var canvasGO = new GameObject("Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Transform C = canvasGO.transform;

        // Dark full-screen backdrop (fills letterbox bars).
        var bg = new GameObject("Backdrop").AddComponent<Image>();
        bg.transform.SetParent(C, false);
        bg.color = new Color(0.02f, 0.03f, 0.05f, 1f);
        Stretch(bg.rectTransform);

        // Title artwork, aspect-fit so it never distorts.
        var holderGO = new GameObject("TitleArt", typeof(RawImage), typeof(AspectRatioFitter));
        holderGO.transform.SetParent(C, false);
        Stretch((RectTransform)holderGO.transform);
        var raw = holderGO.GetComponent<RawImage>();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TITLE_TEX);
        raw.texture = tex;
        if (tex == null)
            Debug.LogWarning("[MainMenu] Title texture not found at " + TITLE_TEX);
        var fitter = holderGO.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1536f / 1024f;

        // Invisible PLAY hotspot over the drawn "PLAY NOW" button (normalized to the art).
        var hotGO = new GameObject("PlayHotspot", typeof(RectTransform));
        hotGO.transform.SetParent(holderGO.transform, false);
        var hot = (RectTransform)hotGO.transform;
        hot.anchorMin = new Vector2(0.33f, 0.085f);
        hot.anchorMax = new Vector2(0.63f, 0.165f);
        hot.offsetMin = hot.offsetMax = Vector2.zero;

        // Small hint text at the very bottom.
        var hint = new GameObject("Hint").AddComponent<Text>();
        hint.transform.SetParent(C, false);
        hint.text = "Click PLAY  ·  or press Enter";
        hint.alignment = TextAnchor.LowerCenter;
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        hint.fontSize = 22;
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f); hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 18f);
        hrt.sizeDelta = new Vector2(600f, 30f);

        // Controller
        var menuGO = new GameObject("_MainMenu");
        var mc = menuGO.AddComponent<MainMenuController>();
        mc.playHotspot = hot;
        mc.gameSceneName = "Graveyard";

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, MENU_SCENE);
        SetBuildOrder();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Main Menu",
            "Title screen built and set as the first scene.\n\n" +
            "Open Assets/Scenes/MainMenu.unity and press Play — click PLAY (or press\n" +
            "Enter) to start the game.\n\n" +
            (AssetDatabase.LoadAssetAtPath<Object>(GAME_SCENE) == null
                ? "Note: build the game first (Tools ▸ Graveyard Keeper ▸ Build World)."
                : "The game scene is ready."),
            "OK");
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetBuildOrder()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == MENU_SCENE || s.path == GAME_SCENE);
        scenes.Insert(0, new EditorBuildSettingsScene(MENU_SCENE, true));
        scenes.Add(new EditorBuildSettingsScene(GAME_SCENE, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif