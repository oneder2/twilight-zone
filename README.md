# Twilight Zone: A Unity 2D Psychological Horror Game

Welcome to Twilight Zone, a personal game development project built using Unity 2D. This is a psychological horror game focusing on time loops, moral dilemmas, and puzzle-solving, set within the unsettling environment of a modern Chinese school trapped in Limbo.

## About the Game

You find yourself trapped in a chillingly familiar school building, caught in the Twilight Zone—an eerie boundary between reality and some unknown abyss. Cursed and bound to a time loop, you discover a horrifying truth: the only way to potentially "save" your acquaintances (classmates, friends, even your crush and teacher) from succumbing to evil spirits and meeting a grim fate is to kill them in a specific order before the spirits fully take hold.

This game explores the psychological toll of these actions within a brightly lit, seemingly normal school environment, creating a stark contrast to the dark narrative. Navigate the loop, uncover clues, face moral choices that impact subsequent cycles, and confront the source of the curse.

## Current Features

The game has a functional foundation with the following systems implemented and key bugs resolved:

1.  **Core Gameplay Loop & State Management:**
    * Robust game state management (`GameRunManager`: InMenu, Loading, Playing, Paused, InCutscene, GameOver, InDialogue).
    * Functional game start, restart (full reset via main menu flow), and exit-to-menu loops.
    * Resolved critical bugs related to scene transitions, singleton persistence, state machine initialization, and asynchronous operation conflicts during restarts.
2.  **Player System:**
    * Basic movement (walk/run) with state machine (`PlayerStateMachine`).
    * Stamina system with UI bar.
    * Interaction system (`InteractableCheck`, `InteractionPrompt`).
3.  **Scene & Persistence:**
    * Scene transition system using teleporters (`TransitionManager`, `ITeleportable` implementations like `Door`, `LadderTeleporter`).
    * Persistent "Boot" scene housing core managers (Singletons).
    * Session-based scene state saving (`SessionStateManager`, `GameSceneManager`) for interactable items (picked up, checked state).
4.  **Game Progression & Environment:**
    * Stage system (`StageManager`, `StageData`) driving changes based on game events.
    * Dynamic lighting changes based on stage (`LightManager`).
    * Dynamic background music changes based on stage (`AudioManager`, `MusicTrack` enum, `MusicLibraryEntry`).
    * Stage-driven enemy spawning (`EnemySpawner`, configured via `StageData` for prefab, interval, count, speed).
    * Basic enemy behavior (`EnemyNPC`) and player death trigger (`Deadly`).
5.  **Interaction & UI:**
    * Basic NPC interaction (`FriendNPC`) and dialogue display (`DialogueGUI`).
    * Item pickup (`ItemPickup`), inspection (`ItemCheckUp`), and inventory system (`Inventory`, `InventoryUI`).
    * Functional Menu System (Main Menu, Pause Menu, Settings).
    * Game Over UI (`GameOverUI`) with working Restart/Main Menu options.
6.  **Event System:**
    * Centralized `EventManager` handling various game events (status changes, transitions, timed events, etc.).
    * Timed event system with proper clearing on session end/restart.
7.  **Timeline CG Infrastructure:**
    * Basic progress tracking (`ProgressManager` for loop count).
    * System for triggering specific Timelines based on progress (`InitialSequenceTrigger`).
    * **Resolved cross-scene signal issue:** Implemented `TimelineSignalProxy` pattern for reliable communication between level-specific Timelines and persistent managers in the "Boot" scene using Unity Signals.
    * Basic opening Timeline (`Timeline_GameStartSequence`) structure set up, capable of controlling game state and player activation.

## Planned Features (Next Steps from Handover)

Development will now focus on building upon the established foundation:

1.  **Timeline CG System Implementation (Core Task):**
    * **Flesh out Opening CG:** Add visual effects (fade), audio, and specific dialogue content to `Timeline_GameStartSequence`.
    * **Create Key Scene Timelines:** Implement sequences for Beginner's death, Crushsis on the rooftop, Friend in the lab, Crush encounter, Teacher confrontation (including evidence/dialogue integration), and the final sequence.
    * **Implement Trigger Logic:** Add code to player interactions (killing Beginner, talking to Crushsis, etc.) to play the corresponding Timelines.
2.  **Player Choice Impact:**
    * Expand `ProgressManager` to track key player decisions (e.g., kill methods, found evidence).
    * Modify interaction scripts to update `ProgressManager`.
    * Adjust `StageManager`/`EnemySpawner` logic to modify difficulty parameters (spawn rate, speed, enemy type) based on stored progress/choices.
3.  **Data Persistence (Save/Load):**
    * Implement saving/loading of `ProgressManager` data to a file (e.g., using JSON serialization via `JsonUtility` and `File.IO`).
    * Integrate load logic on game start and save logic at appropriate points (exit, key events).
4.  **Refine Enemies & Hazards:**
    * Implement trap spawning based on `StageData` or player choices.
    * Develop more specific enemy behaviors if needed.
5.  **UI Enhancements:**
    * Implement the notebook UI element for clues/player thoughts.
    * Create UI for dialogue choices.
    * Potentially add a `CutsceneUIManager` for handling full-screen CG images within or alongside Timelines.
6.  **Core Loop Logic:**
    * Implement puzzle elements (finding keys).
    * Build the evidence checking system and link it to dialogue.
    * Develop the complex dialogue tree system, especially for the Teacher interaction.
    * Implement the multi-ending logic based on player progress and choices.

## Installation & Setup

1.  **Prerequisites:**
    * Unity Editor (**Unity 6 version 6000.0.4f1** or compatible).
    * Git installed.
2.  **Clone the Repository:**
    ```bash
    git clone [https://github.com/](https://github.com/)[YourGitHubUsername]/TwilightZoneUnity.git # Replace with your actual repo URL
    ```
3.  **Open in Unity:**
    * Launch Unity Hub.
    * Click "Open" or "Add project from disk".
    * Navigate to the cloned project folder and select it.
4.  **Run the Game:**
    * Open the initial scene (likely your "Boot" scene or a dedicated startup scene).
    * Press the "Play" button in the Unity Editor.

## Contributing

This is currently a personal project, but feedback and suggestions are always appreciated. Feel free to open an issue if you find bugs or have ideas.

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## Acknowledgments

* Inspired by the atmosphere and narrative design of horror games like "House".
* Built with Unity 2D.


