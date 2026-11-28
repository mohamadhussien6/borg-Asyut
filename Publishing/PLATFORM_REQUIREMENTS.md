# Submission requirements checked 2026-11-28

These are private preparation notes, not platform approval. Recheck the signed-in submission form before upload. No account terms, prices, exclusivity or fees were approved for storefronts.

## itch.io

Prepare Windows ZIP and a separate playable HTML ZIP with index.html at archive root, relative case-sensitive paths and all dependencies bundled. The documented HTML limits are 1,000 files, 500 MB extracted total, 200 MB per file and 240-character paths. Cover: 630×500 (315:250 ratio). Test click-to-play, sound and fullscreen in the draft preview. Payment choice remains owner-controlled.

Sources: https://itch.io/docs/creators/html5 and https://itch.io/docs/creators/getting-started

## Game Jolt

Prepare separate downloadable Windows and HTML5 packages, description, controls, real screenshots and maturity answers. The official add-game page was reachable but not text-readable during research; current upload limits and artwork dimensions could not be independently verified. The kit contains proposed 1280×720 thumbnail and 2000×500 header exports; these are not represented as verified mandatory dimensions. Confirm inside the upload form before submitting.

Source: https://gamejolt.com/help-docs/creators/add-game

## CrazyGames

Prepare 1920×1080 landscape, 800×1200 portrait and 800×800 square covers. Title only on covers; no developer slogan, border or store logos. Silent preview videos: landscape 1080p and portrait 2:3, 15–20 seconds, at most 50 MB, no cursor, black bars, fast-forward or promotional text. Start with the corresponding static cover. Basic launch can omit SDK and monetization; full launch requires integration. Without SDK, total file size must be at most 50 MB; mobile-homepage eligibility is tighter. Measure actual browser download and memory after WebGL build. Current keyboard-only game is not advertised as mobile compatible.

Sources: https://docs.crazygames.com/requirements/game-covers/ and https://docs.crazygames.com/requirements/technical/ and https://docs.crazygames.com/requirements/intro/

## Poki

Requires desktop, mobile and tablet support, proportional 16:9 canvas, offline-bundled dependencies and restricted external requests. Current keyboard-only game needs a separately tested touch-control implementation before acceptance can be claimed. Integrate lifecycle events and verify with Poki Inspector after partnership review; do not add external advertising. Static thumbnail is text-free. Animated thumbnail: square at least 1080×1080, at least 50 fps, 4–6 seconds, muted MP4, at most 100 MB. No Poki SDK, partnership or exclusivity acceptance is claimed.

Sources: https://developers.poki.com/guide/requirements-quality and https://developers.poki.com/guide/your-game-page and https://developers.poki.com/guide/game-thumbnail

## Steam

Prepared store capsules: header 920×430, small 462×174, main 1232×706 and vertical 748×896. Library capsule 600×900, hero 3840×1240, header 920×430; logo fits within 1280×720. Shortcut icon 256×256, app icon 184×184. Use only title and real game artwork in capsules; creator roles belong in listing/credits/trailer. Gameplay screenshots should be at least 1920×1080, 16:9. AppID, SteamPipe depot configuration, Steam Direct fee, tax/bank onboarding and store/build review remain account-level steps. Do not invent an AppID or pay fees. Complete content survey, including generated-content disclosure if applicable, before review; no ESRB/PEGI certificate is claimed.

Sources: https://partner.steamgames.com/doc/store/assets and https://partner.steamgames.com/doc/gettingstarted/contentsurvey and https://partner.steamgames.com/steamdirect

## Unity security gate

The installed 6000.3.2f1 predates the first 6000.3 LTS fixes (6000.3.27f2) listed for Unity's October 2026 security advisories. Use a supported patched 6000.3 LTS editor and matching WebGL module, rerun checks and rebuild both players before official publication. The present candidate is for private review only. No claim is made that this game's specific code exercises the vulnerable APIs.

Sources: https://activation.unity3d.com/security/unity-emudhra-compromise and https://activation.unity3d.com/security/unity-brotli-decoder-vulnerability
