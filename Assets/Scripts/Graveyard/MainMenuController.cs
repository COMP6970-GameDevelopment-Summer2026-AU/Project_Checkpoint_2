// MainMenuController.cs — the title screen. Shows the artwork; clicking the PLAY
// area (an invisible hotspot placed over the drawn button) or pressing Enter/Space
// loads the game scene. No EventSystem required, so it can't silently break with
// the Input System. The cursor is made visible here and re-locked in-game.

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Scene to load when the player presses Play.")]
    public string gameSceneName = "Graveyard";

    [Tooltip("Invisible rect placed over the PLAY button in the artwork.")]
    public RectTransform playHotspot;

    bool loading;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (loading) return;

        if (GKInput.SubmitPressed()) { StartGame(); return; }

        if (GKInput.PointerPressed())
        {
            Vector2 sp = GKInput.PointerPosition();
            bool hit = playHotspot == null ||
                       RectTransformUtility.RectangleContainsScreenPoint(playHotspot, sp, null);
            if (hit) StartGame();
        }
    }

    public void StartGame()
    {
        if (loading) return;
        loading = true;
        AudioManager.PlayClick();
        SceneManager.LoadScene(gameSceneName);
    }
}
