// FlickerLight.cs — a warm point flame that flickers like a flame, added to fire
// baskets, lanterns, and lamp posts for atmosphere and visual polish.

using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    public float baseIntensity = 2.2f;
    public float amount = 0.6f;
    public float speed = 8f;

    Light flame;
    float seed;

    void Awake()
    {
        flame = GetComponent<Light>();
        flame.type = LightType.Point;
        flame.color = new Color(1f, 0.7f, 0.35f);
        flame.range = 9f;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed);
        flame.intensity = baseIntensity + (n - 0.5f) * 2f * amount;
    }
}
