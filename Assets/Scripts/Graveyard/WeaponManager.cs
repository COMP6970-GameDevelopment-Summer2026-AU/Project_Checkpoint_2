// WeaponManager.cs — gives the character selectable, visible weapons.
//
// The Pro Melee Axe Pack contains animations only (no weapon mesh), so this
// attaches a real weapon model (or a procedural axe) to the character's right
// hand so it shows during the animations. Press TAB to open a selection row along
// the bottom of the screen; click a weapon (or press 1-6) to equip it.
//
// Each weapon's true size is measured ONCE at build time (active, unparented), so
// the equipped and menu sizes are computed deterministically regardless of the
// model's import scale or the character's bone scale. Tune with handTargetSize,
// menuTargetSize, gripPosition, gripEuler.

using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [System.Serializable]
    public struct AxeDef
    {
        public string name;
        public Color handle;
        public Color blade;
        public Vector3 bladeSize;
        public bool doubleSided;
        public bool glow;
    }

    [Header("Optional: real weapon models (else procedural axes are used)")]
    public GameObject[] customAxeModels;

    [Header("Sizing (world units)")]
    public float handTargetSize = 1.2f;   // weapon length when held
    public float menuTargetSize = 0.32f;  // weapon size in the selection row

    [Header("Grip in the hand (tune to taste)")]
    public Vector3 gripPosition = new Vector3(0f, 0.02f, 0.03f);
    public Vector3 gripEuler = new Vector3(0f, 0f, 0f);
    public float gripScale = 1f;

    AxeDef[] defs;
    GameObject[] axes;
    float[] unitSizes;      // world max-dimension per 1.0 localScale (at lossyScale 1)
    Transform hand;
    Transform stash;
    CameraRig rig;
    ThirdPersonController tpc;
    Camera cam;
    int selected;
    bool menuOpen;

    void Start()
    {
        cam = Camera.main;
        rig = cam != null ? cam.GetComponent<CameraRig>() : null;
        tpc = GetComponent<ThirdPersonController>();

        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
            hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null) hand = tpc != null && tpc.modelRoot != null ? tpc.modelRoot : transform;

        stash = new GameObject("AxeStash").transform;
        stash.SetParent(transform);

        defs = DefaultAxes();
        BuildAxes();
        Equip(0);

        Debug.Log($"[WeaponManager] Ready. axes={(axes != null ? axes.Length : 0)}, " +
                  $"hand={(hand != null ? hand.name : "NULL")} (lossy={(hand != null ? hand.lossyScale.x : 0f):0.###}), " +
                  $"camera={(cam != null)}. Press TAB.");
    }

    void Update()
    {
        if (axes == null || axes.Length == 0) return;
        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying) return;

        if (GKInput.WeaponMenuPressed())
        {
            if (menuOpen) CloseMenu(selected); else OpenMenu();
        }

        int n = GKInput.NumberKeyPressed();
        if (n >= 1 && n <= axes.Length)
        {
            if (menuOpen) CloseMenu(n - 1); else Equip(n - 1);
        }

        if (menuOpen)
        {
            foreach (var a in axes) if (a) a.transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);

            if (GKInput.PointerPressed() && cam != null)
            {
                Ray ray = cam.ScreenPointToRay(GKInput.PointerPosition());
                if (Physics.Raycast(ray, out RaycastHit hit, 12f))
                {
                    int idx = System.Array.IndexOf(axes, hit.collider.gameObject);
                    if (idx < 0 && hit.collider.transform.parent != null)
                        idx = System.Array.IndexOf(axes, hit.collider.transform.parent.gameObject);
                    if (idx < 0)
                        idx = System.Array.IndexOf(axes, hit.collider.transform.root.gameObject);
                    if (idx >= 0) CloseMenu(idx);
                }
            }

            if (GKInput.UnlockPressed()) CloseMenu(selected);
        }
    }

    // localScale needed so the weapon's world size == target, under a parent of scale `parentLossy`.
    float LocalScaleFor(int i, float target, float parentLossy)
    {
        float unit = (unitSizes != null && i < unitSizes.Length && unitSizes[i] > 0.0001f) ? unitSizes[i] : 1f;
        float pl = Mathf.Abs(parentLossy); if (pl < 0.0001f) pl = 1f;
        return target / (pl * unit);
    }

    public void Equip(int index)
    {
        selected = Mathf.Clamp(index, 0, axes.Length - 1);
        float handLossy = hand != null ? hand.lossyScale.x : 1f;
        for (int i = 0; i < axes.Length; i++)
        {
            if (axes[i] == null) continue;
            if (i == selected)
            {
                axes[i].SetActive(true);
                axes[i].transform.SetParent(hand, false);
                axes[i].transform.localPosition = gripPosition;
                axes[i].transform.localEulerAngles = gripEuler;
                axes[i].transform.localScale = Vector3.one * LocalScaleFor(i, handTargetSize * gripScale, handLossy);
                SetColliders(axes[i], false);
            }
            else
            {
                axes[i].transform.SetParent(stash, false);
                axes[i].SetActive(false);
            }
        }
    }

    void OpenMenu()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[WeaponManager] No Main Camera found."); return; }
        if (rig == null) rig = cam.GetComponent<CameraRig>();

        menuOpen = true;
        if (rig != null) rig.enabled = false;
        if (tpc != null) tpc.showcasing = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float camLossy = cam.transform.lossyScale.x;
        int count = axes.Length;
        for (int i = 0; i < count; i++)
        {
            if (axes[i] == null) continue;
            axes[i].SetActive(true);
            axes[i].transform.SetParent(cam.transform, false);
            float x = (i - (count - 1) / 2f) * 0.33f;
            axes[i].transform.localPosition = new Vector3(x, -0.42f, 1.3f);
            axes[i].transform.localEulerAngles = new Vector3(18f, 0f, 0f);
            axes[i].transform.localScale = Vector3.one * LocalScaleFor(i, menuTargetSize, camLossy);
            SetColliders(axes[i], true);
        }
        Debug.Log($"[WeaponManager] Menu opened with {count} weapons.");
    }

    void CloseMenu(int index)
    {
        menuOpen = false;
        Equip(index);
        if (rig != null) rig.enabled = true;
        if (tpc != null) tpc.showcasing = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        AudioManager.PlayClick();
    }

    // ── Building the weapons ────────────────────────────────────────────────────
    void BuildAxes()
    {
        bool useCustom = customAxeModels != null && customAxeModels.Length > 0;
        int count = useCustom ? customAxeModels.Length : DefaultAxes().Length;
        axes = new GameObject[count];
        unitSizes = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (useCustom)
            {
                if (customAxeModels[i] == null) axes[i] = BuildAxe(DefaultAxes()[0], i);
                else
                {
                    axes[i] = Instantiate(customAxeModels[i]);
                    axes[i].transform.SetParent(null);
                    axes[i].transform.position = Vector3.zero;
                    axes[i].transform.rotation = Quaternion.identity;
                    Recolor(axes[i]);
                    FitCollider(axes[i]);
                }
            }
            else axes[i] = BuildAxe(defs[i], i);

            // Measure true size now (active, unparented, at import scale).
            axes[i].SetActive(true);
            float maxDim = MaxWorldDim(axes[i]);
            float os = Mathf.Abs(axes[i].transform.localScale.x); if (os < 0.0001f) os = 1f;
            unitSizes[i] = Mathf.Max(0.0001f, maxDim / os);
        }
    }

    float MaxWorldDim(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 1f;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
    }

    GameObject BuildAxe(AxeDef d, int idx)
    {
        var root = new GameObject($"Axe_{idx}_{d.name}");
        MakePart(PrimitiveType.Cylinder, root.transform,
            new Vector3(0.035f, 0.32f, 0.035f), new Vector3(0f, 0f, 0f), d.handle, false);
        MakePart(PrimitiveType.Cube, root.transform,
            d.bladeSize, new Vector3(0f, 0.6f, 0.07f), d.blade, d.glow);
        if (d.doubleSided)
            MakePart(PrimitiveType.Cube, root.transform,
                d.bladeSize, new Vector3(0f, 0.6f, -0.07f), d.blade, d.glow);

        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.4f, 0f);
        box.size = new Vector3(0.5f, 1.2f, 0.5f);
        box.enabled = false;
        return root;
    }

    GameObject MakePart(PrimitiveType type, Transform parent, Vector3 scale, Vector3 pos, Color color, bool glow)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.transform.localPosition = pos;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        if (glow && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
        }
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    void SetColliders(GameObject axe, bool on)
    {
        foreach (var c in axe.GetComponentsInChildren<Collider>(true)) c.enabled = on;
    }

    void FitCollider(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
        var rs = go.GetComponentsInChildren<Renderer>();
        var box = go.AddComponent<BoxCollider>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = go.transform.InverseTransformVector(b.size);
        }
        else box.size = Vector3.one * 0.5f;
        box.enabled = false;
    }

    void Recolor(GameObject go)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var src = r.sharedMaterials;
            var mats = new Material[src.Length == 0 ? 1 : src.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                string name = (i < src.Length && src[i] != null) ? src[i].name : "";
                var m = new Material(shader) { color = MaterialColor(name) };
                bool metal = name.ToLowerInvariant().Contains("steel") || name.ToLowerInvariant().Contains("gold");
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal ? 0.7f : 0.1f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", metal ? 0.6f : 0.3f);
                mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
    }

    static Color MaterialColor(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("lightwood")) return new Color(0.45f, 0.26f, 0.15f);
        if (n.Contains("darkwood") || n.Contains("wood")) return new Color(0.32f, 0.18f, 0.10f);
        if (n.Contains("darkbrown") || n.Contains("brown") || n.Contains("leather")) return new Color(0.28f, 0.20f, 0.16f);
        if (n.Contains("lightsteel")) return new Color(0.64f, 0.67f, 0.72f);
        if (n.Contains("darksteel")) return new Color(0.45f, 0.45f, 0.40f);
        if (n.Contains("steel") || n.Contains("metal") || n.Contains("iron")) return new Color(0.55f, 0.58f, 0.62f);
        if (n.Contains("lightgold")) return new Color(0.92f, 0.80f, 0.36f);
        if (n.Contains("gold")) return new Color(0.82f, 0.67f, 0.28f);
        if (n.Contains("lightred")) return new Color(0.72f, 0.26f, 0.22f);
        if (n.Contains("red")) return new Color(0.60f, 0.12f, 0.14f);
        if (n.Contains("black")) return new Color(0.09f, 0.09f, 0.10f);
        return new Color(0.6f, 0.6f, 0.62f);
    }

    AxeDef[] DefaultAxes() => new[]
    {
        new AxeDef { name = "Woodsman",  handle = new Color(0.4f,0.26f,0.15f), blade = new Color(0.7f,0.72f,0.75f), bladeSize = new Vector3(0.06f,0.16f,0.24f) },
        new AxeDef { name = "Battle Axe",handle = new Color(0.25f,0.18f,0.12f), blade = new Color(0.6f,0.62f,0.68f), bladeSize = new Vector3(0.06f,0.24f,0.34f), doubleSided = true },
        new AxeDef { name = "Bearded",   handle = new Color(0.35f,0.22f,0.14f), blade = new Color(0.55f,0.57f,0.62f), bladeSize = new Vector3(0.06f,0.30f,0.22f) },
        new AxeDef { name = "Golden Axe",handle = new Color(0.3f,0.2f,0.1f),   blade = new Color(1f,0.82f,0.2f),   bladeSize = new Vector3(0.06f,0.2f,0.3f), glow = true },
        new AxeDef { name = "Bloodaxe",  handle = new Color(0.15f,0.1f,0.08f), blade = new Color(0.8f,0.1f,0.1f),   bladeSize = new Vector3(0.06f,0.22f,0.3f), glow = true },
        new AxeDef { name = "Cursed Axe",handle = new Color(0.12f,0.14f,0.12f),blade = new Color(0.4f,1f,0.5f),     bladeSize = new Vector3(0.06f,0.24f,0.32f), doubleSided = true, glow = true },
    };
}