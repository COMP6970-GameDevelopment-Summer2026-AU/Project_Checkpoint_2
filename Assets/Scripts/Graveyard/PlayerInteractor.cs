// PlayerInteractor.cs — finds the nearest IInteractable in range (harvestables,
// coffins, the escape gate), shows its prompt, and triggers it on E. Harvesting
// plays the per-resource axe animation; other interactions play a generic swing.

using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2.6f;
    public LayerMask mask = ~0;
    public float swingTime = 0.7f;

    ThirdPersonController controller;
    KeeperAnimator keeperAnimator;
    float swingTimer;

    void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        keeperAnimator = GetComponent<KeeperAnimator>();
    }

    void Update()
    {
        if (swingTimer > 0f) swingTimer -= Time.deltaTime;
        if (controller != null) controller.harvesting = swingTimer > 0f;

        var gm = GraveyardManager.Instance;
        if (gm == null || !gm.IsPlaying) { gm?.HidePrompt(); return; }

        IInteractable target = FindNearest();

        if (target != null)
        {
            gm.ShowPrompt(target.Prompt);

            if (swingTimer <= 0f && GKInput.InteractPressed() && target.CanInteract)
            {
                swingTimer = swingTime;
                if (target is Harvestable h)
                {
                    keeperAnimator?.TriggerHarvest(h.type);
                    VFX.Burst(((MonoBehaviour)target).transform.position + Vector3.up * 0.5f,
                              new Color(0.8f, 0.7f, 0.4f), 12, 0.15f, 2.5f);
                }
                else
                {
                    keeperAnimator?.TriggerHarvest(Harvestable.ResourceType.Pumpkin);
                }
                target.Interact();
            }
        }
        else gm.HidePrompt();
    }

    IInteractable FindNearest()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, interactRange, mask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var it = col.GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract) continue;

            var t = ((MonoBehaviour)it).transform;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
