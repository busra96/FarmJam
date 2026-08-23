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

With a level catalog assigned, `RefreshGame` and `RetryLevel` both reload the current authored level. `NextLevel` advances through the catalog order. If no catalog is assigned, the old configured-random and replay behavior remains available as a fallback.

`AddRandomCard` and the UI's `ADD CARD` action both add a recommended level-one card. Dragging a card onto `TrashDropZone` removes it with a short discard animation; cards added or removed during play do not alter the authored level data.

Card counters are limited to `1-4`. A level-four card cannot merge again. Three-box groups use a compact L triomino, while four-box groups use randomized square/L/T/Z patterns whose width never exceeds two boxes.

The card board holds at most 12 cards. `ADD CARD` always creates a level-one card and chooses its color by comparing queued item demand with the capacity already available in cards and world boxes. Queue order breaks ties; when demand is already covered, it prefers a color that can immediately merge with another level-one card.

`ADD CARD` and `TRASH` each have three uses per attempt. Their remaining uses are shown in the UI and reset on refresh/retry. `FarmBoxMergeActionBudget.GrantAddCardUses` and `GrantTrashUses` are the integration points for a future rewarded-ad completion callback; no ad SDK is currently installed in the project.

`FarmBoxMergeOutcomeController` confirms a win for three seconds and a fail for five seconds before showing UI; both delays are independently configurable. It shows `WinPanel` when every queued, pending or assigned item is gone. A fail countdown starts while items remain and every world box slot is occupied if either all 12 card slots are occupied or the next queued item's color has no available matching box. A card-count, item-count or action-budget change cancels the pending fail timer. `NextLevelButton` advances through the catalog; `RetryLevelButton` reloads the current catalog entry.

## Visual setup

`Tools > FarmBoxMerge > Apply Mobile Visual Polish` reapplies the responsive portrait UI, camera, lighting, farm backdrop, market-table platform and card-prefab styling. `Tools > FarmBoxMerge > Apply Platform Polish` refreshes only the item platform. Both operations are idempotent, so they can be run again after scene hierarchy changes. The item queue shows at most six items on screen; longer authored sequences remain pending and enter the visible queue as space opens.
