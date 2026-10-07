# FarmBoxMerge code structure

- `Core`: Owns the game session lifecycle. `FarmBoxMergeGameController` coordinates refresh and same-level retry flows.
- `Gameplay`: Contains box, item, merge-pattern and active-box registry logic.
- `Spawning`: Creates cards and queued world items from serialized configuration.
- `Levels`: Stores editable level definitions, their ordered catalog and the runtime level loader.
- `UI`: Contains card presentation/interaction and the UI-to-world board coordinator.
- `Helpers`: Stateless shared utilities for easing, random values and object lifecycle operations.
- `Shared`: Contains the shared enums required by FarmBoxMerge, including `ColorType`.
- `Editor`: Contains the FarmBoxMerge level authoring window and is excluded from player builds.

Project assets migrated from the old `Assets/FarmJam` root live under `Assets/FarmBoxMerge/Dependencies/FarmJam`. This includes only the fonts, button sprites, materials, textures and audio clips referenced by FarmBoxMerge; the game no longer depends on the old root folder.

Runtime references stay serialized in the scene. Components may resolve a missing reference once during startup, but gameplay code does not create hidden manager components. Level data lives in configuration assets rather than branching logic inside gameplay components.

The VContainer composition root treats feedback, outcome monitoring and adaptive layout as optional features. Removing one of those components registers a zero-cost null implementation, so core level, card, item and box systems can still run. Prefab configuration and settings remain required core dependencies. Runtime initialization stays in `FarmBoxMergeBootstrapper`, while the single VContainer ticker owns the throttled outcome and responsive-layout checks.

Hot-path allocations are intentionally bounded: card transforms are only written while their visual state is changing, outcome checks run at a configurable 10 Hz by default, screen/safe-area checks run four times per second, item queue animations reuse their movement buffers, remaining-item UI caches its `CanvasGroup` references, and each world slot keeps four reusable ghost boxes instead of rebuilding preview prefabs for every shape change.

## Level authoring

Open `Tools > FarmBoxMerge > Level Editor`. Each `FarmBoxMergeLevelDefinition` asset contains:

- a level name and designer notes;
- an ordered item sequence expressed as color + consecutive count runs;
- a card spawn plan containing each color and its total level-one card count;
- a deterministic transparent-box flow containing the intended solution color, box size and four-box shape variant.

`FarmBoxMergeLevelCatalog` owns the playable level order. Drag levels in the editor window to reorder them. `FarmBoxMergeLevelRuntime` reads the catalog assigned in the scene, disables legacy automatic spawning and loads the selected level. Item sequences longer than the visible queue are retained in a pending queue and fed into the scene without dropping entries.

`Tools > FarmBoxMerge > Rebuild All Deterministic Slot Flows` rebuilds item and transparent-box flows together with a deterministic three-slot schedule. Colors remain mixed, but a new target group enters the item flow only after an active group can finish. The command preserves color totals, card totals and catalog order while preventing an unavoidable fourth-color queue lock. The legacy `Mix All Level Item Flows` menu item now runs the same safe rebuild so item and slot plans cannot drift apart.

`FarmBoxMergeCardDeckBuilder` is the shared pure rule for turning a validated transparent-box solution into its deterministic level-one card deck. Runtime spawning and editor validation both use this builder, keeping new deck strategies independent from `CardSpawner` lifecycle code.

The default catalog contains 35 authored levels in difficulty order. Levels 1-5 teach merge sizes, levels 6-15 introduce all five colors, levels 16-25 focus on queue and three-slot planning, and levels 26-35 provide longer expert layouts. Every authored card enters play at counter `1`. For levels with a fixed transparent-box flow, the card deck is derived deterministically from that solution order: each target receives exactly the level-one resources required to build its value before later colors can flood the 12-card board. A merge or box placement still deals the next pending card whenever a board slot opens. Legacy levels without a fixed flow retain shuffled card totals as a compatibility fallback.

With a level catalog assigned, `RefreshGame` and `RetryLevel` both reload the current authored level. `NextLevel` advances through the catalog order. If no catalog is assigned, the old configured-random and replay behavior remains available as a fallback.

