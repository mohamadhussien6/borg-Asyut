# Public launch preparation

Creator: محمد الحسين محمد رمزي (Mohamad Al Hussein Mohamad Ramzy). Proposed storefront name: Borg Asyut — برج أسيوط. Current in-game subtitle still contains "icy tower Asyut"; review branding clearance before public distribution.

## Release gates
- Confirm all contributor and third-party permissions and required credits; retain soundtrack distribution evidence. Public repository access is not an open-source license.
- Rebuild using Unity 6000.3.2f1 with Git LFS assets restored. Existing Windows download predates the new creator credit. Changing Unity company name changes the default PlayerPrefs storage location; test and decide whether to migrate old scores before launch.
- Run Edit/Play Mode regression tests and fresh Windows launch; inspect the Arabic creator label at different screen sizes. Historical results (23 Edit Mode / 40 Play Mode passed, 13 skipped) do not establish new build success.
- Build WebGL separately with Web support, gzip plus decompression fallback, and root index.html. Test browser audio unlock, keyboard focus, fullscreen, pausing and local scores in a real embed. WebGL support is not yet verified.
- Package complete Windows runtime (exe, matching Data directory, UnityPlayer.dll and remaining runtime files) and license notices; use clean ZIP and SHA-256 checksum. Web ZIP must contain index.html at its root.
- Remove the academic PDF from the existing public release through GitHub release asset management; it remains attached until removed. Do not rewrite Git history. Rebuild screenshots and trailer to reflect the new credit, then create a versioned release with tested ZIPs.
- Inspect telemetry and privacy; existing ProjectSettings includes submitAnalytics: 1. Do not assert no data collection without verification.

## Store order
1. itch.io: Windows ZIP first, HTML5 draft after browser QA. https://itch.io/game/new
2. Game Jolt: Windows build and HTML5 package after testing. https://gamejolt.com/
3. CrazyGames and Poki: separate browser submission, SDK and portal-specific review. https://developer.crazygames.com/ and https://developers.poki.com/
4. Steam: optional later after onboarding, fee and store/build review. https://partner.steamgames.com/steamdirect

Use [store copy](STORE_COPY.md), real gameplay images and official trailer; refresh media after new build. Pricing and public store publication require creator approval. No new binaries or public store pages have been produced by these source changes.
