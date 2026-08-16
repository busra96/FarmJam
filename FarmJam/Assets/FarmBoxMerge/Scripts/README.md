# FarmBoxMerge code structure

- `Core`: Owns the game session lifecycle. `FarmBoxMergeGameController` coordinates refresh and same-level retry flows.
- `Gameplay`: Contains box, item, merge-pattern and active-box registry logic.
- `Spawning`: Creates cards and queued world items from serialized configuration.
- `UI`: Contains card presentation/interaction and the UI-to-world board coordinator.
- `Helpers`: Stateless shared utilities for easing, random values and object lifecycle operations.

Runtime references stay serialized in the scene. Components may resolve a missing reference once during startup, but gameplay code does not create hidden manager components. New level data should be introduced as configuration assets rather than added as branching logic to these components.

`RefreshGame` creates a new configured random layout. `RetryLevel` replays the last captured card and item sequence, so retrying never changes the active level.

`AddRandomCard` adds one card using the level's configured counter range and color palette. Dragging a card onto `TrashDropZone` removes it with a short discard animation; cards added or removed during play do not alter the retry snapshot.
