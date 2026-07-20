// GhostVoiceDirector.cs — plays spooky ghost voices on a schedule: one shortly
// after the night begins (intro), one around the midpoint, and then randomly
// every ~20–40 seconds for the rest of the night. Only plays while the game is
// active. The actual sounds live in AudioManager (synthesized, or your own files
// in Resources/GKAudio/Voices).

using UnityEngine;

public class GhostVoiceDirector : MonoBehaviour
{
    public float introDelay = 3f;
    public Vector2 randomInterval = new Vector2(20f, 40f);

    float introTimer;
    float randomTimer;
    bool introPlayed;
    bool midPlayed;

    void Start()
    {
        introTimer = introDelay;
        randomTimer = Random.Range(randomInterval.x, randomInterval.y);
    }

    void Update()
    {
        var gm = GraveyardManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        // Intro cue.
        if (!introPlayed)
        {
            introTimer -= Time.deltaTime;
            if (introTimer <= 0f) { AudioManager.PlayGhostVoice(); introPlayed = true; }
        }

        // Midpoint cue.
        if (!midPlayed && gm.NightLength > 0f && gm.TimeLeft <= gm.NightLength * 0.5f)
        {
            AudioManager.PlayGhostVoice();
            midPlayed = true;
        }

        // Random cues.
        randomTimer -= Time.deltaTime;
        if (randomTimer <= 0f)
        {
            AudioManager.PlayGhostVoice();
            randomTimer = Random.Range(randomInterval.x, randomInterval.y);
        }
    }
}
