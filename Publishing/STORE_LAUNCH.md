# Borg Asyut — release workflow

Creator: محمد الحسين / Mohamad El Hussein. This is a source-level preparation, not an already validated new game build. Existing Windows ZIP and promotional media predate the updated credit.

## One-command build (Windows)
1. Use a clean Git clone with Git LFS, Python 3.11+ and Unity Hub editor 6000.3.2f1, including Windows and Web Build Support. Disable Development Build in Unity; close the editor.
2. In PowerShell run:

```powershell
git pull --ff-only
git lfs pull
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build_store.ps1 -Target Both
```

Use `-Target Windows` or `-Target Web` independently; pass `-Unity "C:\path\Unity.exe"` if needed. The script makes a new timestamped build directory, validates LFS, runs packaging unit tests, builds target(s), produces ZIPs, hashes, build logs and source commit in Artifacts/Store-*. ExecutionPolicy applies only to the subprocess; review the script before running it.

Alternatively switch Unity Build Profiles to the intended target and choose Ma3alem > Store Release > Build Windows x64 / Build WebGL. Output is Builds/Store unless BORG_STORE_OUTPUT is provided. Use empty output directories. Package with:

```
python Tools/package_store.py windows Builds/Store/Windows Artifacts/Borg_Asyut_Windows.zip
python Tools/package_store.py web Builds/Store/Web Artifacts/Borg_Asyut_Web.zip
```

ZIPs include credits and license notices. Web index.html is at archive root. Do not upload source, checksums or logs as playable games. Check [LAUNCH_GATE.md](LAUNCH_GATE.md) first. Preview Web on HTTP, not file:// (python -m http.server 8000 --directory <web-folder>). The generic Web build is not a portal SDK integration.

## Upload order
- itch.io: https://itch.io/game/new — first downloadable Windows; then HTML project with Web ZIP marked playable in browser and Windows separately downloadable. Start with 1280x720 embed; test fullscreen. https://itch.io/docs/creators/html5 and https://itch.io/docs/creators/getting-started
- Game Jolt: https://gamejolt.com/ — game page, separate Windows and HTML5 builds, publish only after preview and rating.
- CrazyGames: https://developer.crazygames.com/ — initial download <= 50 MB for basic launch; measure actual network transfer; full launch requires SDK. Read https://docs.crazygames.com/requirements/intro/ and https://docs.crazygames.com/requirements/game-covers/
- Poki: https://developers.poki.com/guide/share — review before integration. Read https://developers.poki.com/guide/requirements-quality and https://developers.poki.com/guide/revenue-deal-types before agreeing to any exclusivity.
- Steam: https://partner.steamgames.com/steamdirect — optional later.

See [store copy](STORE_COPY.md) and [media plan](MEDIA_PLAN.md). Proposed price: free/optional donations on itch.io, subject to creator approval. The old academic PDF is still attached to the existing GitHub Release: remove that asset through the GitHub web UI. Do not delete playable Windows ZIP before its tested replacement is available. New builds and public store pages have not been created by the source update.
