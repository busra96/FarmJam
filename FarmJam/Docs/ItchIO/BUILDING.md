# Browser release maintenance

Work on `codex/itchio-release`. The Android edition keeps its existing ads and analytics services. WebGL uses offline services and hides ad badges; Add Card and Trash remain limited by the per-attempt settings. Retry and Refresh do not require ads.

## Unity commands

Select the Web platform before running these Editor menus:

- `Tools > FarmBoxMerge > Itch.io > Run Release Checks`: validates all 35 level plans and independent card-deck solutions, offline helper policy, mobile reward callback semantics, settings and framing calculations.
- `Tools > FarmBoxMerge > Run Gameplay Regression`: runs actual card interactions and all authored solutions, modal/timer/input checks and six Game View sizes. Save scene changes and leave Play Mode first.
- `Tools > FarmBoxMerge > Itch.io > Export Cover and Banner`: renders existing project artwork, without modifying the scenes.
- `Tools > FarmBoxMerge > Itch.io > Build Web Release`: builds and zips the two enabled game scenes, preserving font licenses.

For automated runtime verification, start this project in a separate Editor process with `-buildTarget WebGL -executeMethod FarmBoxMergeItchIORuntimeChecks.Run`. This entry point exits the Editor after completion and restores the saved level, language and tutorial preferences. Do not invoke it in an Editor with unsaved work. It exports screenshots and a runtime report to the release folder.

All exports go to the OS-resolved Desktop under `FARMJAMBUILD/ItchIO-2026-10-15`. The build refuses to overwrite an existing WebGL directory or ZIP. Archive the previous candidate outside the Unity project before rebuilding. Keep builds, symbols, logs and QA images out of Git.

## Local preview and host verification

Serve the exported `WebGL` folder with `python Tools/itch_preview_server.py "<release folder>/WebGL"`. Open `http://127.0.0.1:8794/`; opening `index.html` directly will not work. The preview server binds only to loopback and supplies gzip and WebAssembly MIME headers.

Editor checks are not a substitute for browser checks. On itch.io, keep the page a draft until its hosted preview has passed loading, audio-after-click, first-time tutorial, merge/drop, helper limits, Retry/Refresh, language changes and save/reload checks. Only mark Mobile friendly after testing actual mobile browsers. See `UPLOAD-TR.md` for the upload fields and license review reminders.