`AddRandomCard` and the UI's `ADD CARD` action both add a recommended level-one card. Dragging a card onto `TrashDropZone` removes it with a short discard animation; cards added or removed during play do not alter the authored level data.

Card counters are limited to `1-4`. A level-four card cannot merge again. Three-box groups use a compact L triomino, while authored four-box groups use their saved square/L/T/Z variant whose width never exceeds two boxes.

World drops target the nearest visible slot under the pointer instead of silently choosing the first compatible slot. An occupied or mismatched target rejects the drop and restores the card. Incoming merge targets are reserved until their animation completes, preventing concurrent drags from merging into the same card. The existing card-panel heading displays brief placement/merge guidance without adding another Canvas.

The three reusable world slots show transparent white box silhouettes of size `1`, `2`, `3` or `4`. A silhouette restricts only the card value; the player chooses its color and remains responsible for following a solvable distribution. The first three authored entries fill the initial slots from left to right. Each later entry is consumed only by the slot whose completed box leaves; the other visible silhouettes never change. Retry and Refresh reset the cursor, so the same actions always reveal the same saved silhouettes and four-box variants. The intended color stored in the level is editor-only solution metadata and never colors or restricts the white silhouette. After the authored flow ends it loops deterministically only to preserve the always-visible slot presentation until the outcome state locks gameplay input. Levels without an authored flow retain the old live-calculated behavior as a compatibility fallback.

The card board holds at most 12 cards. `ADD CARD` always creates a level-one card and chooses its color by comparing queued item demand with the capacity already available in cards and world boxes. Queue order breaks ties; when demand is already covered, it prefers a color that can immediately merge with another level-one card.

`ADD CARD` and `TRASH` each begin with one free use per attempt. While the free use remains, its count is shown and the AD badge stays hidden. At zero, the control remains available, its AD badge appears, and every further use requires a completed LevelPlay rewarded ad. Refresh and the top Retry action always require a rewarded ad; the FailPanel retry remains immediate. A LevelPlay interstitial is requested after each completed 15-level interval and never blocks progression when no ad is ready. Platform keys, ad-unit IDs, placements, and the interval live in `FarmBoxMergeAdsSettings.asset`.

`FarmBoxMergeOutcomeController` confirms a win for three seconds and a fail for five seconds before showing UI; both delays are independently configurable. A win requires both every queued, pending or assigned item to be gone and every active box group to have cleared. The fail countdown also starts whenever a color's empty-box demand is greater than that color's remaining unplaced item count, covering partially filled groups that can no longer be completed. The existing full-board/blocked-queue checks remain active. Card, item and action-budget activity cancels and reevaluates the pending fail timer. `NextLevelButton` advances through the catalog order; `RetryLevelButton` reloads the current catalog entry.

An exhausted deck with no remaining legal merge or placement also starts the fail timer, even if the card panel is not full. Application suspension pauses outcome evaluation. Settings lock gameplay input while open and close automatically on either outcome; their input lock does not override the win/fail lock. The settings button respects top and side safe-area insets.

`FarmBoxMergeRemainingItemsView` shows the remaining unplaced collectables in `Canvas/RemainingItemsPanel`. It only enables colors authored in the current level, keeps those colors visible after their count reaches zero and uses a horizontal layout/content-size fitter so the panel width follows the active color count. Counts include both visible and pending level items and decrease when an item lands in a matching box. Reapply this HUD after hierarchy changes with `Tools > FarmBoxMerge > Apply Remaining Items HUD`.

## First-time tutorial

Both steps share the single `Tutorial Hand` sprite and `Finger Pivot` on the Canvas component. Preview can be opened from Edit Mode or while already playing (including the main menu); it focuses Game View when ready. Unsaved Edit Mode scenes show Unity's Save/Don't Save/Cancel prompt rather than silently refusing to start. Clicking Preview again restarts an already running preview without modifying real player preferences.

