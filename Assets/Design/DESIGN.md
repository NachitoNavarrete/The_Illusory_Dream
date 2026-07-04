# Game Design: Nivel 2 (Colossal Update)

This document outlines the design for the "colossal" overhaul of Nivel 2, focusing on a "Destroyed" aesthetic, improved platforming rhythm, expanded puzzle logic, and the Robot Boss encounter sequence.

## UI Design

- **Color system:** 
  - **Surfaces:** Dark metallic greys (#2D2D2D) and rusted iron oranges (#8B4513).
  - **Intent Mapping:** 
    - **Safe (Checkpoint):** Caution Yellow (#FFD700) for "Save Houses".
    - **Hostile:** Pulsing Red (#FF0000) for the Boss and active hazards.
    - **Interactivity:** Cyan Sparks (#00FFFF) for active switches and energy nodes.
- **Typography:** 
  - **Headline:** Bold, technical sans-serif for "ENEMIGO! ELIMINAR!".
  - **Label:** Monospace for UI prompts ("Guardar [Abajo]").
- **Layout & Scaling:** 
  - **Tilemap Resolution:** Adjust Pixel Per Unit (PPU) to match the player's scale better. If tiles feel too large, increase PPU (e.g., from 16 to 32) to make the physical world feel more dense and detailed.
  - **Platforming Rhythm:** Reduce horizontal "dead space". Use verticality (broken pipes as ladders, hanging wires as platforms) to fill the "empty" world feel.
- **Components:**
  - **Save Houses (Checkpoints):** Small yellow metallic cabins with a flickering interior light. Use a soft yellow bloom to signal safety. (core)

## Asset Design

- **Visual identity:** "Industrial Ruin" — High contrast, heavy outlines to maintain visibility against busy backgrounds.
- **Palette:** 
  - **Dominant:** Desaturated Metal, Rust.
  - **Secondary:** Industrial Yellow (hazards/safety).
  - **Accent:** Energy Cyan (sparks/cables).
- **Composition rules:** 
  - **Backgrounds:** Multi-layered parallax. Far layers should be hazy/foggy (desaturated), mid-layers should have large silhouettes of broken structures.
  - **Decorations:** Broken pipes, dangling wires (some sparking), piles of scrap metal, cracked concrete floors.
- **Reference:** Match the `RobotEnemy` sprite's detail level and chunky, mechanical feel. The Boss should be 2.5x the size of a standard RobotEnemy, with more exposed internal circuitry.

## Game Feedback

- **Genre profile:** High-Energy Action-Platformer.
- **Interaction map:**

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Input | Rationale |
|------------|------|-----------|--------|------|-----------|--------|-------|-------|-----------|
| Robot Boss Scream | core | Heavy | Shake (low freq) | — | Scale (pulse) | Red Flash | SFX + Automata.mp3 | — | Establish threat |
| Melee Hit (Boss) | core | Critical | Shake (dir) | Hitstop (0.05s) | Squash | Large Sparks | Heavy Thud | Rumble | Unparryable impact |
| Revive Enemy | core | Medium | — | — | Pop (0.2s) | Blue glow | Digital hum | — | Signal threat return |
| Checkpoint Save | core | Minor | — | — | Pulse | Yellow Flash | Ding | — | Confirm safety |

### Robot Boss Sequence: The Automata

**Music:** `Automata.mp3` (Chase/Dodge), `Destructor.mp3` (Battle).

1. **Intro (Dialogue):** 
   - Camera locks on Boss rising from scrap. 
   - UI Text: "ENEMIGO! ELIMINAR!" in glitched red font.
2. **Phase 1: The Chase (core):**
   - Boss flies behind the player, shooting tracking energy orbs.
   - **Gimmick:** Previously defeated `RobotEnemy` sprites revive as ghostly red versions when the Boss passes them.
   - Music: `Automata.mp3`.
3. **Phase 2: The Dodge (core):**
   - Transition after reaching the "Arena". Boss creates energy barriers, trapping the player.
   - For 15 seconds, the Boss stays airborne, firing barrages. Player must use platforming to survive.
4. **Phase 3: Final Battle (core):**
   - Music: `Destructor.mp3`.
   - **Melee:** Boss slams the ground (unparryable, creates shockwave).
   - **Ranged:** Tracking laser fire.
   - **Vulnerability:** Boss repeats the flight phase; hitting its exposed core (cyan) during flight causes it to crash and take double damage.

### Puzzle Design

1. **Explosive Chain (core):**
   - Use `RobotEnemy` self-destruct countdown. Player must lure a robot to a "Cracked Door", damage it, and escape the blast radius to proceed.
2. **Timed Circuit (optional):**
   - 3 `PuzzleSwitch` (Toggle) connected to one `PuzzleDoor`. 
   - Hitting Switch 1 starts a 5s timer. All 3 must be hit before the timer resets to open the door.
3. **Weight Transfer (optional):**
   - A `PuzzleSwitch` (Hold) requires a physical object. Player must knock a "Scrap Block" onto the plate using a Dash or Bullet to keep the door open.

### Assets Needed

- **Robot Boss Prefab (core):** Large version of `RobotEnemy` with flying/hover animations.
- **Yellow Save House (core):** Sprite/Prefab for `Checkpoint.cs`.
- **Industrial Debris (core):** Broken pipes, scrap piles for decoration.
- **Ghostly Robot Sprite (optional):** Red tint/outline version of `RobotEnemy`.
- **Shockwave Particle (core):** Visual for Boss melee slam.
