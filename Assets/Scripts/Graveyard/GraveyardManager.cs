// GraveyardManager.cs — game state for Graveyard Keeper (Checkpoint 2).
// Tracks resources, souls, and banished spirits; defines the goal (harvest targets
// + souls) that unlocks the escape gate; and handles the three end states:
// ESCAPED (win, via the gate), PERISHED (lose, health hit 0), and DAWN (time out).

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GraveyardManager : MonoBehaviour
{
    public static GraveyardManager Instance { get; private set; }

    public enum EndKind { Escaped, Perished, Dawn }

    [Header("Goal — collect these, then reach the gate")]
    public int woodTarget = 6;
    public int stoneTarget = 4;
    public int pumpkinTarget = 3;
    public int soulTarget = 5;

    [Header("Night length (seconds)")]
    public float nightDuration = 240f;

    [Header("HUD (assigned by the builder)")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI pumpkinText;
    public TextMeshProUGUI soulText;
    public TextMeshProUGUI banishText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI timerText;

    [Header("Interaction prompt")]
    public GameObject promptRoot;
    public TextMeshProUGUI promptText;

    [Header("End panel")]
    public GameObject endPanel;
    public TextMeshProUGUI endText;

    int wood, stone, pumpkin, souls, banished;
    float timeLeft;
    bool playing = true;

    public bool IsPlaying => playing;
    public float TimeLeft => timeLeft;
    public float NightLength => nightDuration;

    public bool IsGoalMet =>
        wood >= woodTarget && stone >= stoneTarget &&
        pumpkin >= pumpkinTarget && souls >= soulTarget;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        timeLeft = nightDuration;
        playing = true;
        Time.timeScale = 1f;
        if (endPanel) endPanel.SetActive(false);
        HidePrompt();
        UpdateHUD();
    }

    void Update()
    {
        if (!playing)
        {
            if (GKInput.RestartPressed()) Restart();
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) { timeLeft = 0f; EndGame(EndKind.Dawn); }
        UpdateTimer();
    }

    // ── Progress ────────────────────────────────────────────────────────────────
    public void AddResource(Harvestable.ResourceType type, int amount)
    {
        switch (type)
        {
            case Harvestable.ResourceType.Wood:    wood    += amount; break;
            case Harvestable.ResourceType.Stone:   stone   += amount; break;
            case Harvestable.ResourceType.Pumpkin: pumpkin += amount; break;
        }
        UpdateHUD();
    }

    public void AddSoul(int amount)   { souls += amount; UpdateHUD(); }
    public void BanishSpirit()        { banished++; UpdateHUD(); }

    // ── HUD ─────────────────────────────────────────────────────────────────────
    void UpdateHUD()
    {
        if (woodText)    woodText.text    = $"Wood {wood}/{woodTarget}";
        if (stoneText)   stoneText.text   = $"Stone {stone}/{stoneTarget}";
        if (pumpkinText) pumpkinText.text = $"Pumpkins {pumpkin}/{pumpkinTarget}";
        if (soulText)    soulText.text    = $"Souls {souls}/{soulTarget}";
        if (banishText)  banishText.text  = $"Spirits banished {banished}";

        if (objectiveText)
        {
            objectiveText.text = IsGoalMet
                ? "<color=#7CFC7C>Gate OPEN — reach the green beacon and press E to escape!</color>"
                : "Harvest resources & banish spirits to collect souls, then reach the gate.";
        }
    }

    void UpdateTimer()
    {
        if (!timerText) return;
        int m = Mathf.FloorToInt(timeLeft / 60f);
        int s = Mathf.FloorToInt(timeLeft % 60f);
        timerText.text = $"Dawn in {m:0}:{s:00}";
    }

    public void ShowPrompt(string msg)
    {
        if (promptRoot) promptRoot.SetActive(true);
        if (promptText) promptText.text = msg;
    }

    public void HidePrompt() { if (promptRoot) promptRoot.SetActive(false); }

    // ── End states ──────────────────────────────────────────────────────────────
    public void Win()  => EndGame(EndKind.Escaped);
    public void Lose() => EndGame(EndKind.Perished);

    void EndGame(EndKind kind)
    {
        if (!playing) return;
        playing = false;
        HidePrompt();
        AudioManager.PlayEnd(kind == EndKind.Escaped);

        if (endPanel) endPanel.SetActive(true);
        if (endText)
        {
            string title;
            switch (kind)
            {
                case EndKind.Escaped:
                    title = "<color=#7CFC7C><size=54><b>YOU ESCAPED</b></size></color>\n<size=26>You survived the night and fled the graveyard!</size>";
                    break;
                case EndKind.Perished:
                    title = "<color=#FF5555><size=54><b>YOU PERISHED</b></size></color>\n<size=26>The spirits claimed you.</size>";
                    break;
                default:
                    title = "<color=#FFB347><size=54><b>DAWN BROKE</b></size></color>\n<size=26>You didn't escape in time.</size>";
                    break;
            }

            endText.text =
                title + "\n\n" +
                $"<color=white>Wood {wood}/{woodTarget}   Stone {stone}/{stoneTarget}   Pumpkins {pumpkin}/{pumpkinTarget}\n" +
                $"Souls {souls}/{soulTarget}   Spirits banished {banished}</color>\n\n" +
                "<color=#8FE3FF><size=26><b>Press SPACE to play again</b></size></color>";
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioManager.ResumeMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
