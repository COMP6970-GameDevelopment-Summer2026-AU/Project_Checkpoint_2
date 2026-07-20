// PlayerHealth.cs — player stat + lose condition. Ghosts damage the player; at
// zero health the night ends in defeat. Collecting souls heals a little. Shows a
// health bar and a red hurt flash, and plays a hit-react animation.

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float invulnTime = 0.6f;

    [Header("UI (assigned by the builder)")]
    public Image healthFill;
    public Image hurtFlash;
    public Color fullColor = new Color(0.35f, 0.85f, 0.35f);
    public Color lowColor = new Color(0.90f, 0.20f, 0.20f);

    float health;
    float invuln;
    bool dead;
    float fullWidth = -1f;
    KeeperAnimator anim;

    public float Health01 => Mathf.Clamp01(health / maxHealth);

    void Awake()
    {
        health = maxHealth;
        anim = GetComponent<KeeperAnimator>();
    }

    void Start()
    {
        if (healthFill != null) fullWidth = healthFill.rectTransform.sizeDelta.x;
        UpdateBar();
    }

    void Update()
    {
        if (invuln > 0f) invuln -= Time.deltaTime;
        if (hurtFlash != null && hurtFlash.color.a > 0f)
        {
            var c = hurtFlash.color;
            c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 1.4f);
            hurtFlash.color = c;
        }
    }

    public void TakeDamage(float amount, Vector3 fromDir)
    {
        if (dead || invuln > 0f) return;
        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying) return;

        health -= amount;
        invuln = invulnTime;
        UpdateBar();
        AudioManager.PlayHurt();
        anim?.TriggerReact();

        if (hurtFlash != null) hurtFlash.color = new Color(0.7f, 0f, 0f, 0.45f);

        if (health <= 0f)
        {
            dead = true;
            GraveyardManager.Instance?.Lose();
        }
    }

    public void Heal(float amount)
    {
        if (dead) return;
        health = Mathf.Min(maxHealth, health + amount);
        UpdateBar();
    }

    // Continuous damage that ignores the hit-invulnerability window (hazards).
    public void Drain(float amount)
    {
        if (dead) return;
        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying) return;
        health -= amount;
        UpdateBar();
        if (hurtFlash != null && hurtFlash.color.a < 0.2f)
            hurtFlash.color = new Color(0.4f, 0.6f, 0.1f, 0.25f);
        if (health <= 0f) { dead = true; GraveyardManager.Instance?.Lose(); }
    }

    void UpdateBar()
    {
        if (healthFill == null) return;
        var rt = healthFill.rectTransform;
        if (fullWidth < 0f) fullWidth = rt.sizeDelta.x;   // capture if not set yet
        float h01 = Health01;
        rt.sizeDelta = new Vector2(fullWidth * h01, rt.sizeDelta.y);
        healthFill.color = Color.Lerp(lowColor, fullColor, h01);
    }
}