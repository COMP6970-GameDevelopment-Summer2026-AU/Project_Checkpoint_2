// ThirdPersonController.cs — camera-relative movement for the graveyard keeper.
// Uses a CharacterController for solid collision. Moves relative to the camera,
// rotates the model to face the movement direction, supports sprint and gravity.
// No animation rig required (Kenney models are static); hooks are noted for
// plugging in Mixamo animations later.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 8f;
    public float turnSmoothTime = 0.08f;
    public float gravity = -20f;

    [Header("References")]
    public Transform cameraTransform;   // set by CameraRig / builder
    public Transform modelRoot;         // the visible keeper mesh (rotated to face movement)
    public KeeperAnimator keeperAnimator;  // optional; drives locomotion + harvest anims

    [HideInInspector] public bool harvesting;   // set by PlayerInteractor during a swing
    [HideInInspector] public bool showcasing;   // set by AnimationShowcase while cycling clips
    [HideInInspector] public bool attacking;    // set by CombatController during a melee swing
    public bool Busy => harvesting || showcasing || attacking;

    CharacterController cc;
    float turnVelocity;
    float verticalVel;

    public bool IsMoving { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (modelRoot == null) modelRoot = transform;
        if (keeperAnimator == null) keeperAnimator = GetComponent<KeeperAnimator>();
    }

    void Update()
    {
        // Freeze movement while the game is over / not playing.
        if (GraveyardManager.Instance != null && !GraveyardManager.Instance.IsPlaying)
        {
            IsMoving = false;
            return;
        }

        Vector2 input = GKInput.Move();
        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;
        IsMoving = dir.sqrMagnitude > 0.01f && !Busy;   // stand still during a harvest swing / showcase

        Vector3 move = Vector3.zero;

        if (IsMoving && cameraTransform != null)
        {
            // Direction relative to where the camera faces.
            float camYaw = cameraTransform.eulerAngles.y;
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + camYaw;

            float angle = Mathf.SmoothDampAngle(
                modelRoot.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
            modelRoot.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float speed = GKInput.Sprint() ? sprintSpeed : walkSpeed;
            move = moveDir.normalized * speed;
        }

        // Gravity
        if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;
        move.y = verticalVel;

        cc.Move(move * Time.deltaTime);

        // Drive the locomotion blend (Idle 0 / Walk 0.6 / Run 1).
        if (keeperAnimator != null)
        {
            float animSpeed = IsMoving ? (GKInput.Sprint() ? 1f : 0.6f) : 0f;
            keeperAnimator.SetMoveSpeed(animSpeed);
        }
    }
}
