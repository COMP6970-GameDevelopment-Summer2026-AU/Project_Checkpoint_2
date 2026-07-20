// EscapeGate.cs — the win objective. It stays LOCKED (red beacon) until the
// player meets the goal (harvest targets + enough souls), then UNLOCKS (green
// beacon). Standing near an unlocked gate and pressing E wins the game. A tall
// beacon of light marks it so it can be found in the endless world.

using UnityEngine;

public class EscapeGate : MonoBehaviour, IInteractable
{
    Light beacon;
    Renderer beam;
    Material beamMat;

    static readonly Color LockedColor = new Color(1f, 0.2f, 0.2f);
    static readonly Color OpenColor   = new Color(0.4f, 1f, 0.5f);

    void Awake()
    {
        // Beacon light
        var lightGO = new GameObject("Beacon");
        lightGO.transform.SetParent(transform, false);
        lightGO.transform.localPosition = Vector3.up * 3f;
        beacon = lightGO.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.range = 30f;
        beacon.intensity = 4f;

        // Beam of light (tall thin emissive cylinder)
        var beamGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beamGO.name = "Beam";
        Destroy(beamGO.GetComponent<Collider>());
        beamGO.transform.SetParent(transform, false);
        beamGO.transform.localScale = new Vector3(0.6f, 20f, 0.6f);
        beamGO.transform.localPosition = Vector3.up * 20f;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        beamMat = new Material(shader);
        if (beamMat.HasProperty("_EmissionColor")) beamMat.EnableKeyword("_EMISSION");
        beam = beamGO.GetComponent<Renderer>();
        beam.sharedMaterial = beamMat;

        // Trigger area for proximity (interaction handled by PlayerInteractor collider check)
        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 3f;
    }

    void Update()
    {
        bool open = CanInteract;
        Color c = open ? OpenColor : LockedColor;
        float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 2f);
        if (beacon != null) { beacon.color = c; beacon.intensity = (open ? 5f : 3f) * pulse; }
        if (beamMat != null)
        {
            beamMat.color = c;
            if (beamMat.HasProperty("_EmissionColor")) beamMat.SetColor("_EmissionColor", c * pulse * 2f);
        }
    }

    public bool CanInteract =>
        GraveyardManager.Instance != null && GraveyardManager.Instance.IsGoalMet;

    public string Prompt => CanInteract
        ? "Press [E] to ESCAPE the graveyard!"
        : "The gate is sealed — finish your tasks first";

    public void Interact()
    {
        if (!CanInteract) return;
        AudioManager.PlayGate();
        GraveyardManager.Instance?.Win();
    }
}
