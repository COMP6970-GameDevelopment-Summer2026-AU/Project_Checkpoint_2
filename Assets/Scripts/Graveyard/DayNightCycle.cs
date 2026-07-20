// DayNightCycle.cs — Module 6.3. Rotates the directional light across the night
// and eases fog/ambient colors from deep night toward pre-dawn over the cycle,
// so the sky and lighting change while the player harvests.

using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    [Tooltip("Seconds for a full sweep of the light across the sky.")]
    public float cycleSeconds = 180f;

    public Gradient lightColor;
    public Color nightFog = new Color(0.05f, 0.06f, 0.12f);
    public Color dawnFog  = new Color(0.35f, 0.32f, 0.42f);
    public float startAngle = 200f;   // low on the horizon (night)

    Light sun;
    float t;

    void Awake()
    {
        sun = GetComponent<Light>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;

        if (lightColor == null || lightColor.colorKeys.Length == 0)
            lightColor = DefaultGradient();
    }

    void Update()
    {
        t += Time.deltaTime / Mathf.Max(1f, cycleSeconds);
        float k = Mathf.Clamp01(t);

        // Sweep the light 120 degrees across the sky over the night.
        float ang = startAngle + k * 120f;
        transform.rotation = Quaternion.Euler(ang, -30f, 0f);

        Color c = lightColor.Evaluate(k);
        sun.color = c;
        sun.intensity = Mathf.Lerp(0.4f, 0.9f, k);

        Color fog = Color.Lerp(nightFog, dawnFog, k);
        RenderSettings.fogColor = fog;
        RenderSettings.ambientLight = Color.Lerp(nightFog * 1.5f, dawnFog * 1.3f, k);
    }

    static Gradient DefaultGradient()
    {
        var g = new Gradient();
        g.colorKeys = new[]
        {
            new GradientColorKey(new Color(0.35f, 0.4f, 0.7f), 0f),   // moonlight
            new GradientColorKey(new Color(0.6f, 0.5f, 0.6f), 0.6f),
            new GradientColorKey(new Color(1f, 0.75f, 0.55f), 1f),    // dawn warmth
        };
        g.alphaKeys = new[]
        {
            new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
        };
        return g;
    }
}
