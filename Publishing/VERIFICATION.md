# Verification — 2026-11-28

Source baseline: b2e269c (main at checkout). This preparation is a private release candidate, not an approved public release.

| Check | Actual result |
|---|---|
| Git LFS integrity | Passed: git lfs fsck |
| Python archive tests | 4 passed, 0 failed; includes Borg Asyut Windows filename and unsupported-platform rejection |
| .NET 10 extracted production logic | 6 passed, 0 failed; actual RTLTMPro source and nested rules, scalar Mathf adapter; not engine/physics validation |
| Unity 6000.3.2f1 EditMode | 23 passed, 0 failed, 0 skipped |
| Unity 6000.3.2f1 PlayMode after Latin shaping fix | 40 passed, 0 failed, 14 skipped; skipped cases listed in XML; not executed tests |
| Windows x64 candidate build | Built successfully; fresh final candidate and exact receipts are in private Publishing_Ready evidence |
| Native Windows interaction | Player launched; actual menu, Credits, start and Escape pause inspected using Windows input and screen captures |
| Separate native verification build | 120-second run, 196 jumps/landings, floor 201, all four zones, zero logged errors; 28 pooled platforms; preferences restored. Mean 59.99 FPS and p95 16.67 ms on this machine under concurrent tool activity; not a hardware guarantee. No wall-bounce or flip occurred in this automated route; PlayMode covers those separately. A second 10-second 1920×1080 run also completed without errors. |
| WebGL | Not built or browser-tested: Web Build Support absent; disk capacity prevents installing complete patched toolchain |
| Network traffic / human listening / clean second PC | Not performed; no passing result claimed |

Commands executed from repository root:

```
python -B -m unittest discover -s Tools -v
python -B Tools/validate_project.py
dotnet run --project Tools/LogicChecks/LogicChecks.csproj -- .
```

Unity tests use the installed editor's `-batchmode -projectPath <repo> -runTests -testPlatform EditMode` / `PlayMode`, with separate XML and log output. Test runs omit `-quit` because Unity Test Runner exits after reporting. Full logs and reports are retained privately; public docs do not expose local machine paths or identifiers.

Reproduced and fixed: store builder referenced nonexistent PlayerSettings APIs; metadata validator lacked its package-reference manifest and mishandled URL-encoded links; pure-logic runner lacked scalar Mathf helpers; English credit lines were reordered by Arabic shaping. The exact-line Credits regression was observed failing before the shaping fix. Menu interaction checks now scope gameplay buttons separately from the new Credits control.

Preserved: eight runtime source files, scene/prefab GUIDs, game physics, four floor ranges, supplied art, original voice bytes and approved music bytes. No new gameplay system or production dependency was added. Product rename changes Unity preferences namespace; migration of legacy scores/settings is not claimed.

Official release blockers: patched Unity editor (see PLATFORM_REQUIREMENTS.md), WebGL toolchain/build/browser QA, verified rights evidence and store content questionnaires, account-level submissions and owner publication approval. Old playable GitHub release remains available.

The native verification build includes an opt-in driver under ASYUT_VERIFY. It is separate from the distributable candidate; do not distribute the verification executable. Exact assembly receipts and absence of the driver from the packaged candidate are recorded in private evidence.
