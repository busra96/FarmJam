# FarmBoxMerge code structure

- `Core`: Owns the game session lifecycle. `FarmBoxMergeGameController` coordinates refresh and same-level retry flows.
- `Gameplay`: Contains box, item, merge-pattern and active-box registry logic.
- `Spawning`: Creates cards and queued world items from serialized configuration.
- `Levels`: Stores editable level definitions, their ordered catalog and the runtime level loader.
- `UI`: Contains card presentation/interaction and the UI-to-world board coordinator.
- `Helpers`: Stateless shared utilities for easing, random values and object lifecycle operations.
- `Editor`: Contains the FarmBoxMerge level authoring window and is excluded from player builds.

Runtime references stay serialized in the scene. Components may resolve a missing reference once during startup, but gameplay code does not create hidden manager components. Level data lives in configuration assets rather than branching logic inside gameplay components.

## Level authoring

Open `Tools > FarmBoxMerge > Level Editor`. Each `FarmBoxMergeLevelDefinition` asset contains:

- a level name and designer notes;
- an ordered item sequence expressed as color + consecutive count runs;
- the ordered starting-card list, including each card's color and counter.

`FarmBoxMergeLevelCatalog` owns the playable level order. Drag levels in the editor window to reorder them. `FarmBoxMergeLevelRuntime` reads the catalog assigned in the scene, disables legacy automatic spawning and loads the selected level. Item sequences longer than the visible queue are retained in a pending queue and fed into the scene without dropping entries.

The default catalog contains 35 authored levels in difficulty order. Levels 1-5 teach merge sizes, levels 6-15 introduce all five colors, levels 16-25 focus on queue and three-slot planning, and levels 26-35 provide longer expert layouts. Every level stays within the 12-card board limit and has enough color capacity to clear its authored item flow.

With a level catalog assigned, `RefreshGame` and `RetryLevel` both reload the current authored level. `NextLevel` advances through the catalog order. If no catalog is assigned, the old configured-random and replay behavior remains available as a fallback.

`AddRandomCard` and the UI's `ADD CARD` action both add a recommended level-one card. Dragging a card onto `TrashDropZone` removes it with a short discard animation; cards added or removed during play do not alter the authored level data.

Card counters are limited to `1-4`. A level-four card cannot merge again. Three-box groups use a compact L triomino, while four-box groups use randomized square/L/T/Z patterns whose width never exceeds two boxes.

The card board holds at most 12 cards. `ADD CARD` always creates a level-one card and chooses its color by comparing queued item demand with the capacity already available in cards and world boxes. Queue order breaks ties; when demand is already covered, it prefers a color that can immediately merge with another level-one card.

`ADD CARD` and `TRASH` each have three uses per attempt. Their remaining uses are shown in the UI and reset on refresh/retry. `FarmBoxMergeActionBudget.GrantAddCardUses` and `GrantTrashUses` are the integration points for a future rewarded-ad completion callback; no ad SDK is currently installed in the project.

`FarmBoxMergeOutcomeController` confirms a win for three seconds and a fail for five seconds before showing UI; both delays are independently configurable. A win requires both every queued, pending or assigned item to be gone and every active box group to have cleared. The fail countdown also starts whenever a color's empty-box demand is greater than that color's remaining unplaced item count, covering partially filled groups that can no longer be completed. The existing full-board/blocked-queue checks remain active. Card, item and action-budget activity cancels and reevaluates the pending fail timer. `NextLevelButton` advances through the catalog order; `RetryLevelButton` reloads the current catalog entry.

`FarmBoxMergeRemainingItemsView` shows the remaining unplaced collectables in `Canvas/RemainingItemsPanel`. It only enables colors authored in the current level, keeps those colors visible after their count reaches zero and uses a horizontal layout/content-size fitter so the panel width follows the active color count. Counts include both visible and pending level items and decrease when an item lands in a matching box. Reapply this HUD after hierarchy changes with `Tools > FarmBoxMerge > Apply Remaining Items HUD`.

## Visual setup

`Tools > FarmBoxMerge > Apply Mobile Visual Polish` reapplies the responsive portrait UI, camera, lighting, farm backdrop, market-table platform and card-prefab styling. `Tools > FarmBoxMerge > Apply Platform Polish` refreshes only the item platform. Both operations are idempotent, so they can be run again after scene hierarchy changes. The item queue shows at most six items on screen; longer authored sequences remain pending and enter the visible queue as space opens.

## Game feel

`Tools > FarmBoxMerge > Apply Game Feel Polish` adds the centralized sound, particle, haptic and animation controller and assigns the existing FarmJam SFX library.

- `FarmBoxMergeFeedbackController` owns SFX/music levels, pooled world/UI particles, mobile haptics, panel entrances and restrained camera punches.
- `FarmBoxMergeButtonFeedback` is added to scene buttons at runtime for consistent press/release animation and click sound.
- Gameplay scripts only announce meaningful moments (card merge/discard, box creation, item landing, box clear, win/fail), keeping feedback reusable for future levels.
