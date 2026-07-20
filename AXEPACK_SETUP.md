# Pro Melee Axe Pack — Character + 47 Animations

This pack is already wired to work automatically. It contains **The Boss** (a
rigged humanoid character) and **47 Mixamo-style animations** (idle/walk/run,
turns, jumps, taunts, reactions, and a full set of axe melee attacks). All 47 are
playable in the game.

## What's automated

- **Import settings** — `AxePackImporter.cs` is an AssetPostprocessor: any FBX
  under `Assets/Art/Player/AxePack/` is imported as a **Humanoid** rig, each clip
  is renamed to its file name, and idle/walk/run clips are set to loop. You never
  touch the Rig/Animation tabs.
- **Character + controller + wiring** — one menu does the rest.

## Steps

1. Make sure the pack is at `Assets/Art/Player/AxePack/` (it ships there in this
   package — just let Unity import it).
2. Build the world first if you haven't: **Tools ▸ Graveyard Keeper ▸ Build World
   (Checkpoint 1)**.
3. Run **Tools ▸ Graveyard Keeper ▸ Setup Axe Pack Character (47 anims)**.

That single action:
- force-reimports the pack with Humanoid settings,
- swaps the player's mesh to **The Boss** and refits the CharacterController,
- builds `Assets/Art/Player/BossAxe.controller` with a locomotion blend tree
  (idle / walk / run), per-resource harvest attacks, **and a state for every one
  of the 47 clips**,
- assigns it to the character and wires the on-screen animation browser.

## Controls

| Action | Input |
| --- | --- |
| Move / sprint | WASD / Shift (idle→walk→run blend) |
| Harvest | `E` — plays the axe attack for that resource (Chop/Mine/Collect) |
| **Next animation** | `N` |
| **Previous animation** | `B` |
| **Back to movement** | `L` |

Press **N** to start cycling; the character plays each clip and the name + index
(e.g. `Animation 12/47 — standing melee attack downward`) appears on screen. This
is the easy way to demonstrate that all 47 movements work.

## Harvest → animation mapping

| Resource | Animation used |
| --- | --- |
| Wood (Chop) | `standing melee attack downward` |
| Stone (Mine) | `standing melee attack horizontal` |
| Pumpkin (Collect) | `crouch to standing idle` |

(The setup picks sensible fallbacks if a clip name differs.)

## Troubleshooting

- **T-pose / no animation:** the FBX didn't import as Humanoid. Re-run the setup
  menu (it force-reimports), or select the pack folder → reimport.
- **Character too big/small or feet off the ground:** the setup normalizes The
  Boss to ~1.8 units and drops its feet to the Player origin; if it still looks
  off, tweak the CharacterController height/center on the Player.
- **An animation doesn't show when cycling:** its clip name may collide with a
  reserved state; it's added as "name (clip)" — it still plays, just labeled that
  way.

## Credit

Character and animations: **Pro Melee Axe Pack** (as provided). Confirm its
license/attribution for your submission.
