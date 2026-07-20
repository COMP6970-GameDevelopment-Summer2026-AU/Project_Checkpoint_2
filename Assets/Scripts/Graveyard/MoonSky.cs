// MoonSky.cs — adds a world-fixed moon and a scatter of stars to the night sky.
// A sky root follows the camera's position (not rotation), so the moon and stars
// sit at fixed directions and feel infinitely far as the player moves. The moon
// billboards toward the camera; both use the fog-free GK/SkyUnlit shader and dim
// as the day/night cycle approaches dawn. If the shader is unavailable, it simply
// skips the billboards and leaves the skybox in place — no errors.

using UnityEngine;

public class MoonSky : MonoBehaviour
{
    public Transform cameraTransform;
    public DayNightCycle cycle;

    [Header("Moon")]
    public Vector3 moonDirection = new Vector3(0.3f, 0.6f, 1f);
    public float moonDistance = 680f;
    public float moonSize = 110f;
    public Color moonColor = new Color(0.95f, 0.96f, 1f);

    [Header("Stars")]
    public int starCount = 60;
    public float starDistance = 720f;

    Transform skyRoot;
    Transform moon;
    Material moonMat;
    Material starMat;
    bool ok;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (cycle == null) cycle = FindAnyObjectByType<DayNightCycle>();

        var shader = Shader.Find("GK/SkyUnlit");
        if (shader == null || cameraTransform == null) return;   // degrade gracefully

        skyRoot = new GameObject("SkyRoot").transform;

        // Moon
        moonMat = new Material(shader);
        moonMat.SetTexture("_BaseMap", BuildMoonTexture());
        moonMat.SetColor("_BaseColor", moonColor);
        moon = MakeQuad("Moon", moonMat, moonDirection.normalized * moonDistance, moonSize);

        // Stars (one quad each, fixed direction, facing the camera origin)
        starMat = new Material(shader);
        starMat.SetTexture("_BaseMap", BuildStarTexture());
        starMat.SetColor("_BaseColor", Color.white);
        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = RandomSkyDir();
            var s = MakeQuad("Star", starMat, dir * starDistance, Random.Range(3f, 7f));
            s.forward = -dir;   // face the center (camera)
        }

        ok = true;
    }

    void LateUpdate()
    {
        if (!ok) return;

        skyRoot.position = cameraTransform.position;

        // Billboard the moon toward the camera.
        if (moon != null)
            moon.rotation = Quaternion.LookRotation(moon.position - cameraTransform.position, Vector3.up);

        // Dim toward dawn (uses the light's brightness as a proxy).
        float dim = 1f;
        if (cycle != null)
        {
            var sun = cycle.GetComponent<Light>();
            if (sun != null) dim = Mathf.Clamp01(1.2f - sun.intensity);
        }
        if (moonMat != null) moonMat.SetColor("_BaseColor", moonColor * Mathf.Lerp(0.5f, 1f, dim));
        if (starMat != null) starMat.SetColor("_BaseColor", Color.white * dim);
    }

    Transform MakeQuad(string name, Material mat, Vector3 localPos, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(skyRoot, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Vector3 RandomSkyDir()
    {
        // Upper hemisphere, biased away from straight down.
        Vector3 d;
        do { d = Random.onUnitSphere; } while (d.y < 0.15f);
        return d;
    }

    Texture2D BuildMoonTexture()
    {
        int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var rng = new System.Random(7);
        Vector2 c = new Vector2(s / 2f, s / 2f);
        // A few soft crater centers.
        var craters = new Vector2[6];
        var cr = new float[6];
        for (int i = 0; i < 6; i++)
        {
            craters[i] = new Vector2(rng.Next(20, s - 20), rng.Next(20, s - 20));
            cr[i] = rng.Next(6, 16);
        }
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (s / 2f);
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.85f, 1f, d));   // soft disc edge
                float shade = 1f - 0.12f * d;                                    // limb darkening
                foreach (var (pos, rad) in Zip(craters, cr))
                {
                    float cd = Vector2.Distance(new Vector2(x, y), pos);
                    if (cd < rad) shade -= (1f - cd / rad) * 0.18f;
                }
                shade = Mathf.Clamp01(shade);
                tex.SetPixel(x, y, new Color(shade, shade, shade, a));
            }
        tex.Apply();
        return tex;
    }

    static System.Collections.Generic.IEnumerable<(Vector2, float)> Zip(Vector2[] a, float[] b)
    {
        for (int i = 0; i < a.Length; i++) yield return (a[i], b[i]);
    }

    Texture2D BuildStarTexture()
    {
        int s = 16;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(s / 2f, s / 2f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (s / 2f);
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }
}
