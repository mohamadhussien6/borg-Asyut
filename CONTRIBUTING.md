# Working Conventions

<p align="center">
  <a href="https://github.com/mohamadhussien6/borg-Asyut/blob/main/README.md"><img alt="Project Home" src="https://img.shields.io/badge/Project_Home-24292F?style=for-the-badge&amp;logo=github&amp;logoColor=white" height="28"></a>
  &nbsp;
  <a href="https://github.com/mohamadhussien6/borg-Asyut/blob/main/Documentation/Final/Asyut_Game_Documentation_Final.pdf"><img alt="Documentation PDF" src="https://img.shields.io/badge/Documentation_PDF-2EA44F?style=for-the-badge&amp;logo=adobeacrobatreader&amp;logoColor=white" height="28"></a>
  &nbsp;
  <a href="https://github.com/mohamadhussien6/borg-Asyut/blob/main/CURRENT_STATUS.md"><img alt="Project Status" src="https://img.shields.io/badge/Project_Status-475569?style=for-the-badge" height="28"></a>
</p>

Keep the project small: seven main gameplay scripts, one Egyptian cartoon style, Arabic UI with correct shaping/RTL, optional real owner recordings, Windows only. Do not introduce progression systems or advanced architecture.

Inspect code callers and scene/prefab references before edits. Preserve `.meta` GUIDs and move assets with their metadata. The seven original script GUIDs are retained. Do not replay old external workspace generators over this project.

Use PascalCase C# types/files and scene/prefab names; camelCase fields/locals; character frames `Player_State_01.png`. Preserve the supplied voice filenames listed in the current asset inventory. Player-facing strings are logical Unicode Arabic; UIManager shapes them once before sending visual-order text to TMP. Do not reverse strings or enable a second reversal.

Keep existing Git/LFS rules; audio/fonts/layered source assets use LFS, metadata remains text. Never commit Library/Temp/build folders or credentials. Changes remain local; no remote push is part of this delivery. Update TASKS and actual validation evidence after a change. Static checks do not establish runtime success.
