# Graveyard Keeper: Night Harvest — Checkpoint 2

**Course:** COMP 6910 · Game Development
**Developer:** Jahidul Arafat
**Engine:** Unity 6 (URP) · third-person 3D
**Modules covered:** M6–M7 (world, player, harvesting) + **M8–M9** (challenge, enemies, game loop, polish)

## 🎬 Gameplay Demo

[![Graveyard Keeper — gameplay demo](https://img.youtube.com/vi/i2hTi3xACW0/maxresdefault.jpg)](https://youtu.be/i2hTi3xACW0)

▶️ **Watch the demo:** https://youtu.be/i2hTi3xACW0

> Work the night shift in a cursed, endless graveyard: harvest what you need,
> banish the spirits that hunt you, choose your weapon, and reach the escape gate
> before dawn — or before the ghosts claim you.

---

## Checkpoint 2 Requirements — Validation

| # | Requirement | Met | Where / How |
| --- | --- | :---: | --- |
| 1 | Continue developing an original 3D game in Unity | ✅ | Graveyard Keeper (original theme + assets), built on the CP1 foundation |
| 2 | Keep/improve the playable 3D world | ✅ | **Endless** streaming graveyard (`EndlessWorld.cs`) — graves, resources, coffins, hazards, and ghosts spawn/recycle around the player |
| 3 | Keep/improve controllable player + camera | ✅ | Third-person "Boss" character with 47 animations (`ThirdPersonController`, `KeeperAnimator`) + orbit-follow camera (`CameraRig`) |
| 4 | ≥ 5 interactive / collectible / usable objects | ✅ (8) | Wood, Stone, Pumpkin nodes · Souls · Openable coffins · Escape gate · Ghosts · Selectable weapons |
| 5 | ≥ 3 interaction systems | ✅ (5) | Harvesting (E), collecting souls (touch), opening coffins & gate (E), melee combat (click), weapon selection (Tab/click) |
| 6 | Progress tracking system | ✅ | Wood/Stone/Pumpkin counts, Souls, Spirits banished, and player **Health** (`GraveyardManager`, `PlayerHealth`) |
| 7 | Clear UI feedback | ✅ | Resource counters, soul & banish counters, objective text, night timer, interaction prompt, **health bar**, hurt flash, end screen |
| 8 | ≥ 3 challenge systems | ✅ (4) | Lethal ghost **enemies** with chase/attack AI · **night-timer** pressure · cursed-**mist hazard** zones · **locked** escape gate |
| 9 | Clear gameplay goal | ✅ | Harvest the required resources + collect souls → the gate unlocks → reach it to escape |
| 10 | Win / lose / ending / restart | ✅ | **Escaped** (win), **Perished** (health 0 = lose), **Dawn** (timeout); end panel + **Space** to restart |
| 11 | Add/improve audio | ✅ | Kenney UI SFX (harvest/hit/pickup/gate), procedural night ambience, axe whoosh, hurt/soul/gate sounds, ghost voices |
| 12 | Basic visual polish | ✅ | URP **post-processing** (bloom/vignette/color), **particles** (banish/harvest/souls), **flickering fire lights**, moon + stars skybox, day/night cycle |
| 13 | Testable start to finish, no blocking errors | ✅ | Full loop: Main Menu → play → escape/perish/dawn → restart |

**Deliverables:** GitHub repository (this repo) + this updated README (title, concept, controls, objective, gameplay systems, known issues, and asset links — all below).

---

## Game Concept

A third-person survival game in an **endless** procedurally-streamed graveyard.
You harvest resources, fight and banish roaming ghosts to release **souls**, pick a
weapon, and must complete your tasks to unlock the **escape gate** and flee before
the night timer runs out. Ghosts fight back and can kill you, so it's a risk/reward
loop of gathering, fighting, and surviving.

## Objective / Gameplay Goal

1. Harvest the required **Wood, Stone, and Pumpkins**.
2. **Banish ghosts** (they drop **souls**) until you've collected enough souls.
3. When all targets are met, the **escape gate unlocks** (its beacon turns green).
4. Reach the gate and press **E** to **escape and win**.

**Win:** escape through the gate. **Lose:** health reaches 0 (*You Perished*), or the
night timer runs out before you escape (*Dawn Broke*). Press **Space** to play again.

## Controls

| Action | Input |
| --- | --- |
| Move | `W A S D` or **Arrow keys** |
| Look / orbit camera | Mouse · scroll to zoom |
| Sprint | `Left Shift` |
| Interact (harvest / open coffin / escape gate) | `E` |
| Attack (banish ghosts) | `Left mouse` |
| Select weapon | `Tab` (click a weapon, or press `1`–`6`) |
| Free the cursor | `Esc` |
| Play again (after the night ends) | `Space` |
| Browse all 47 character animations | `N` / `B` / `L` |

## Gameplay Systems

- **Endless world streaming** — content spawns in a grid around the player and
  recycles as you move (`EndlessWorld.cs`).
- **Third-person controller + follow camera** with locomotion/attack/hit-react
  animation from the 47-clip set.
- **Interaction system (`IInteractable`)** — one interactor drives harvesting,
  opening coffins, and using the escape gate (`PlayerInteractor.cs`).
- **Harvesting** — Wood / Stone / Pumpkin nodes with per-type axe animations.
- **Weapon system** — selectable, visible weapons (Quaternius axes/sword/hammer/
  scythe) attached to the hand; Tab weapon-select screen (`WeaponManager.cs`).
- **Combat** — left-click melee; ghosts flash, take knockback, and are banished,
  dropping a soul (`CombatController`, `GhostWander`, `Soul`).
- **Enemy AI** — ghosts wander, chase when near, and **attack for damage**.
- **Health & damage** — player health, hit reactions, hurt flash, death → lose
  (`PlayerHealth`); souls heal a little on pickup.
- **Progress tracking** — resource/soul/banish counters and a goal that gates the
  win (`GraveyardManager`).
- **Challenges** — lethal ghosts, night-timer pressure, cursed-mist hazards
  (`HazardZone`), and a locked escape gate (`EscapeGate`).
- **Day/night cycle**, night **skybox with moon + stars**, and **ghost voices**.
- **Audio** — Kenney UI SFX + procedural ambience, swing whoosh, and voices.
- **Visual polish** — URP post-processing, particle bursts, flickering fire lights.
- **Main menu** — custom Checkpoint 2 title screen; PLAY NOW / Enter starts the game.

## How to Run

1. Unity **URP** project (Unity 6 / 2022.3 LTS+). Install the **Input System**
   package and import **TMP Essentials** (*Window ▸ TextMeshPro*).
2. Copy this package's `Assets/` into your project.
3. **Tools ▸ Graveyard Keeper ▸ Build World (Checkpoint 2)**
4. **Tools ▸ Graveyard Keeper ▸ Setup Axe Pack Character (47 anims)** — character,
   animations, combat, and the weapon system.
5. **Tools ▸ Graveyard Keeper ▸ Build Main Menu**
6. Open `Assets/Scenes/MainMenu.unity`, press **Play**, click **PLAY NOW**.

> Note: the builders generate the `.unity` scenes, so re-run all three after any
> full Assets replacement. See `GIT_SETUP.md` for pushing/cloning.

## Known Issues / Limitations

- Ghost attack "impact" uses fixed timing rather than an animation event, so the
  hit may land slightly before/after the visual swing.
- Weapon **grip** (position/angle in hand) is tuned via `Grip Position` / `Grip
  Euler` on the Weapon Manager and may need a small nudge per preference.
- Balancing (targets, ghost damage, hazard density, night length) is first-pass.
- The demo video currently shows Checkpoint 1; a Checkpoint 2 recording is pending.
- Ghost voices are synthesized placeholders (override in `Assets/Resources/GKAudio/Voices/`).

## External Assets & Resources

- **Kenney — Graveyard Kit** (environment models) — CC0 — https://kenney.nl/assets
- **Kenney — UI Audio** (interaction SFX) — CC0 — https://kenney.nl/assets/ui-audio
- **Pro Melee Axe Pack** — rigged character ("The Boss") + 47 Mixamo-style
  animations (locomotion, melee, hit-reactions, blocks). *Animations only — no
  weapon meshes.*
- **Quaternius — Medieval Weapons Pack** — CC0 weapon models (axes, sword, hammer,
  scythe) for the weapon selector — https://quaternius.itch.io/lowpoly-medieval-weapons
- **Unity Technologies** — URP, TextMeshPro, Input System.
- **Night ambience, axe whoosh, and ghost voices** — generated procedurally in
  `AudioManager.cs` (original).
- **Title artwork** — `Assets/Art/UI/GraveyardRun_Title.png` (Checkpoint 2 banner).

---

*Checkpoint 2 — the core loop (gather → fight → survive → escape) is playable start
to finish. Remaining work is polish, balancing, the demo video, and itch.io.*