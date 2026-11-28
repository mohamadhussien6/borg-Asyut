# Unity Build Automation browser configuration

Project created by owner: borg asyut. Select source branch codex/cloud-release, project folder '.', pinned Unity 6000.3.29f1. Keep auto-build off during setup so source updates do not consume additional provider allowance.

Create Windows 64-bit and WebGL configurations on the already displayed Free tier. Use only the standard/free machine allowed by the dashboard; no upgrade, card, paid add-on or paid capacity is authorized.

For both configurations, Advanced Settings -> Tests:
- Run Edit Mode Tests: enabled.
- Run Play Mode Tests: enabled.
- Failed Unit Test Fails Build: enabled.
- Select a timeout long enough for the existing approximately 100-second PlayMode suite (for example 900 seconds). A provider timeout is not a passing test.
- Default tests still exclude Explicit capture cases; use the separate Actions captures job after legitimate activation, and retain every skipped case in reports.

Advanced Settings -> Script hooks:
- Pre-Export Method: Ma3alem.Editor.CloudBuilds.PrepareBuildAutomation
- Post-Export Method: Ma3alem.Editor.CloudBuilds.RecordBuildAutomationExport
- Development Build: disabled.

The pre-export hook enforces the pinned editor, identity and approved-music exclusion checks. On WebGL it enables Gzip plus decompression fallback for static hosting, without changing gameplay or media assets. The post-export hook includes notices and an actual engine/platform receipt. These hooks are configured source, not proof of compilation; first successful cloud import/build must verify them.

Review usage/free quota before starting builds. A successful export alone is insufficient: inspect test results and run native/browser QA. No public sharing, release publishing, fees or storefront upload is enabled by these hooks.

Official reference: https://docs.unity.com/en-us/build-automation/advanced-build-configuration/run-custom-scripts-during-the-build-process and https://docs.unity.com/en-us/build-automation/reference/unit-tests
