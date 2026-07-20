// Openable.cs — a coffin/door the player opens with E (interaction system:
// opening). Opening tilts the lid, releases a soul as a reward, and plays a sound.
// One-time use.

using System.Collections;
using UnityEngine;

public class Openable : MonoBehaviour, IInteractable
{
    public bool opened;

    public bool CanInteract => !opened;
    public string Prompt => "Press [E] to open the coffin";

    public void Interact()
    {
        if (opened) return;
        opened = true;
        AudioManager.PlayGate();
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        // Tilt the whole object as a simple "lid opening".
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(-35f, 0f, 0f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, t / 0.5f);
            yield return null;
        }
        Soul.Spawn(transform.position + Vector3.up * 0.5f);
        VFX.Burst(transform.position + Vector3.up * 0.8f, new Color(0.6f, 1f, 0.8f), 18);
    }
}
