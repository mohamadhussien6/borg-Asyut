# Public launch checklist (pending evidence)

## Rights and identity
- [ ] Creator: محمد الحسين / Mohamad El Hussein. Verify contributor permissions and required credits, soundtrack rights, and store terms; keep evidence privately.
- [ ] Store title: Borg Asyut — برج أسيوط. Runtime, executable, header and new submission artwork use Borg Asyut; historical captures remain clearly identified.
- [x] Removed the academic PDF attachment after a checksum-verified private backup; previous playable archive and third-party notices retained.
- [ ] Review privacy/Unity services and actual network traffic. submitAnalytics and configured Unity services are disabled; actual network traffic still needs verification.
- [ ] Answer store rating questions accurately (fictional theft/smuggling; hookah imagery); disclose generated content where asked.

## Build and test
- [ ] Clean LFS checkout, patched Unity 6000.3 LTS (at least 6000.3.27f2 for reviewed advisories), no missing references; run Edit Mode and Play Mode tests again. Historical 23 + 40 passed, 13 skipped are not new results.
- [ ] Windows build starts on clean PC without Unity, no verification mode, local-only music, debug package or missing DLL.
- [ ] Main menu, Arabic creator label, keyboard, help, sound, pause/resume, restart, four stages, combo, Game Over and score after restart. Check 16:9 and other screen sizes, audio balance, performance, antivirus and unsigned exe behavior.
- [ ] Verify company-name change and decide whether to migrate local saved scores. Do not promise old scores carry over.
- [ ] Web: Chrome, Edge, Firefox; click-to-focus/audio unlock, Escape, tab switch, fullscreen, controls, four stages, reload score/settings, console errors, memory, responsive embed, no browser quit button. Test on the actual store preview; no mobile support claim.
- [ ] ZIP integrity, SHA-256, source commit, logs and actual sizes. Browser initial network download measured for portal limits.

## Store assets and delivery
- [ ] Updated cover, real screenshots and trailer show final creator branding; no academic files. Use separate crop per portal. Existing media predates new creator label.
- [ ] Paste approved bilingual listing, platform, price, support contact, language and content disclosures. Draft preview before public listing.
- [ ] CrazyGames: initial download <= 50 MB, cover formats/preview video, basic review; full launch requires SDK. Poki: submit for review, verify SDK, mobile and agreement/exclusivity before distributing there.
- [ ] Create new versioned GitHub Release with approved tested binaries; preserve prior release except unwanted academic attachment. Publish only after owner approval.
