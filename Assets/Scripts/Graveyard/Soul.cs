// Soul.cs — a glowing collectible dropped when a ghost is banished. It bobs, is
// drawn toward the player when near, and on contact adds to the soul count, heals
// a little, and pops with a particle + sound. Builds its own glowing visual, so
// no prefab/model is required. Create one with Soul.Spawn(position).

using UnityEngine;

public class Soul : MonoBehaviour
{
    public float attractRange = 4f;
    public float collectRange = 1.1f;
    public float moveSpeed = 6f;
    public float healAmount = 6f;

    Transform player;
    float baseY;

    public static void Spawn(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Soul";
        go.transform.position = pos + Vector3.up * 0.6f;
        go.transform.localScale = Vector3.one * 0.35f;
        Object.Destroy(go.GetComponent<Collider>());

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        Color soulColor = new Color(0.5f, 1f, 0.7f);
        mat.color = soulColor;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", soulColor * 2f);
        }
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = soulColor;
        light.range = 5f;
        light.intensity = 2f;

        go.AddComponent<Soul>();
    }

    void Start()
    {
        baseY = transform.position.y;
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);
        float bob = Mathf.Sin(Time.time * 3f) * 0.15f;

        if (player != null)
        {
            float d = Vector3.Distance(transform.position, player.position);
            if (d < collectRange) { Collect(); return; }
            if (d < attractRange)
            {
                Vector3 target = player.position + Vector3.up * 1f;
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                return;
            }
        }
        transform.position = new Vector3(transform.position.x, baseY + bob, transform.position.z);
    }

    void Collect()
    {
        GraveyardManager.Instance?.AddSoul(1);
        AudioManager.PlaySoul();
        VFX.Burst(transform.position, new Color(0.5f, 1f, 0.7f), 20, 0.2f, 4f);

        if (player != null)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null) hp.Heal(healAmount);
        }
        Destroy(gameObject);
    }
}
