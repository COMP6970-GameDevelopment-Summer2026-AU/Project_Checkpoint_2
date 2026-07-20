// GKInput.cs — input abstraction for Graveyard Keeper.
// Works with the new Input System (Keyboard/Mouse.current) when the package is
// enabled, and falls back to the legacy Input Manager otherwise, so the project
// compiles and runs either way. Reads WASD, sprint, interact, mouse look, zoom.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class GKInput
{
    public static Vector2 Move()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        bool right = k.dKey.isPressed || k.rightArrowKey.isPressed;
        bool left  = k.aKey.isPressed || k.leftArrowKey.isPressed;
        bool up    = k.wKey.isPressed || k.upArrowKey.isPressed;
        bool down  = k.sKey.isPressed || k.downArrowKey.isPressed;
        float x = (right ? 1f : 0f) - (left ? 1f : 0f);
        float y = (up ? 1f : 0f) - (down ? 1f : 0f);
        return new Vector2(x, y);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    public static bool Sprint()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    public static bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // Melee attack — left mouse button.
    public static bool AttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // Menu: pointer click + position, and submit (Enter/Space).
    public static bool PointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    public static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        return m != null ? m.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    public static bool SubmitPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame ||
                             k.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    // Weapon selection: Tab toggles the axe menu; number keys 1-6 pick directly.
    public static bool WeaponMenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    public static bool DebugTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.f3Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F3);
#endif
    }

    public static int NumberKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0;
        if (k.digit1Key.wasPressedThisFrame) return 1;
        if (k.digit2Key.wasPressedThisFrame) return 2;
        if (k.digit3Key.wasPressedThisFrame) return 3;
        if (k.digit4Key.wasPressedThisFrame) return 4;
        if (k.digit5Key.wasPressedThisFrame) return 5;
        if (k.digit6Key.wasPressedThisFrame) return 6;
        return 0;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
        if (Input.GetKeyDown(KeyCode.Alpha5)) return 5;
        if (Input.GetKeyDown(KeyCode.Alpha6)) return 6;
        return 0;
#endif
    }

    public static bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    public static bool UnlockPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // Animation showcase: N = next, B = previous, L = back to locomotion.
    public static bool NextAnimPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.nKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.N);
#endif
    }

    public static bool PrevAnimPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.bKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.B);
#endif
    }

    public static bool LocomotionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.lKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.L);
#endif
    }

    // Mouse look delta (already frame-scaled by the device).
    public static Vector2 Look()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return Vector2.zero;
        return m.delta.ReadValue() * 0.05f;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    public static float Zoom()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return 0f;
        return Mathf.Clamp(m.scroll.ReadValue().y, -1f, 1f);
#else
        return Input.GetAxis("Mouse ScrollWheel") * 10f;
#endif
    }
}