// HazardZone.cs — a patch of cursed ground. It warns the player as they approach
// (via HazardWarning), turns its glow redder the closer they get, and drains
// health while they stand inside it.

using UnityEngine;

public class HazardZone : MonoBehaviour
{
    public float radius = 3f;
    public float damagePerSecond = 8f;
    public float warnRadius = 6.5f;   // player is warned within this range

    Light glow;
    Transform player;
    float seed;

    void Awake()
    {
        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;

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
        float flicker = 1.2f + Mathf.PerlinNoise(seed, Time.time * 3f) * 1.2f;

        if (GraveyardManager.Instance == null || !GraveyardManager.Instance.IsPlaying)
        {
            if (glow != null) glow.intensity = flicker;
            return;
        }

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        float t = 1f;   // 0 = far, 1 = inside
        if (player != null)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = player.position; b.y = 0f;
            float d = Vector3.Distance(a, b);

            if (d < warnRadius)
            {
                bool insideDamage = d < radius;
                HazardWarning.Report(insideDamage);
                t = Mathf.Clamp01(1f - (d - radius) / Mathf.Max(0.01f, warnRadius - radius));
            }
        }

        // Glow shifts green -> red as the player closes in.
        if (glow != null)
        {
            glow.color = Color.Lerp(new Color(0.4f, 1f, 0.4f), new Color(1f, 0.25f, 0.2f), t);
            glow.intensity = flicker * (1f + t);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var hp = other.GetComponent<PlayerHealth>();
        if (hp != null) hp.Drain(damagePerSecond * Time.deltaTime);
    }
}