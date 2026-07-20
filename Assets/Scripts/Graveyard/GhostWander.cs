// GhostWander.cs — a roaming ghost with combat reactions.
// It drifts around, and when the player gets close it slowly floats toward them
// (light aggression). Getting hit by the player's axe makes it flash, get knocked
// back, and — after enough hits — be banished (rises, shrinks, and vanishes).

using System.Collections;
using UnityEngine;

public class GhostWander : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 1.5f;
    public float roamRadius = 12f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    [Header("Aggression")]
    public float aggroRadius = 11f;
    public float chaseSpeed = 2.2f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackDamage = 12f;
    public float attackCooldown = 1.5f;

    [Header("Combat")]
    public int hp = 2;
    public float knockback = 5f;

    Vector3 home, targetPoint, knockVel;
    float baseY, reactTimer, attackTimer;
    bool dead;

    Transform player;
    Renderer[] renderers;
    MaterialPropertyBlock mpb;
    static readonly int ID_Base = Shader.PropertyToID("_BaseColor");
    static readonly int ID_Std  = Shader.PropertyToID("_Color");

    void Start()
    {
        home = transform.position;
        baseY = transform.position.y;
        PickNewTarget();

        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        // A trigger collider so the player's swing can detect this ghost.
        if (GetComponent<Collider>() == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.8f;
            sc.center = Vector3.up * 0.2f;
        }
    }

    void Update()
    {
        if (dead) return;
        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying) return;

        // Knockback / react window.
        if (reactTimer > 0f)
        {
            reactTimer -= Time.deltaTime;
            transform.position += knockVel * Time.deltaTime;
            knockVel = Vector3.Lerp(knockVel, Vector3.zero, Time.deltaTime * 5f);
            return;
        }

        Vector3 flat = new Vector3(transform.position.x, home.y, transform.position.z);
        Vector3 dir;

        bool chasing = player != null &&
                       (player.position - flat).sqrMagnitude < aggroRadius * aggroRadius;

        if (chasing)
        {
            Vector3 to = player.position - flat; to.y = 0f;
            dir = to.normalized;

            // Attack when in range.
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;
            if (to.magnitude <= attackRange)
            {
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    var hpc = player.GetComponent<PlayerHealth>();
                    if (hpc != null) hpc.TakeDamage(attackDamage, dir);
                    StartCoroutine(Flash());
                }
            }
            else
            {
                flat += dir * chaseSpeed * Time.deltaTime;
            }
        }
        else
        {
            Vector3 to = targetPoint - flat;
            if (to.sqrMagnitude < 0.25f) { PickNewTarget(); return; }
            dir = to.normalized;
            flat += dir * speed * Time.deltaTime;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(flat.x, baseY + bob, flat.z);

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 3f * Time.deltaTime);
        }
    }

    public void Hit(Vector3 fromDir, int dmg)
    {
        if (dead) return;
        hp -= dmg;
        AudioManager.PlayGhostHit();
        StartCoroutine(Flash());

        knockVel = new Vector3(fromDir.x, 0f, fromDir.z).normalized * knockback;
        reactTimer = 0.35f;

        if (hp <= 0) StartCoroutine(Banish());
    }

    IEnumerator Flash()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.12f);
        SetColor(null);
    }

    void SetColor(Color? c)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            if (c.HasValue) { mpb.SetColor(ID_Base, c.Value); mpb.SetColor(ID_Std, c.Value); }
            else { mpb.SetColor(ID_Base, Color.white * 1f); mpb.SetColor(ID_Std, Color.white); }
            r.SetPropertyBlock(c.HasValue ? mpb : null);
        }
    }

    IEnumerator Banish()
    {
        dead = true;
        AudioManager.PlayGhostBanish();
        GraveyardManager.Instance?.BanishSpirit();
        VFX.Burst(transform.position, new Color(0.5f, 0.9f, 1f), 26, 0.25f, 4f);
        Soul.Spawn(transform.position);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Vector3 start = transform.position;
        Vector3 startScale = transform.localScale;
        float t = 0f, dur = 0.6f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.position = start + Vector3.up * (k * 2.5f);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, k);
            yield return null;
        }
        Destroy(gameObject);
    }

    void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * roamRadius;
        targetPoint = new Vector3(home.x + r.x, home.y, home.z + r.y);
    }
}
