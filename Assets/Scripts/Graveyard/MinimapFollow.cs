// MinimapFollow.cs — Module 6.4. Keeps a top-down orthographic camera centered
// over the player so its RenderTexture shows a north-up minimap. Rotation is
// fixed (north stays up); only the X/Z position tracks the player.

using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;
    public float height = 40f;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x, height, target.position.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // look straight down, north up
    }
}
