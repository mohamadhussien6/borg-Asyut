# Cloud-only delivery

No commands, downloads, installations or file operations on the owner's computer are required or permitted by this workflow. All checkout, Unity execution, testing, image/video generation and release uploads run on standard GitHub-hosted runners. No self-hosted runners, paid larger runners or public deployment steps are configured.

## Version and scope

The cloud branch pins Unity 6000.3.29f1 (96d2fe17529c), a patched Unity 6.3 LTS release. Updating ProjectVersion.txt is configuration, not evidence of successful upgrade: first import, compilation, tests and player runs must pass on the cloud runner. Original README logo remains byte-identical; approved runtime art, voices and game physics are unchanged.

## Activation blocker and browser-only setup

GameCI requires a legitimate CI license configuration. The license job checks only whether the required secret values are present; it never prints their contents. Do not paste passwords, license contents or access tokens into chat or Git commits.

Unity Personal manual/offline request-file activation is no longer supported officially. No activation bypass or local license retrieval is prescribed here. If an existing valid CI setup is available, enter its legitimate values directly in the GitHub browser Settings → Secrets and variables → Actions: UNITY_LICENSE (or a legitimately owned UNITY_SERIAL), UNITY_EMAIL, UNITY_PASSWORD. Do not buy Pro for this workflow without explicit approval.

For a new browser-only Personal setup, use Unity Build Automation's free allowance: sign in at https://cloud.unity.com/ , choose/create the project, open DevOps → Build Automation, connect the public GitHub repository using minimum read permissions, select codex/cloud-release, project path '.', Unity 6000.3.29f1, Windows x64 and WebGL. Review the free allowance/billing page; stop if a payment method, subscription, charge or changed commercial agreement is required. No payment or provider account setup has been performed by this source change. Account authorization is owner-controlled. Do not enable public sharing or automatic storefront publishing.

Official sources:
- https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/licenses-and-activation/manual-license-activation/manual-activation-guide
- https://game.ci/docs/github/activation/
- https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- https://activation.unity3d.com/products/pricing-updates

## What runs

Source/package/logic checks and media generation do not need a Unity license. Unity tests and matrix builds wait for activation. Windows verification is a separate non-shipping player; its report must show actual jumps, persistence and all four zones. Retail Windows packages reject verification-driver assemblies. Chromium/Firefox run actual WebGL over HTTP with software rendering and collect menu/credits/pause screenshots and console exceptions. Browser smoke is not a complete human playthrough, listening review or performance certification.

Default Unity All excludes Explicit media capture cases; every skip stays recorded. Historical screenshots/trailer gameplay are identified as historical; cloud native and browser captures are separate fresh evidence. No claim of all-tests-passed is made until reports exist. WebGL retains compressed portable hosting settings; actual size limits are measured after building rather than advertised as a presumed improvement.

Every run attempts a new GitHub Release with draft=true and prerelease=true. A failed license/build/test produces a clearly BLOCKED diagnostics draft, never fabricated binaries. No step publishes a release. The owner must approve any eventual public launch. All outputs remain on GitHub; do not download them to the workstation for this task.
