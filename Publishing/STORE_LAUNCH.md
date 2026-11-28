# Borg Asyut — private release workflow

Developer: Mohamad El Hussein — محمد الحسين.
Game Creator, Game Designer, Programmer, Creative Director & Producer.

Use a clean Git/LFS checkout, Python 3.11+ and a supported patched Unity 6000.3 LTS editor with Windows and Web Build Support. The installed 6000.3.2f1 can produce a private review candidate, but predates security fixes linked in [platform requirements](PLATFORM_REQUIREMENTS.md).

With Unity closed, run Tools/build_store.ps1 in PowerShell, selecting -Target Both and -Unity followed by the actual patched Unity.exe path. The script validates Git/LFS cleanliness, runs packaging tests and Unity EditMode/PlayMode with XML reports, builds requested targets and creates ZIPs/checksums under timestamped Artifacts/Store-* directories. Missing support or failed checks stop the workflow. Actual editor version is recorded from the executable.

For existing fresh players:

```
python Tools/package_store.py windows Builds/Store/Windows Artifacts/Borg_Asyut_Windows.zip
python Tools/package_store.py web Builds/Store/Web Artifacts/Borg_Asyut_Web.zip
```

Windows requires Borg Asyut.exe, matching Borg Asyut_Data and UnityPlayer.dll. Web requires root index.html and complete Build components. Notices are included; debug symbols/logs excluded. Fixture packaging tests are not player tests.

Before upload review [verification](VERIFICATION.md), [rights/privacy](RIGHTS_PRIVACY_REVIEW.md) and [launch gate](LAUNCH_GATE.md). Preview Web over HTTP and on the actual portal; verify audio unlock, focus, Escape, restart, fullscreen, persistence, download size, memory and console errors. Generic WebGL is not proof of Poki integration or mobile support.

The academic PDF was removed from the old GitHub Release on 2026-11-28 after a checksum-verified private backup. Its playable Windows ZIP was retained. No new public release or storefront listing is approved.
