// Compass.cs — Module 6.4. Rotates a UI needle so it always points to world
// north relative to where the camera is facing, and shows the heading text.

using UnityEngine;
using TMPro;

public class Compass : MonoBehaviour
{
    public Transform cameraTransform;    // reference to read yaw from
    public RectTransform needle;         // the arrow that rotates
    public TextMeshProUGUI headingText;  // optional "N 42°"

    void Update()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null) cameraTransform = Camera.main.transform;
            else return;
        }

        float yaw = cameraTransform.eulerAngles.y;

        if (needle != null)
            needle.localRotation = Quaternion.Euler(0f, 0f, yaw);

        if (headingText != null)
            headingText.text = $"{Cardinal(yaw)} {Mathf.RoundToInt(yaw)}\u00B0";
    }

    static string Cardinal(float deg)
    {
        deg = (deg % 360f + 360f) % 360f;
        string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        return dirs[Mathf.RoundToInt(deg / 45f) % 8];
    }
}
