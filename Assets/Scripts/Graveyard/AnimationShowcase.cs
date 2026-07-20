// AnimationShowcase.cs — lets you play EVERY one of the 47 axe-pack animations
// in-game. Press N / B to step forward/back through the full list, and L to
// return to normal movement. The current clip name and index show on screen.
// Movement is frozen while showcasing so the animation plays cleanly.

using UnityEngine;
using TMPro;

public class AnimationShowcase : MonoBehaviour
{
    public Animator animator;
    public ThirdPersonController controller;   // frozen while showcasing
    public TextMeshProUGUI label;              // optional on-screen readout
    public string[] stateNames;                // all 47 animator state names
    public float crossFade = 0.15f;

    int index = -1;
    bool active;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (controller == null) controller = GetComponent<ThirdPersonController>();
        if (label != null) label.gameObject.SetActive(false);
    }

    void Update()
    {
        if (stateNames == null || stateNames.Length == 0 || animator == null) return;

        if (GKInput.NextAnimPressed()) Step(+1);
        else if (GKInput.PrevAnimPressed()) Step(-1);
        else if (GKInput.LocomotionPressed()) Resume();
    }

    void Step(int dir)
    {
        if (animator == null || animator.runtimeAnimatorController == null || animator.layerCount == 0)
        {
            Debug.LogWarning("[AnimationShowcase] No animator controller assigned — run Setup Axe Pack Character.");
            return;
        }

        index = (index + dir + stateNames.Length) % stateNames.Length;

        int hash = Animator.StringToHash(stateNames[index]);
        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"[AnimationShowcase] State not found: {stateNames[index]}");
            return;
        }

        active = true;
        if (controller != null) controller.showcasing = true;    // hold still to show the clip

        animator.CrossFadeInFixedTime(hash, crossFade, 0);

        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text =
                $"<b>Animation {index + 1}/{stateNames.Length}</b>\n{stateNames[index]}\n" +
                "<size=70%>N next  ·  B previous  ·  L resume</size>";
        }
    }

    void Resume()
    {
        if (!active) return;
        active = false;
        if (controller != null) controller.showcasing = false;
        if (animator != null && animator.runtimeAnimatorController != null &&
            animator.HasState(0, Animator.StringToHash("Locomotion")))
            animator.CrossFadeInFixedTime("Locomotion", 0.2f);
        if (label != null) label.gameObject.SetActive(false);
    }
}