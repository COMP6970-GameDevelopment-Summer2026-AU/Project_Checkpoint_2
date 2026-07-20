# Adding Mixamo Harvest Animations

The keeper's harvesting now supports per-resource animations (Chop for Wood, Mine
for Stone, Collect for Pumpkin — Module 7.3). This guide gets Mixamo animations
onto the player.

## Important: you need a rigged humanoid character

Mixamo animates **rigged humanoid** characters. The Kenney graveyard "keeper" is a
static mesh with no skeleton, so it can't be Mixamo-animated. Use one of these as
the player instead (both are what your Module 6 resources point to):

- A **Mixamo character** (mixamo.com has free rigged characters), or
- **Quaternius – Animated Men** (already rigged, free): https://quaternius.com/packs/animatedmen.html

Everything else in the game stays the same — you're only swapping the player mesh.

## Step 1 — Get the character + animations from Mixamo

1. Go to https://www.mixamo.com and sign in (free Adobe account).
2. Pick a **Character** (or upload the Quaternius/other humanoid model to auto-rig).
3. Add these animations (search terms in parentheses) and **Download** each as
   **FBX for Unity**, **With Skin** for the character/idle and **Without Skin** for
   the rest, "In Place" checked for walk/run:
   - **Idle** (search: *idle*)
   - **Walk** (*walking* — enable *In Place*)
   - **Run** (*running* — enable *In Place*)
   - **Chop** for Wood (*chopping*, *axe*, or *standing melee attack*)
   - **Mine** for Stone (*mining*, *pickaxe*, or *hit*)
   - **Collect** for Pumpkin (*picking up*, *gathering*, or *crouch to standing*)
4. Rename the downloaded files so each contains its keyword, e.g. `Keeper@Idle.fbx`,
   `Keeper@Walk.fbx`, `Keeper@Run.fbx`, `Keeper@Chop.fbx`, `Keeper@Mine.fbx`,
   `Keeper@Collect.fbx`. (The builder matches on the words idle/walk/run/chop/
   mine/collect, so the names just need to contain them.)

## Step 2 — Import into Unity

1. Put the FBX files in `Assets/Art/Player/`.
2. Select the **character** FBX → Inspector → **Rig** tab → Animation Type =
   **Humanoid** → Apply. Do the same for each animation FBX (Humanoid, and set
   Avatar Definition = *Copy From Other Avatar* → the character's avatar).
3. For Walk/Run/Idle clips: **Animation** tab → check **Loop Time**.

## Step 3 — Put the character in the scene

1. Build the world if you haven't: **Tools ▸ Graveyard Keeper ▸ Build World**.
2. In the Hierarchy, expand **Player**. Delete the old **Keeper_Mesh** child.
3. Drag your **rigged character** into **Player** as a child. Reset its local
   position to `(0,0,0)` so its feet sit at the Player origin.
4. Select **Player** → in **Third Person Controller**, set **Model Root** to the
   new character transform.

The character comes with its own **Animator** component — that's what we'll drive.

## Step 4 — Build the Animator (one click)

Run **Tools ▸ Graveyard Keeper ▸ Build Keeper Animator**.

It scans your imported clips, builds an Animator Controller with a locomotion
blend tree (Idle/Walk/Run on `Speed`) plus **Chop / Mine / Collect** states
triggered by `Harvest` + `HarvestType`, saves it to
`Assets/Art/Player/KeeperAnimator.controller`, and assigns it to the Player's
Animator. A dialog reports which clip it used for each slot.

## Step 5 — Play

Press Play. Walking blends idle→walk→run; pressing **E** on a resource stops the
keeper, plays the matching harvest animation, and grants the resource. The
movement lock during the swing is `Swing Time` on the Player's **Player
Interactor** (default 0.7s) — set it close to your harvest clip length.

## Troubleshooting

- **Character is a T-pose / doesn't animate:** the rig isn't Humanoid, or the
  animation FBX avatars aren't copied from the character's avatar (Step 2.2).
- **Feet float or sink:** reset the character's local position to 0 and make sure
  its own scale matches; adjust the CharacterController height/center on Player.
- **Slides while animating:** that's expected — root motion is off on purpose so
  the CharacterController drives movement. Match `Swing Time` and locomotion speeds
  to taste.
- **"No Animator on Player" when building:** you haven't added the rigged character
  under Player yet (Step 3), or you deleted its Animator.
