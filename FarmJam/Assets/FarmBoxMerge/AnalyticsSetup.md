# FarmBoxMerge Analytics Setup

FarmBoxMerge records only the following three gameplay events:

| Event | When it is recorded | Parameter | Type |
| --- | --- | --- | --- |
| `fbm_level_start` | A level is spawned and becomes playable | `level_number` | integer |
| `fbm_level_win` | The level win state is confirmed | `level_number` | integer |
| `fbm_level_fail` | The level fail state is confirmed | `level_number` | integer |

Create and enable these custom event schemas in Unity Dashboard > Analytics > Event Manager.
Event and parameter names are case-sensitive.

## Consent

Unity Analytics collection stays disabled while `AnalyticsIntent` is unspecified or denied.
After an in-game privacy/consent UI obtains the player's choice, inject
`IFarmBoxMergeAnalyticsService` and call `SetConsent(true)` or `SetConsent(false)`. The Unity
Developer Data framework persists that choice.
