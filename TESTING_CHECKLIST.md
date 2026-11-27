# Verification status

## Automated regression

- Edit Mode: 23 passed, zero failed.
- Play Mode: 40 passed, zero failed; 13 explicit media/review tests skipped.
- StageWidthFactorsAreContinuousAtNewBoundaries checks factor continuity around 26, 56 and 91.
- ActualCameraPressureTargetsMatchZoneFloors checks floors 25, 26, 55, 56, 90 and 91, including global pressure without a reset.
- StageIntroductionUsesMenuStyleAndAppearsOnlyAtZoneStart checks all four stage banners, fitting, timing and non-duplication.
- ActualSerializedRouteProvidesFairWidthsAndGapsAcrossZones and SeededRestStartJumpsLandOnGeneratedTargets cover generation and jump reachability.
- CameraPressureContinuesAtHighAltitude derives its fixture from the configured fourth-stage height.

## Build and preservation

Both scenes and route defaults use 0, 26, 56 and 91. Sprite GUIDs, import settings, artwork and platform collider configuration remain unchanged. PlayerController, CameraFollow, ScoreManager, GameManager, AudioManager and building code are unchanged. The Windows player builds and launches; the exact approved music is embedded and verification driver code is excluded.

## Gameplay and media

Fresh native automated gameplay covers all four stages, real jumps, landings, both wall rebounds and combos. Promotional frames are read directly from Unity rendering. The 30-second trailer, screenshots and comparison use the final stage names. Earlier owner-reported manual acceptance predates this update; a new human listening/feel review is not claimed.
