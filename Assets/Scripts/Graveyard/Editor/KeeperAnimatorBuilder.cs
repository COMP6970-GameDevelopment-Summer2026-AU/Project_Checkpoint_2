// KeeperAnimatorBuilder.cs — Editor tool (Module 7.3).
// Scans the project for imported animation clips (e.g. Mixamo FBX), builds an
// Animator Controller with a locomotion blend tree (idle/walk/run on "Speed")
// plus Chop / Mine / Collect states triggered by "Harvest" + "HarvestType",
// then assigns it to the Player's Animator in the open scene.
//
// USAGE:  Tools ▸ Graveyard Keeper ▸ Build Keeper Animator
//
// Name your imported clips so they contain a recognizable word (case-insensitive):
//   idle · walk · run/jog/sprint · chop/axe/cut · mine/pick/mining · gather/collect/pickup
// Missing clips fall back to a sensible alternative so the controller always builds.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class KeeperAnimatorBuilder
{
    const string CONTROLLER_PATH = "Assets/Art/Player/KeeperAnimator.controller";

    [MenuItem("Tools/Graveyard Keeper/Build Keeper Animator")]
    public static void BuildAnimator()
    {
        var clips = GatherClips();
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("Keeper Animator",
                "No AnimationClips found in the project.\n\n" +
                "Import a rigged humanoid character and some Mixamo animations first " +
                "(set their Rig to Humanoid), then run this again.", "OK");
            return;
        }

        AnimationClip idle = Match(clips, "idle") ?? clips[0];
        AnimationClip walk = Match(clips, "walk") ?? idle;
        AnimationClip run  = Match(clips, "run", "jog", "sprint") ?? walk;
        AnimationClip chop = Match(clips, "chop", "axe", "cut", "swing") ?? Match(clips, "attack") ?? idle;
        AnimationClip mine = Match(clips, "mine", "mining", "pick", "hit") ?? chop;
        AnimationClip pick = Match(clips, "gather", "collect", "pickup", "pick up", "crouch") ?? chop;

        System.IO.Directory.CreateDirectory("Assets/Art/Player");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        controller.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        controller.AddParameter("Harvest",     AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HarvestType", AnimatorControllerParameterType.Int);

        var sm = controller.layers[0].stateMachine;

        // Locomotion blend tree (Idle 0 / Walk 0.6 / Run 1) on "Speed".
        var locoState = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 0.6f);
        tree.AddChild(run, 1f);
        sm.defaultState = locoState;

        // Harvest states.
        AddHarvestState(sm, locoState, "Chop",    chop, 0);
        AddHarvestState(sm, locoState, "Mine",    mine, 1);
        AddHarvestState(sm, locoState, "Collect", pick, 2);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        AssignToPlayer(controller);

        Debug.Log($"[KeeperAnimator] Built controller.\n" +
                  $"idle={Name(idle)} walk={Name(walk)} run={Name(run)}\n" +
                  $"chop={Name(chop)} mine={Name(mine)} collect={Name(pick)}");

        EditorUtility.DisplayDialog("Keeper Animator",
            "Animator Controller built and assigned to the Player.\n\n" +
            "Clips used:\n" +
            $"  Idle: {Name(idle)}\n  Walk: {Name(walk)}\n  Run: {Name(run)}\n" +
            $"  Chop (Wood): {Name(chop)}\n  Mine (Stone): {Name(mine)}\n  Collect (Pumpkin): {Name(pick)}\n\n" +
            "Press Play and harvest a resource to see it.", "OK");
    }

    static void AddHarvestState(AnimatorStateMachine sm, AnimatorState loco,
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
        exit.duration = 0.1f;
    }

    static void AssignToPlayer(AnimatorController controller)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[KeeperAnimator] No 'Player' object in the scene — assign the controller manually.");
            return;
        }

        var anim = player.GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogWarning("[KeeperAnimator] Player has no Animator yet. Add your rigged humanoid " +
                             "character under Player (it brings its own Animator + Avatar), then re-run.");
            return;
        }

        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;   // the CharacterController drives movement

        var ka = player.GetComponent<KeeperAnimator>();
        if (ka == null) ka = player.AddComponent<KeeperAnimator>();
        ka.animator = anim;

        EditorUtility.SetDirty(player);
    }

    // ── Clip discovery ──────────────────────────────────────────────────────────
    static List<AnimationClip> GatherClips()
    {
        var clips = new List<AnimationClip>();
        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                if (obj is AnimationClip c && !c.name.StartsWith("__preview"))
                    clips.Add(c);
            var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (direct != null && !direct.name.StartsWith("__preview") && !clips.Contains(direct))
                clips.Add(direct);
        }
        return clips.Distinct().ToList();
    }

    static AnimationClip Match(List<AnimationClip> clips, params string[] keys)
    {
        foreach (var c in clips)
        {
            string n = c.name.ToLowerInvariant();
            if (keys.Any(k => n.Contains(k))) return c;
        }
        return null;
    }

    static string Name(AnimationClip c) => c != null ? c.name : "(none)";
}
#endif
