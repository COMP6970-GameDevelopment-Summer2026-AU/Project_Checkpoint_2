// CameraRig.cs — third-person orbit/follow camera.
// Mouse orbits around the player (yaw/pitch), scroll zooms, and the camera
// smoothly follows the target. Cursor is locked for mouse-look; Esc toggles it.

using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [Header("Target")]
    public Transform target;            // the player root
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Orbit")]
    public float distance = 7f;
    public float minDistance = 3.5f;
    public float maxDistance = 12f;
    public float yaw = 0f;
    public float pitch = 20f;
    public float minPitch = -5f;
    public float maxPitch = 70f;
    public float lookSensitivity = 3f;
    public float followLerp = 12f;

    bool cursorLocked = true;

    void Start()
    {
        LockCursor(true);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (GKInput.UnlockPressed())
            LockCursor(!cursorLocked);

        if (cursorLocked)
        {
            Vector2 look = GKInput.Look();
            yaw   += look.x * lookSensitivity;
            pitch -= look.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        distance = Mathf.Clamp(distance - GKInput.Zoom() * 0.5f, minDistance, maxDistance);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + pivotOffset;
        Vector3 desired = pivot - rot * Vector3.forward * distance;

        // Simple collision: keep camera from clipping through the ground/walls.
        if (Physics.Linecast(pivot, desired, out RaycastHit hit))
            desired = hit.point + hit.normal * 0.2f;

        transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);
        transform.rotation = rot;
    }

    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
