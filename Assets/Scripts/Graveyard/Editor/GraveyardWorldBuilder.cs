// GraveyardWorldBuilder.cs — Editor tool (Modules 6 & 7).
// One click builds the whole playable Checkpoint-2 scene from the Kenney
// Graveyard Kit: terrain, night lighting + day/night cycle, a third-person
// keeper with a follow camera, a fenced graveyard scattered with decoration and
// harvestable resource nodes (Wood / Stone / Pumpkin), wandering ghosts, and a
// full HUD (resource counters, objective, night timer, interaction prompt,
// minimap, compass, end panel). This IS the M7 "custom scatter tool".
//
// USAGE:  Tools ▸ Graveyard Keeper ▸ Build World (Checkpoint 2)
//
// Requires the Kenney FBX models under Assets/Art/Graveyard/ (they ship with
// this package) and TMP Essentials imported (Window ▸ TextMeshPro ▸ Import TMP
// Essential Resources).

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GraveyardWorldBuilder
{
    const float SIZE = 80f;                 // square terrain size
    const float HALF = SIZE / 2f;
    const string COLORMAP = "Assets/Art/Graveyard/Textures/colormap.png";
    const string SCENE_PATH = "Assets/Scenes/Graveyard.unity";

    static Material colormapMat;

    [MenuItem("Tools/Graveyard Keeper/Build World (Checkpoint 2)")]
    public static void BuildWorld()
    {
        // Fresh, empty scene so we never clobber other work.
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        colormapMat = BuildColormapMaterial();

        BuildEndlessGround();
        BuildLighting();
        BuildNightSky();

        GameObject player = BuildPlayer(null);
        Camera cam = BuildCamera(player.transform);
        player.GetComponent<ThirdPersonController>().cameraTransform = cam.transform;

        BuildEndlessWorld(player.transform);
        BuildSkyDetails(cam.transform);
        BuildGhostVoices();

        var minimapCam = BuildMinimapCamera(player.transform, out RenderTexture minimapRT);

        GraveyardManager gm = BuildManagers();
        BuildHUD(gm, cam.transform, minimapRT, player);
        BuildEscapeGate(player.transform);
        BuildPostFX(cam);

        // Save + add to build settings.
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AddSceneToBuild(SCENE_PATH);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Graveyard Keeper",
            "Checkpoint 2 world built!\n\nPress Play to test.\n\n" +
            "Goal: harvest resources, banish ghosts to collect souls, then reach the\n" +
            "green beacon (escape gate) and press E to escape. Ghosts damage you — if\n" +
            "your health hits 0 you perish. Beware the green cursed mist.\n\n" +
            "Controls: WASD/Arrows move · Mouse look · Shift sprint · E interact ·\n" +
            "Left-click attack · Esc free cursor.\n\n" +
            "Next: Tools ▸ Graveyard Keeper ▸ Setup Axe Pack Character, then Build Main Menu.",
            "OK");
    }

    // ── Night sky (skybox + moon + stars) ───────────────────────────────────────
    static void BuildNightSky()
    {
        var sky = new Material(Shader.Find("Skybox/Procedural"));
        if (sky != null)
        {
            if (sky.HasProperty("_SunSize"))         sky.SetFloat("_SunSize", 0.03f);
            if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 3f);
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.4f);
            if (sky.HasProperty("_SkyTint"))         sky.SetColor("_SkyTint", new Color(0.08f, 0.10f, 0.20f));
            if (sky.HasProperty("_GroundColor"))     sky.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.04f));
            if (sky.HasProperty("_Exposure"))        sky.SetFloat("_Exposure", 0.45f);
            RenderSettings.skybox = sky;
        }
    }

    static void BuildSkyDetails(Transform cam)
    {
        var go = new GameObject("_Sky");
        var moon = go.AddComponent<MoonSky>();
        moon.cameraTransform = cam;
        // cycle is found at runtime if left null
    }

    static void BuildGhostVoices()
    {
        var go = new GameObject("_GhostVoices");
        go.AddComponent<GhostVoiceDirector>();
    }

    // ── Endless ground + streamer ───────────────────────────────────────────────
    static void BuildEndlessGround()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "EndlessGround";
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = new Vector3(200f, 1f, 200f);   // 2000 x 2000 units

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        var tex = MakeGroundTexture();
        tex.wrapMode = TextureWrapMode.Repeat;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTextureScale = new Vector2(400f, 400f);
        plane.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void BuildEndlessWorld(Transform player)
    {
        var go = new GameObject("_EndlessWorld");
        var ew = go.AddComponent<EndlessWorld>();
        ew.player = player;
        ew.colormap = colormapMat;
        ew.woodModels    = LoadModels("pine", "pine-crooked", "pine-fall", "trunk", "trunk-long");
        ew.stoneModels   = LoadModels("rocks", "rocks-tall", "gravestone-debris", "debris");
        ew.pumpkinModels = LoadModels("pumpkin", "pumpkin-tall", "pumpkin-carved");
        ew.decorModels   = LoadModels("gravestone-bevel", "gravestone-round", "gravestone-cross",
                                      "gravestone-wide", "grave", "cross", "crypt", "crypt-small",
                                      "coffin", "pillar-obelisk", "lightpost-single", "lantern-glass",
                                      "urn-round", "bench", "fire-basket", "iron-fence");
        ew.ghostModel    = FindModel("character-ghost");
        ew.coffinModels  = LoadModels("coffin", "coffin-old");
    }

    // ── Escape gate + post-processing ───────────────────────────────────────────
    static void BuildEscapeGate(Transform player)
    {
        var model = FindModel("crypt") ?? FindModel("gravestone-cross") ?? FindModel("cross");
        var go = model != null ? (GameObject)Object.Instantiate(model) : new GameObject("Gate");
        go.name = "EscapeGate";
        ApplyColormap(go);
        // Place it a short walk north of the spawn so the player has to travel.
        go.transform.position = new Vector3(0f, 0f, 42f);
        go.AddComponent<EscapeGate>();
    }

    static void BuildPostFX(Camera cam)
    {
        try
        {
            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            Directory.CreateDirectory("Assets/Art");
            AssetDatabase.CreateAsset(profile, "Assets/Art/GraveyardPostFX.asset");

            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.intensity.Override(0.7f);
            bloom.threshold.Override(0.9f);

            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vig.intensity.Override(0.38f);
            vig.smoothness.Override(0.4f);

            var ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            ca.postExposure.Override(-0.15f);
            ca.contrast.Override(12f);
            ca.saturation.Override(-8f);

            // Persist the overrides inside the profile asset.
            foreach (var comp in profile.components)
            {
                comp.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(comp, profile);
            }
            AssetDatabase.SaveAssets();

            var volGO = new GameObject("Global Volume");
            var vol = volGO.AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.profile = profile;

            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Builder] Post-processing setup skipped: " + e.Message);
        }
    }

    static GameObject[] LoadModels(params string[] names)
    {
        var list = new List<GameObject>();
        foreach (var n in names)
        {
            var m = FindModel(n);
            if (m != null) list.Add(m);
        }
        return list.ToArray();
    }

    // ── Materials ───────────────────────────────────────────────────────────────
    static Material BuildColormapMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(s) { name = "GraveyardColormap" };
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(COLORMAP);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        Directory.CreateDirectory("Assets/Art/Graveyard");
        AssetDatabase.CreateAsset(mat, "Assets/Art/Graveyard/GraveyardColormap.mat");
        return mat;
    }

    // ── Terrain (M6.1) ──────────────────────────────────────────────────────────
    static Terrain BuildTerrain()
    {
        try
        {
            var data = new TerrainData
            {
                heightmapResolution = 129,
                size = new Vector3(SIZE, 6f, SIZE)
            };

            int res = data.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    heights[y, x] = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.12f;
            data.SetHeights(0, 0, heights);

            // A simple ground layer so the terrain is not flat magenta.
            try
            {
                var layer = new TerrainLayer { diffuseTexture = MakeGroundTexture(), tileSize = new Vector2(8, 8) };
                Directory.CreateDirectory("Assets/Art/Graveyard");
                AssetDatabase.CreateAsset(layer, "Assets/Art/Graveyard/GroundLayer.terrainlayer");
                data.terrainLayers = new[] { layer };
            }
            catch { /* layer is cosmetic; ignore if it fails */ }

            AssetDatabase.CreateAsset(data, "Assets/Art/Graveyard/GraveyardTerrain.asset");

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.position = new Vector3(-HALF, 0f, -HALF);
            return go.GetComponent<Terrain>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Builder] Terrain failed, using a ground plane instead: " + e.Message);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.18f, 0.2f, 0.16f);
            plane.GetComponent<Renderer>().sharedMaterial = mat;
            return null;
        }
    }

    static Texture2D MakeGroundTexture()
    {
        var tex = new Texture2D(64, 64);
        var baseCol = new Color(0.16f, 0.19f, 0.15f);
        var rng = new System.Random(3);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float n = (float)rng.NextDouble() * 0.08f;
                tex.SetPixel(x, y, baseCol + new Color(n, n, n));
            }
        tex.Apply();
        return tex;
    }

    static float GroundY(Terrain terrain, float x, float z)
    {
        if (terrain != null) return terrain.SampleHeight(new Vector3(x, 0f, z));
        return 0f;
    }

    // ── Lighting + day/night (M6.3) ─────────────────────────────────────────────
    static void BuildLighting()
    {
        var go = new GameObject("Sun (Moon)");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.intensity = 0.6f;
        go.transform.rotation = Quaternion.Euler(200f, -30f, 0f);
        go.AddComponent<DayNightCycle>();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.22f);
    }

    // ── Fencing + decoration ────────────────────────────────────────────────────
    static void BuildBorderFence(Terrain terrain)
    {
        var model = FindModel("iron-fence") ?? FindModel("fence") ?? FindModel("brick-wall");
        var root = new GameObject("World_Fence").transform;
        float step = 4f;
        for (float d = -HALF + 4f; d <= HALF - 4f; d += step)
        {
            SpawnAt(model, terrain, d, -HALF + 4f, Quaternion.Euler(0, 0, 0), root);
            SpawnAt(model, terrain, d,  HALF - 4f, Quaternion.Euler(0, 180, 0), root);
            SpawnAt(model, terrain, -HALF + 4f, d, Quaternion.Euler(0, 90, 0), root);
            SpawnAt(model, terrain,  HALF - 4f, d, Quaternion.Euler(0, -90, 0), root);
        }
    }

    static void ScatterDecor(Terrain terrain, int count)
    {
        string[] names =
        {
            "gravestone-bevel", "gravestone-round", "gravestone-cross", "gravestone-wide",
            "grave", "cross", "crypt", "crypt-small", "coffin", "pillar-obelisk",
            "lightpost-single", "lantern-glass", "candle", "urn-round", "bench", "fire-basket"
        };
        var root = new GameObject("World_Decor").transform;
        var rng = new System.Random(11);
        for (int i = 0; i < count; i++)
        {
            var model = FindModel(names[rng.Next(names.Length)]);
            float x = (float)(rng.NextDouble() * (SIZE - 14) - (HALF - 7));
            float z = (float)(rng.NextDouble() * (SIZE - 14) - (HALF - 7));
            if (Mathf.Abs(x) < 6f && Mathf.Abs(z) < 6f) continue;   // keep spawn clear
            var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            SpawnAt(model, terrain, x, z, rot, root);
        }
    }

    // ── Harvestable resources (M7.1) ────────────────────────────────────────────
    static void ScatterResources(Terrain terrain, Transform root, Harvestable.ResourceType type,
                                 string[] modelNames, int count, int hits, float scale)
    {
        var rng = new System.Random((int)type * 97 + 5);
        var typeRoot = new GameObject($"Resources_{type}").transform;
        typeRoot.SetParent(root);

        for (int i = 0; i < count; i++)
        {
            var model = FindModel(modelNames[rng.Next(modelNames.Length)]);
            float x, z; int guard = 0;
            do
            {
                x = (float)(rng.NextDouble() * (SIZE - 16) - (HALF - 8));
                z = (float)(rng.NextDouble() * (SIZE - 16) - (HALF - 8));
                guard++;
            } while (Mathf.Abs(x) < 6f && Mathf.Abs(z) < 6f && guard < 10);

            var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            GameObject go = SpawnAt(model, terrain, x, z, rot, typeRoot, scale);

            // Collider sized to the model so the player can detect + bump it.
            var bounds = GetBounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(bounds.center);
            Vector3 s = bounds.size;
            box.size = new Vector3(
                Mathf.Max(0.5f, s.x / go.transform.lossyScale.x),
                Mathf.Max(0.5f, s.y / go.transform.lossyScale.y),
                Mathf.Max(0.5f, s.z / go.transform.lossyScale.z));

            var h = go.AddComponent<Harvestable>();
            h.type = type;
            h.hitsToHarvest = hits;
            h.yieldAmount = 1;
        }
    }

    // ── Player (M6.2) ───────────────────────────────────────────────────────────
    static GameObject BuildPlayer(Terrain terrain)
    {
        var root = new GameObject("Player");
        root.tag = "Player";

        var model = FindModel("character-keeper") ?? FindModel("character-skeleton");
        GameObject mesh = model != null ? (GameObject)Object.Instantiate(model)
                                        : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mesh.name = "Keeper_Mesh";
        mesh.transform.SetParent(root.transform, false);
        ApplyColormap(mesh);

        // Adaptive scale/placement so the mesh's feet sit at the root origin.
        var b = GetBounds(mesh);
        float meshHeight = Mathf.Max(0.5f, b.size.y);
        float targetHeight = 1.7f;
        float scale = targetHeight / meshHeight;
        mesh.transform.localScale = Vector3.one * scale;
        b = GetBounds(mesh);
        mesh.transform.localPosition -= new Vector3(0f, b.min.y - root.transform.position.y, 0f);

        var cc = root.AddComponent<CharacterController>();
        cc.height = targetHeight;
        cc.center = new Vector3(0f, targetHeight / 2f, 0f);
        cc.radius = 0.3f;

        var tpc = root.AddComponent<ThirdPersonController>();
        tpc.modelRoot = mesh.transform;
        root.AddComponent<PlayerInteractor>();
        root.AddComponent<CombatController>();
        root.AddComponent<PlayerHealth>();

        // Animation driver (harmless until a rigged humanoid + controller is added).
        var ka = root.AddComponent<KeeperAnimator>();
        tpc.keeperAnimator = ka;

        float y = GroundY(terrain, 0f, 0f) + 0.2f;
        root.transform.position = new Vector3(0f, y, 0f);
        return root;
    }

    static Camera BuildCamera(Transform target)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        cam.farClipPlane = 1200f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        var rig = go.AddComponent<CameraRig>();
        rig.target = target;
        return cam;
    }

    static void ScatterGhosts(Terrain terrain, int count)
    {
        var model = FindModel("character-ghost");
        var root = new GameObject("Ghosts").transform;
        var rng = new System.Random(42);
        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * (SIZE - 24) - (HALF - 12));
            float z = (float)(rng.NextDouble() * (SIZE - 24) - (HALF - 12));
            var go = SpawnAt(model, terrain, x, z, Quaternion.identity, root);
            go.transform.position += Vector3.up * 1.2f;
            go.AddComponent<GhostWander>();
        }
    }

    static Camera BuildMinimapCamera(Transform target, out RenderTexture rt)
    {
        rt = new RenderTexture(256, 256, 16) { name = "MinimapRT" };
        AssetDatabase.CreateAsset(rt, "Assets/Art/Graveyard/MinimapRT.renderTexture");

        var go = new GameObject("Minimap Camera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 22f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
        cam.targetTexture = rt;
        var follow = go.AddComponent<MinimapFollow>();
        follow.target = target;
        return cam;
    }

    // ── Managers ────────────────────────────────────────────────────────────────
    static GraveyardManager BuildManagers()
    {
        var go = new GameObject("_GameManager");
        var gm = go.AddComponent<GraveyardManager>();
        // AudioManager is NOT added here: it self-boots onto its own persistent
        // object (see AudioManager.Boot), so reloading the scene resets the game
        // state cleanly while the audio keeps playing.
        return gm;
    }

    // ── HUD (M6.4 + M7.2) ───────────────────────────────────────────────────────
    static void BuildHUD(GraveyardManager gm, Transform cam, RenderTexture minimapRT, GameObject player)
    {
        var canvasGO = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Transform C = canvasGO.transform;

        // Resource counters (top-left).
        gm.woodText    = Text("Wood",    C, new Vector2(0,1), new Vector2(30,-30),  new Vector2(320,44), 30, TextAlignmentOptions.Left, Color.white);
        gm.stoneText   = Text("Stone",   C, new Vector2(0,1), new Vector2(30,-78),  new Vector2(320,44), 30, TextAlignmentOptions.Left, Color.white);
        gm.pumpkinText = Text("Pumpkins",C, new Vector2(0,1), new Vector2(30,-126), new Vector2(320,44), 30, TextAlignmentOptions.Left, new Color(1f,0.6f,0.2f));
        gm.soulText    = Text("Souls",   C, new Vector2(0,1), new Vector2(30,-174), new Vector2(340,44), 30, TextAlignmentOptions.Left, new Color(0.5f,1f,0.7f));
        gm.banishText  = Text("Spirits", C, new Vector2(0,1), new Vector2(30,-222), new Vector2(360,40), 26, TextAlignmentOptions.Left, new Color(0.7f,0.85f,1f));

        // Health bar (top-center, prominent). Driven by width (no sprite needed).
        const float BAR_W = 460f, BAR_H = 30f, PAD = 4f;

        var hpBg = new GameObject("HealthBG").AddComponent<Image>();
        hpBg.transform.SetParent(C, false);
        hpBg.color = new Color(0.04f, 0.05f, 0.06f, 0.85f);
        var hbg = hpBg.rectTransform;
        hbg.anchorMin = hbg.anchorMax = hbg.pivot = new Vector2(0.5f, 1f);
        hbg.anchoredPosition = new Vector2(0, -104);
        hbg.sizeDelta = new Vector2(BAR_W, BAR_H);
        var hpOutline = hpBg.gameObject.AddComponent<Outline>();
        hpOutline.effectColor = new Color(0.5f, 0.75f, 0.55f, 0.9f);
        hpOutline.effectDistance = new Vector2(2f, -2f);

        var hpFill = new GameObject("HealthFill").AddComponent<Image>();
        hpFill.transform.SetParent(hpBg.transform, false);
        hpFill.color = new Color(0.35f, 0.85f, 0.35f, 1f);
        var hf = hpFill.rectTransform;
        hf.anchorMin = hf.anchorMax = new Vector2(0f, 0.5f);
        hf.pivot = new Vector2(0f, 0.5f);
        hf.anchoredPosition = new Vector2(PAD, 0f);
        hf.sizeDelta = new Vector2(BAR_W - PAD * 2f, BAR_H - PAD * 2f);

        var hpLabel = Text("HP", hpBg.transform, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(BAR_W,BAR_H), 17, TextAlignmentOptions.Center, Color.white);
        hpLabel.rectTransform.anchorMin = hpLabel.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
        hpLabel.fontStyle = FontStyles.Bold;
        hpLabel.text = "HEALTH";

        // Full-screen hurt flash (starts transparent).
        var flash = new GameObject("HurtFlash").AddComponent<Image>();
        flash.transform.SetParent(C, false);
        flash.color = new Color(0.7f,0f,0f,0f);
        flash.raycastTarget = false;
        var frt = flash.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;

        var hp = player.GetComponent<PlayerHealth>();
        if (hp != null) { hp.healthFill = hpFill; hp.hurtFlash = flash; }


        // Objective (top-center) + timer.
        gm.objectiveText = Text("Objective", C, new Vector2(0.5f,1), new Vector2(0,-30), new Vector2(1100,40), 26, TextAlignmentOptions.Center, new Color(0.85f,0.9f,1f));
        gm.objectiveText.rectTransform.anchorMin = gm.objectiveText.rectTransform.anchorMax = new Vector2(0.5f,1);
        gm.timerText = Text("Timer", C, new Vector2(0.5f,1), new Vector2(0,-72), new Vector2(400,40), 28, TextAlignmentOptions.Center, new Color(1f,0.85f,0.5f));
        gm.timerText.rectTransform.anchorMin = gm.timerText.rectTransform.anchorMax = new Vector2(0.5f,1);

        // Interaction prompt (bottom-center, hidden until near a resource).
        var promptGO = new GameObject("Prompt");
        promptGO.transform.SetParent(C, false);
        var promptBg = promptGO.AddComponent<Image>();
        promptBg.color = new Color(0f,0f,0f,0.55f);
        var prt = promptBg.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 90);
        prt.sizeDelta = new Vector2(560, 60);
        gm.promptText = Text("PromptText", promptGO.transform, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(540,54), 26, TextAlignmentOptions.Center, Color.white);
        gm.promptText.rectTransform.anchorMin = gm.promptText.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
        gm.promptRoot = promptGO;
        promptGO.SetActive(false);

        // Minimap (top-right).
        var mmGO = new GameObject("Minimap");
        mmGO.transform.SetParent(C, false);
        var raw = mmGO.AddComponent<RawImage>();
        raw.texture = minimapRT;
        var mrt = raw.rectTransform;
        mrt.anchorMin = mrt.anchorMax = new Vector2(1,1);
        mrt.pivot = new Vector2(1,1);
        mrt.anchoredPosition = new Vector2(-30,-30);
        mrt.sizeDelta = new Vector2(220,220);

        // Compass (below minimap): a rotating needle + heading text.
        var compGO = new GameObject("Compass");
        compGO.transform.SetParent(C, false);
        var compBg = compGO.AddComponent<Image>();
        compBg.color = new Color(0f,0f,0f,0.5f);
        var crt = compBg.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(1,1);
        crt.pivot = new Vector2(1,1);
        crt.anchoredPosition = new Vector2(-135,-270);
        crt.sizeDelta = new Vector2(90,90);

        var needleGO = new GameObject("Needle");
        needleGO.transform.SetParent(compGO.transform, false);
        var needle = needleGO.AddComponent<Image>();
        needle.color = new Color(1f,0.3f,0.3f);
        var nrt = needle.rectTransform;
        nrt.anchorMin = nrt.anchorMax = nrt.pivot = new Vector2(0.5f,0.5f);
        nrt.sizeDelta = new Vector2(8,60);

        var heading = Text("Heading", compGO.transform, new Vector2(0.5f,0f), new Vector2(0,-4), new Vector2(120,28), 20, TextAlignmentOptions.Center, Color.white);
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f,0f);
        heading.rectTransform.pivot = new Vector2(0.5f,1f);

        var compass = compGO.AddComponent<Compass>();
        compass.cameraTransform = cam;
        compass.needle = nrt;
        compass.headingText = heading;

        // End panel (hidden).
        var endGO = new GameObject("End Panel");
        endGO.transform.SetParent(C, false);
        var endBg = endGO.AddComponent<Image>();
        endBg.color = new Color(0f,0f,0f,0.8f);
        var ert = endBg.rectTransform;
        ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
        ert.offsetMin = ert.offsetMax = Vector2.zero;
        gm.endText = Text("EndText", endGO.transform, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(900,600), 30, TextAlignmentOptions.Center, Color.white);
        gm.endText.rectTransform.anchorMin = gm.endText.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
        gm.endPanel = endGO;
        endGO.SetActive(false);
    }

    static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchor, Vector2 pos,
                                Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.text = name;
        t.richText = true;
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        return t;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    static GameObject FindModel(string modelName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{modelName} t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path)
                    .Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    static GameObject SpawnAt(GameObject model, Terrain terrain, float x, float z,
                              Quaternion rot, Transform parent, float scale = 1f)
    {
        GameObject go = model != null
            ? (GameObject)Object.Instantiate(model)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, GroundY(terrain, x, z), z);
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * scale;
        ApplyColormap(go);
        return go;
    }

    static void ApplyColormap(GameObject go)
    {
        if (colormapMat == null) return;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[r.sharedMaterials.Length == 0 ? 1 : r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = colormapMat;
            r.sharedMaterials = mats;
        }
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif