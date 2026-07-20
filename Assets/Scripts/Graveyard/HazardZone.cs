// HazardZone.cs — a patch of cursed ground. While the player stands in it, it
// drains health over time (an environmental-danger challenge). Shown as a soft
// green glow (a point light) rather than blocky particles.

using UnityEngine;

public class HazardZone : MonoBehaviour
{
    public float radius = 3f;
    public float damagePerSecond = 8f;

    Light glow;
    float seed;

    void Awake()
    {
        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;

        // Soft green danger glow (no blocky particles).
        var lightGO = new GameObject("CurseGlow");
        lightGO.transform.SetParent(transform, false);
        lightGO.transform.localPosition = Vector3.up * 0.6f;
        glow = lightGO.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.4f, 1f, 0.4f);
        glow.range = radius * 2.5f;
        glow.intensity = 1.6f;
        seed = Random.value * 100f;
    }

    void Update()
    {
        if (glow != null)
            glow.intensity = 1.2f + Mathf.PerlinNoise(seed, Time.time * 3f) * 1.2f;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var hp = other.GetComponent<PlayerHealth>();
        if (hp != null) hp.Drain(damagePerSecond * Time.deltaTime);
    }
}
