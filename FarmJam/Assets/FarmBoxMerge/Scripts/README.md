# FarmBoxMerge code structure

- `Core`: Owns the game session lifecycle. `FarmBoxMergeGameController` is the single refresh entry point.
- `Gameplay`: Contains box, item, merge-pattern and active-box registry logic.
- `Spawning`: Creates cards and queued world items from serialized configuration.
- `UI`: Contains card presentation/interaction and the UI-to-world board coordinator.
- `Helpers`: Stateless shared utilities for easing, random values and object lifecycle operations.

Runtime references stay serialized in the scene. Components may resolve a missing reference once during startup, but gameplay code does not create hidden manager components. New level data should be introduced as configuration assets rather than added as branching logic to these components.
