# Project workflow

- After each task, run the relevant tests or verification checks and fix failures introduced by the task.
- When verification passes, create a local Git commit containing only that task's changes. Do not push automatically.
- Preserve unrelated user changes and do not include them in commits.
- Report the checks performed, their results, and the short commit hash. If verification is blocked, explain the limitation instead of claiming success.
- Save distributable builds, build summaries and generated symbol folders under the operating system's Desktop/FARMJAMBUILD directory, never inside the Unity project. Resolve the Desktop path through the operating system.
- Do not commit APK/AAB files, build output, Library, Temp or generated logs.
- Before mutating a project through Unity MCP, verify that the connected Editor's Application.dataPath belongs to this workspace; another project may be open.

For font and Android output-path changes, use the Editor menu
`Tools > FarmBoxMerge > Run Font and Build Path Checks`, or the batch entry point
`FarmBoxMergeEditorRegressionChecks.Run`. Use the appropriate additional checks for gameplay changes.
