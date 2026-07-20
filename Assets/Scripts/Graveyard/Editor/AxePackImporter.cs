// AxePackImporter.cs — automatic import settings for the Pro Melee Axe Pack.
// Any FBX placed under Assets/Art/Player/AxePack/ is imported as a HUMANOID rig
// (so the 47 clips retarget onto the character), each clip is renamed to its file
// name (Mixamo FBX otherwise import as "mixamo.com"), and locomotion/idle clips
// are set to loop. This means you just drop the pack in — no per-file clicking.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;

public class AxePackImporter : AssetPostprocessor
{
    static bool InPack(string path) =>
        path.Replace('\\', '/').Contains("/Art/Player/AxePack/") &&
        path.ToLowerInvariant().EndsWith(".fbx");

    void OnPreprocessModel()
    {
        var mi = assetImporter as ModelImporter;
        if (mi == null || !InPack(mi.assetPath)) return;

        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = true;
    }

    void OnPreprocessAnimation()
    {
        var mi = assetImporter as ModelImporter;
        if (mi == null || !InPack(mi.assetPath)) return;

        var clips = mi.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;

        string fileName = Path.GetFileNameWithoutExtension(mi.assetPath);
        string ln = fileName.ToLowerInvariant();
        bool loop = ln.Contains("idle") || ln.Contains("walk") || ln.Contains("run");

        clips[0].name = fileName;          // distinct, human-readable clip name
        clips[0].loopTime = loop;
        mi.clipAnimations = clips;
    }
}
#endif
