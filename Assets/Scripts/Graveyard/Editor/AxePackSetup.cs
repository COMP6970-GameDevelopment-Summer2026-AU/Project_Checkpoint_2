// AxePackSetup.cs — one-click setup for the Pro Melee Axe Pack (fully automatic).
//
//   Tools ▸ Graveyard Keeper ▸ Setup Axe Pack Character (47 anims)
//
// It: (1) force-reimports the pack so the Humanoid settings apply, (2) swaps the
// player's mesh to "The Boss" and refits the CharacterController, (3) builds an
// Animator Controller with a locomotion blend tree (idle/walk/run), per-resource
// harvest attacks (Chop/Mine/Collect), AND a state for every one of the 47 clips,
// (4) assigns it, and (5) wires the AnimationShowcase + on-screen label so you can
// play all 47 with N / B / L. Run "Build World" first, then run this.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class AxePackSetup
{
    const string PACK_DIR = "Assets/Art/Player/AxePack";
    const string CHARACTER = PACK_DIR + "/The Boss.fbx";
    const string CONTROLLER_PATH = "Assets/Art/Player/BossAxe.controller";

    [MenuItem("Tools/Graveyard Keeper/Setup Axe Pack Character (47 anims)")]
    public static void Setup()
    {
        if (!Directory.Exists(PACK_DIR))
        {
            EditorUtility.DisplayDialog("Axe Pack",
                "Couldn't find " + PACK_DIR + ".\nImport the pack into that folder first.", "OK");
            return;
        }

        // 1) Force-reimport so the Humanoid postprocessor settings are applied.
        foreach (var path in FbxPaths())
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        // 2) Gather clips.
        var clips = GatherClips();
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("Axe Pack", "No animation clips found in the pack after import.", "OK");
            return;
        }

        // 3) Find the player in the open scene.
        var player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Axe Pack",
                "No 'Player' object in the scene.\nRun Tools ▸ Graveyard Keeper ▸ Build World first.", "OK");
            return;
        }

        // 4) Swap the mesh to The Boss.
        Animator bossAnim = SwapToBoss(player);
        if (bossAnim == null)
        {
            EditorUtility.DisplayDialog("Axe Pack",
                "Couldn't load/instantiate 'The Boss.fbx'. Check it imported as Humanoid.", "OK");
            return;
        }

        // 5) Build the controller (locomotion + harvest + all 47 states).
        var controller = BuildController(clips, out List<string> stateNames);
        bossAnim.runtimeAnimatorController = controller;
        bossAnim.applyRootMotion = false;

        // 6) Wire KeeperAnimator + ThirdPersonController.
        var tpc = player.GetComponent<ThirdPersonController>();
        var ka = player.GetComponent<KeeperAnimator>() ?? player.AddComponent<KeeperAnimator>();
        ka.animator = bossAnim;
        if (tpc != null) { tpc.keeperAnimator = ka; tpc.modelRoot = bossAnim.transform; }
        if (player.GetComponent<CombatController>() == null) player.AddComponent<CombatController>();
        var wm = player.GetComponent<WeaponManager>() ?? player.AddComponent<WeaponManager>();
        wm.customAxeModels = LoadWeapons("Axe", "Axe_Small", "Axe_Double", "Sword_Big", "Hammer_Double", "Scythe");
        wm.gripScale = 0.5f;
        wm.gripPosition = new Vector3(0f, 0f, 0.05f);
        wm.gripEuler = new Vector3(0f, 0f, 0f);

        // 7) Showcase + on-screen label.
        var label = BuildShowcaseLabel();
        var show = player.GetComponent<AnimationShowcase>() ?? player.AddComponent<AnimationShowcase>();
        show.animator = bossAnim;
        show.controller = tpc;
        show.label = label;
        show.stateNames = stateNames.ToArray();

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Axe Pack",
            $"Done!\n\nCharacter: The Boss\nAnimations wired: {stateNames.Count}/47\n\n" +
            "Play the scene:\n• WASD/Shift = walk/run (blended)\n• E = harvest (per-resource axe attack)\n" +
            "• Left-click = attack\n• TAB = open the axe-select menu (click an axe, or press 1-6)\n" +
            "• N / B = next / previous animation (cycles all 47)\n• L = back to movement", "OK");
    }

    // ── Mesh swap ───────────────────────────────────────────────────────────────
    static Animator SwapToBoss(GameObject player)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(CHARACTER);
        if (model == null) return null;

        // Remove old visual children (e.g. the Kenney Keeper_Mesh).
        for (int i = player.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(player.transform.GetChild(i).gameObject);

        var boss = (GameObject)Object.Instantiate(model);
        boss.name = "Boss_Mesh";
        boss.transform.SetParent(player.transform, false);
        boss.transform.localPosition = Vector3.zero;
        boss.transform.localRotation = Quaternion.identity;

        // Normalize to ~1.8 units tall regardless of the FBX's import scale.
        var b = GetBounds(boss);
        float h = Mathf.Max(0.3f, b.size.y);
        float scale = 1.8f / h;
        boss.transform.localScale = Vector3.one * scale;

        // Drop feet to the player origin.
        b = GetBounds(boss);
        boss.transform.localPosition -= new Vector3(0f, b.min.y - player.transform.position.y, 0f);

        // Refit the CharacterController.
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            b = GetBounds(boss);
            cc.height = Mathf.Max(1.2f, b.size.y);
            cc.center = new Vector3(0f, cc.height / 2f, 0f);
            cc.radius = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 0.35f, 0.2f, 0.5f);
        }

        var anim = boss.GetComponent<Animator>() ?? boss.GetComponentInChildren<Animator>();
        return anim;
    }

    // ── Controller ──────────────────────────────────────────────────────────────
    static AnimatorController BuildController(List<AnimationClip> clips, out List<string> stateNames)
    {
        Directory.CreateDirectory("Assets/Art/Player");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        controller.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        controller.AddParameter("Harvest",     AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HarvestType", AnimatorControllerParameterType.Int);
        controller.AddParameter("Attack",      AnimatorControllerParameterType.Trigger);
        controller.AddParameter("React",       AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        AnimationClip idle = Find(clips, "standing idle", "unarmed idle", "idle") ?? clips[0];
        AnimationClip walk = Find(clips, "standing walk forward", "unarmed walk forward", "walk forward", "walk") ?? idle;
        AnimationClip run  = Find(clips, "standing run forward", "unarmed run forward", "run forward", "run") ?? walk;

        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 0.6f);
        tree.AddChild(run, 1f);
        sm.defaultState = loco;

        AnimationClip chop = Find(clips, "melee attack downward", "melee combo attack ver. 1", "melee attack") ?? idle;
        AnimationClip mine = Find(clips, "melee attack horizontal", "melee attack backhand", "melee attack 360 low") ?? chop;
        AnimationClip pick = Find(clips, "crouch to standing idle", "crouch idle", "equip underarm") ?? idle;

        AddHarvest(sm, loco, "Chop", chop, 0);
        AddHarvest(sm, loco, "Mine", mine, 1);
        AddHarvest(sm, loco, "Collect", pick, 2);

        // Combat attack (left-click) — a combo swing triggered by "Attack".
        AnimationClip attack = Find(clips, "melee combo attack ver. 1", "melee attack horizontal",
                                    "melee attack backhand", "melee attack") ?? chop;
        var atkState = sm.AddState("Attack");
        atkState.motion = attack;
        var atkEnter = loco.AddTransition(atkState);
        atkEnter.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        atkEnter.hasExitTime = false;
        atkEnter.duration = 0.05f;
        var atkExit = atkState.AddTransition(loco);
        atkExit.hasExitTime = true;
        atkExit.exitTime = 0.8f;
        atkExit.duration = 0.12f;

        // Hit reaction (played when a ghost damages the player).
        AnimationClip react = Find(clips, "react large gut", "standing react large", "react") ?? attack;
        var reactState = sm.AddState("React");
        reactState.motion = react;
        var reEnter = loco.AddTransition(reactState);
        reEnter.AddCondition(AnimatorConditionMode.If, 0f, "React");
        reEnter.hasExitTime = false;
        reEnter.duration = 0.05f;
        var reExit = reactState.AddTransition(loco);
        reExit.hasExitTime = true;
        reExit.exitTime = 0.7f;
        reExit.duration = 0.12f;

        // A dedicated state for EVERY clip so the showcase can play all 47.
        stateNames = new List<string>();
        var reserved = new HashSet<string> { "Locomotion", "Chop", "Mine", "Collect", "Attack", "React" };
        foreach (var c in clips)
        {
            string name = c.name;
            if (reserved.Contains(name)) name += " (clip)";
            var st = sm.AddState(name);
            st.motion = c;
            stateNames.Add(name);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static void AddHarvest(AnimatorStateMachine sm, AnimatorState loco,
                           string name, AnimationClip clip, int typeValue)
    {
        var state = sm.AddState(name);
        state.motion = clip;

        var enter = loco.AddTransition(state);
        enter.AddCondition(AnimatorConditionMode.If, 0f, "Harvest");
        enter.AddCondition(AnimatorConditionMode.Equals, typeValue, "HarvestType");
        enter.hasExitTime = false;
        enter.duration = 0.05f;

        var exit = state.AddTransition(loco);
        exit.hasExitTime = true;
        exit.exitTime = 0.85f;
        exit.duration = 0.12f;
    }

    // ── Showcase label UI ───────────────────────────────────────────────────────
    static TextMeshProUGUI BuildShowcaseLabel()
    {
        var canvas = GameObject.Find("HUD Canvas");
        if (canvas == null) return null;

        var existing = canvas.transform.Find("ShowcaseLabel");
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

        var go = new GameObject("ShowcaseLabel");
        go.transform.SetParent(canvas.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 170f);
        rt.sizeDelta = new Vector2(700f, 120f);
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 26;
        t.color = new Color(1f, 0.9f, 0.6f);
        t.richText = true;
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        go.SetActive(false);
        return t;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    static GameObject[] LoadWeapons(params string[] names)
    {
        var list = new List<GameObject>();
        foreach (var n in names)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Weapons/{n}.fbx");
            if (go != null) list.Add(go);
        }
        return list.ToArray();
    }

    static IEnumerable<string> FbxPaths() =>
        Directory.GetFiles(PACK_DIR, "*.fbx", SearchOption.TopDirectoryOnly)
                 .Select(p => p.Replace('\\', '/'));

    static List<AnimationClip> GatherClips()
    {
        var list = new List<AnimationClip>();
        foreach (var path in FbxPaths())
        {
            if (path.EndsWith("The Boss.fbx")) continue;   // character, not a clip
            foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                if (obj is AnimationClip c && !c.name.StartsWith("__preview"))
                    list.Add(c);
        }
        return list.GroupBy(c => c.name).Select(g => g.First()).ToList();
    }

    static AnimationClip Find(List<AnimationClip> clips, params string[] keys)
    {
        foreach (var key in keys)
        {
            string k = key.ToLowerInvariant();
            var hit = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(k));
            if (hit != null) return hit;
        }
        return null;
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
#endif
