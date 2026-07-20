// KeeperAnimator.cs — drives a Humanoid Animator for the keeper.
// Feeds a "Speed" float for the locomotion blend tree (idle/walk/run) and fires
// a "Harvest" trigger with a "HarvestType" int so each resource plays a different
// animation (Module 7.3). Every call is guarded: if the Animator or its
// parameters/controller aren't set up yet, the methods simply no-op, so the game
// still runs while you're still importing Mixamo clips.

using UnityEngine;

public class KeeperAnimator : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Smoothing for the locomotion Speed parameter.")]
    public float moveDamp = 0.1f;

    static readonly int P_Speed   = Animator.StringToHash("Speed");
    static readonly int P_Harvest = Animator.StringToHash("Harvest");
    static readonly int P_Type    = Animator.StringToHash("HarvestType");
    static readonly int P_Attack  = Animator.StringToHash("Attack");
    static readonly int P_React   = Animator.StringToHash("React");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    bool Ready => animator != null && animator.runtimeAnimatorController != null;

    bool Has(int hash, AnimatorControllerParameterType type)
    {
        if (!Ready) return false;
        foreach (var p in animator.parameters)
            if (p.nameHash == hash && p.type == type) return true;
        return false;
    }

    public void SetMoveSpeed(float value01)
    {
        if (Has(P_Speed, AnimatorControllerParameterType.Float))
            animator.SetFloat(P_Speed, Mathf.Clamp01(value01), moveDamp, Time.deltaTime);
    }

    public void TriggerHarvest(Harvestable.ResourceType type)
    {
        if (!Ready) return;
        if (Has(P_Type, AnimatorControllerParameterType.Int))
            animator.SetInteger(P_Type, (int)type);          // Wood=0, Stone=1, Pumpkin=2
        if (Has(P_Harvest, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(P_Harvest);
    }

    public void TriggerAttack()
    {
        if (Has(P_Attack, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(P_Attack);
    }

    public void TriggerReact()
    {
        if (Has(P_React, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(P_React);
    }
}
