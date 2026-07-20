// Harvestable.cs — a resource node the player can harvest (Module 7).
// Three resource types, each with its own verb, sound, and yield. The node takes
// a few "hits", does a small scale-punch per hit as feedback (a stand-in for the
// per-type harvest animations from M7.3), then depletes and grants resources.

using System.Collections;
using UnityEngine;

public class Harvestable : MonoBehaviour, IInteractable
{
    public enum ResourceType { Wood, Stone, Pumpkin }

    [Header("Resource")]
    public ResourceType type = ResourceType.Wood;
    public int hitsToHarvest = 3;
    public int yieldAmount = 1;

    [Header("Feedback")]
    public float punchScale = 0.12f;

    int hitsRemaining;
    Vector3 baseScale;
    bool depleted;
    Coroutine punchRoutine;

    void Awake()
    {
        hitsRemaining = Mathf.Max(1, hitsToHarvest);
        baseScale = transform.localScale;
    }

    public string Verb()
    {
        switch (type)
        {
            case ResourceType.Wood:    return "Chop";
            case ResourceType.Stone:   return "Mine";
            default:                   return "Collect";
        }
    }

    public string DisplayName() => type.ToString();

    // IInteractable
    public bool CanInteract => !depleted;
    public string Prompt => $"Press [E] to {Verb()} {DisplayName()}";
    public void Interact() => Harvest();

    // Called by PlayerInteractor when the player presses interact nearby.
    // Returns true when the node is fully harvested this hit.
    public bool Harvest()
    {
        if (depleted) return false;

        hitsRemaining--;
        AudioManager.PlayHarvest(type);

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(Punch());

        if (hitsRemaining <= 0)
        {
            depleted = true;
            GraveyardManager.Instance?.AddResource(type, yieldAmount);
            AudioManager.PlayCollected();
            StartCoroutine(SinkAndRemove());
            return true;
        }
        return false;
    }

    public bool IsDepleted => depleted;

    IEnumerator Punch()
    {
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t / dur * Mathf.PI) * punchScale;
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    IEnumerator SinkAndRemove()
    {
        // Sink into the ground and shrink, then destroy.
        float t = 0f, dur = 0.5f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.down * 1.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.position = Vector3.Lerp(start, end, k);
            transform.localScale = Vector3.Lerp(baseScale, baseScale * 0.2f, k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
