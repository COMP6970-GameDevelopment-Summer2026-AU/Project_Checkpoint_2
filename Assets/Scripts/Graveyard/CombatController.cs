// CombatController.cs — melee combat. Left-click swings the axe: it plays an
// attack animation, briefly locks movement, and (at the impact moment) hits any
// ghosts in a forward arc so they react — flash, get knocked back, and are
// banished after enough hits.

using System.Collections;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Swing")]
    public float cooldown = 0.65f;
    public float impactDelay = 0.25f;
    public int damage = 1;

    [Header("Hit shape")]
    public float reach = 2.8f;
    public float radius = 2.2f;
    [Range(-1f, 1f)] public float arcDot = 0.25f;   // ~75° cone in front

    ThirdPersonController tpc;
    KeeperAnimator anim;
    float lockTimer;

    void Awake()
    {
        tpc = GetComponent<ThirdPersonController>();
        anim = GetComponent<KeeperAnimator>();
    }

    void Update()
    {
        if (lockTimer > 0f) lockTimer -= Time.deltaTime;
        if (tpc != null) tpc.attacking = lockTimer > 0f;

        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying) return;
        if (tpc != null && tpc.showcasing) return;

        if (lockTimer <= 0f && GKInput.AttackPressed())
        {
            lockTimer = cooldown;
            anim?.TriggerAttack();
            AudioManager.PlaySwing();
            StartCoroutine(Impact());
        }
    }

    IEnumerator Impact()
    {
        yield return new WaitForSeconds(impactDelay);

        Vector3 fwd = (tpc != null && tpc.modelRoot != null) ? tpc.modelRoot.forward : transform.forward;
        fwd.y = 0f; fwd.Normalize();
        Vector3 origin = transform.position + Vector3.up * 1f + fwd * (reach * 0.5f);

        foreach (var col in Physics.OverlapSphere(origin, radius))
        {
            var ghost = col.GetComponentInParent<GhostWander>();
            if (ghost == null) continue;

            Vector3 to = ghost.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f && Vector3.Dot(fwd, to.normalized) < arcDot) continue;

            ghost.Hit(fwd, damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.5f);
        Vector3 fwd = transform.forward;
        Gizmos.DrawWireSphere(transform.position + Vector3.up + fwd * (reach * 0.5f), radius);
    }
}