`FarmBoxMergeTutorialController` is an optional VContainer feature on the authored gameplay Canvas. On level 1 only, an incomplete tutorial loops `hand1` between the two matching cards, then uses the same `hand1` sprite to demonstrate dragging the merged card to a compatible white box shape. Either merge direction and any compatible size-two slot are accepted. Tutorial graphics never intercept pointer events; a removable board interaction gate blocks early placement/trash, while the game controller temporarily disables add/refresh/retry actions. Settings pause the guide, and attempt resets restart an unfinished tutorial.

Completion is saved immediately after a successful real box placement under `<settings PlayerPrefsPrefix>.TutorialCompleted`. Once saved, the guide does not return on retry, scene reload or app restart. Existing players already beyond level 1 are not forced back. Removing the Canvas tutorial component disables the optional feature without changing core gameplay or creating a new Canvas.

Use `Tools > FarmBoxMerge > Tutorial > Preview First-Time Tutorial` for a playable level-one preview with isolated tutorial preferences and disabled progression writes. Stop Play Mode to exit; the original scene and saved player data are preserved. `Run Tutorial Checks` exercises actual merge/drop, both merge directions, rejected trash, input locks, settings pause, interrupted retry and completion persistence. Its report/screenshots are in `Temp/FarmBoxMergeTests/tutorial*`. `Reset Completion Flag` clears only the tutorial flag; to view it in ordinary Play Mode, the current level must also be level 1. Tutorial wording, timings and hand references can be edited on the Canvas component. `Install Canvas Guide` installs the authored overlay after intentional scene reconstruction.

## Visual setup

`Tools > FarmBoxMerge > Apply Mobile Visual Polish` reapplies the responsive portrait UI, camera, lighting, farm backdrop, market-table platform and card-prefab styling. `Tools > FarmBoxMerge > Apply Platform Polish` refreshes only the item platform. Both operations are idempotent, so they can be run again after scene hierarchy changes. Item queue capacity comes directly from the authored `ItemQueuePoints` list. When the queue is full and its front item leaves, the next pending item is created at the final off-camera point before the line shifts forward, avoiding visible pop-in.

Queued collectables use the serialized `queueItemEulerAngles` presentation rotation (30 degrees on X and Y by default). On landing, `MergeItem.GetVisualBottomLocalY` measures the active model mesh and aligns its real bottom to `boxItemFloorHeight`, so differently pivoted produce models sit on the box floor instead of clipping through it.

## Game feel

`Tools > FarmBoxMerge > Apply Game Feel Polish` adds the centralized sound, particle, haptic and animation controller and assigns the migrated local SFX library.

- `FarmBoxMergeFeedbackController` owns SFX/music levels, pooled world/UI particles, mobile haptics, panel entrances and restrained camera punches.
- `FarmBoxMergeButtonFeedback` is added to scene buttons at runtime for consistent press/release animation and click sound.
- Gameplay scripts only announce meaningful moments (card merge/discard, box creation, item landing, box clear, win/fail), keeping feedback reusable for future levels.

## Regression checks

Save the scene and leave Play Mode, then run `Tools > FarmBoxMerge > Run Gameplay Regression` (F8). The Editor-only runner validates all authored plans and an independent 12-card deck simulation, then plays the catalog through real card merge/drag/drop and item/box lifecycle APIs. It also exercises settings persistence using isolated temporary preference keys, rewarded completion/cancellation callback ordering, attempt budgets, retry determinism, separate outcome delays, modal/input locks, six Game View resolutions and synthetic safe-area insets.

Reports and screenshots are written to `Temp/FarmBoxMergeTests`. The original scene and Game View size are restored. Saved player progression is not advanced, and temporary Game View sizes and isolated test preferences are removed. Run this check in a quiet editor session without editing scripts or stopping Play Mode; the level sweep accelerates gameplay animations for testing only. Real ad-network behavior, Android rendering, touch latency, thermal performance and hardware haptics still require physical-device testing.

`Repeat Presentation Checks` reruns the screen/safe-area and authored main-menu Play transition checks after a completed level sweep, retaining its previously passed runtime results explicitly in the report. Use the full regression again after changing level or gameplay rules.
