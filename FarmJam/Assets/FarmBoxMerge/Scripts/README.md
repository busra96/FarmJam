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

## Level authoring

Open `Tools > FarmBoxMerge > Level Editor`. Each `FarmBoxMergeLevelDefinition` asset contains:

- a level name and designer notes;
- an ordered item sequence expressed as color + consecutive count runs;
- a card spawn plan containing each color and its total level-one card count;
- a deterministic transparent-box flow containing the intended solution color, box size and four-box shape variant.

`FarmBoxMergeLevelCatalog` owns the playable level order. Drag levels in the editor window to reorder them. `FarmBoxMergeLevelRuntime` reads the catalog assigned in the scene, disables legacy automatic spawning and loads the selected level. Item sequences longer than the visible queue are retained in a pending queue and fed into the scene without dropping entries.

`Tools > FarmBoxMerge > Rebuild All Deterministic Slot Flows` rebuilds item and transparent-box flows together with a deterministic three-slot schedule. Colors remain mixed, but a new target group enters the item flow only after an active group can finish. The command preserves color totals, card totals and catalog order while preventing an unavoidable fourth-color queue lock. The legacy `Mix All Level Item Flows` menu item now runs the same safe rebuild so item and slot plans cannot drift apart.

The default catalog contains 35 authored levels in difficulty order. Levels 1-5 teach merge sizes, levels 6-15 introduce all five colors, levels 16-25 focus on queue and three-slot planning, and levels 26-35 provide longer expert layouts. Every authored card enters play at counter `1`. For levels with a fixed transparent-box flow, the card deck is derived deterministically from that solution order: each target receives exactly the level-one resources required to build its value before later colors can flood the 12-card board. A merge or box placement still deals the next pending card whenever a board slot opens. Legacy levels without a fixed flow retain shuffled card totals as a compatibility fallback.

With a level catalog assigned, `RefreshGame` and `RetryLevel` both reload the current authored level. `NextLevel` advances through the catalog order. If no catalog is assigned, the old configured-random and replay behavior remains available as a fallback.

`AddRandomCard` and the UI's `ADD CARD` action both add a recommended level-one card. Dragging a card onto `TrashDropZone` removes it with a short discard animation; cards added or removed during play do not alter the authored level data.

Card counters are limited to `1-4`. A level-four card cannot merge again. Three-box groups use a compact L triomino, while authored four-box groups use their saved square/L/T/Z variant whose width never exceeds two boxes.

The three reusable world slots show transparent white box silhouettes of size `1`, `2`, `3` or `4`. A silhouette restricts only the card value; the player chooses its color and remains responsible for following a solvable distribution. The first three authored entries fill the initial slots from left to right. Each later entry is consumed only by the slot whose completed box leaves; the other visible silhouettes never change. Retry and Refresh reset the cursor, so the same actions always reveal the same saved silhouettes and four-box variants. The intended color stored in the level is editor-only solution metadata and never colors or restricts the white silhouette. After the authored flow ends it loops deterministically only to preserve the always-visible slot presentation until the outcome state locks gameplay input. Levels without an authored flow retain the old live-calculated behavior as a compatibility fallback.

The card board holds at most 12 cards. `ADD CARD` always creates a level-one card and chooses its color by comparing queued item demand with the capacity already available in cards and world boxes. Queue order breaks ties; when demand is already covered, it prefers a color that can immediately merge with another level-one card.

`ADD CARD` and `TRASH` each have three uses per attempt. Their remaining uses are shown in the UI and reset on refresh/retry. `FarmBoxMergeActionBudget.GrantAddCardUses` and `GrantTrashUses` are the integration points for a future rewarded-ad completion callback; no ad SDK is currently installed in the project.

`FarmBoxMergeOutcomeController` confirms a win for three seconds and a fail for five seconds before showing UI; both delays are independently configurable. A win requires both every queued, pending or assigned item to be gone and every active box group to have cleared. The fail countdown also starts whenever a color's empty-box demand is greater than that color's remaining unplaced item count, covering partially filled groups that can no longer be completed. The existing full-board/blocked-queue checks remain active. Card, item and action-budget activity cancels and reevaluates the pending fail timer. `NextLevelButton` advances through the catalog order; `RetryLevelButton` reloads the current catalog entry.

`FarmBoxMergeRemainingItemsView` shows the remaining unplaced collectables in `Canvas/RemainingItemsPanel`. It only enables colors authored in the current level, keeps those colors visible after their count reaches zero and uses a horizontal layout/content-size fitter so the panel width follows the active color count. Counts include both visible and pending level items and decrease when an item lands in a matching box. Reapply this HUD after hierarchy changes with `Tools > FarmBoxMerge > Apply Remaining Items HUD`.

## Visual setup

`Tools > FarmBoxMerge > Apply Mobile Visual Polish` reapplies the responsive portrait UI, camera, lighting, farm backdrop, market-table platform and card-prefab styling. `Tools > FarmBoxMerge > Apply Platform Polish` refreshes only the item platform. Both operations are idempotent, so they can be run again after scene hierarchy changes. Item queue capacity comes directly from the authored `ItemQueuePoints` list. When the queue is full and its front item leaves, the next pending item is created at the final off-camera point before the line shifts forward, avoiding visible pop-in.

Queued collectables use the serialized `queueItemEulerAngles` presentation rotation (30 degrees on X and Y by default). On landing, `MergeItem.GetVisualBottomLocalY` measures the active model mesh and aligns its real bottom to `boxItemFloorHeight`, so differently pivoted produce models sit on the box floor instead of clipping through it.

## Game feel

`Tools > FarmBoxMerge > Apply Game Feel Polish` adds the centralized sound, particle, haptic and animation controller and assigns the migrated local SFX library.

- `FarmBoxMergeFeedbackController` owns SFX/music levels, pooled world/UI particles, mobile haptics, panel entrances and restrained camera punches.
- `FarmBoxMergeButtonFeedback` is added to scene buttons at runtime for consistent press/release animation and click sound.
- Gameplay scripts only announce meaningful moments (card merge/discard, box creation, item landing, box clear, win/fail), keeping feedback reusable for future levels.
