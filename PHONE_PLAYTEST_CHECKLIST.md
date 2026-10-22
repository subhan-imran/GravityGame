# Phone playtest checklist

Run this later when you have access to an Android phone. Mark each item Pass / Fail / Not tested. Record the APK filename/date, phone model, Android version and any failed steps. A short screen recording helps with timing or camera problems.

The October 22 tutorial APK has two planets and three stars. The new **First Expedition** scene has three planets (Azure, Ember, Verdant) and six stars, two per planet. Use the matching count when testing. Editor automation does not replace these phone checks.

## First launch and tutorial

- [ ] Launch with the phone held upright and auto-rotate disabled. The game opens horizontally and controls fit inside the screen cutout/gesture area.
- [ ] On a fresh install, follow the tutorial without skipping. The movement, landing and third-jump instructions are readable while playing.
- [ ] Use Skip. Pause and choose Replay Tutorial: guidance returns, collected stars and the checkpoint remain.
- [ ] Close and reopen after Skip/completion. The tutorial stays hidden; Replay Tutorial still works. Reinstalling is unnecessary to replay it.

## Movement, jumps and camera

- [ ] Partial joystick movement walks; the outer edge runs automatically. Releasing it stops movement on the surface.
- [ ] Hold full movement through jump 1 and jump 2 on every planet. Both return to that planet and cannot escape gravity or switch planets.
- [ ] Tap again after landing within 1.2 seconds. Jumps become higher; the second landing shows a gold escape cue. Jump 3 can leave gravity.
- [ ] Repeated taps in the air do not skip a stage. Holding Jump does not repeatedly jump. Waiting more than 1.2 seconds after landing resets to jump 1.
- [ ] Move, jump and drag the camera together with separate fingers. No control gets stuck when a finger slides off it or another finger lifts.
- [ ] Walk around the underside and poles. The character stays upright relative to the surface and the camera stays outside the planet.
- [ ] Cross between planets in both directions. Camera transitions feel smooth; manual camera dragging temporarily overrides assistance.
- [ ] First Expedition only: try Azure <-> Ember, Azure <-> Verdant and Ember <-> Verdant. Choose different launch and landing points; no landing zone is required.

## Feedback and the full game loop

- [ ] Jump stages have distinct, comfortable sounds. Each actual landing produces one pulse, with no repeated sound while standing still.
- [ ] Collect stars in different orders and on opposite sides of each planet. Each disappears once, creates sparkles and increments the counter once.
- [ ] Landing on another planet does not win. Only collecting the last star opens victory and blocks movement underneath the screen.
- [ ] Miss all planets after jump 3. The countdown ends in Lost in Space without returning automatically.
- [ ] Retry returns to the last safe landing, clears momentum and starts the jump chain at 1. Collected stars remain collected.
- [ ] Play Again after victory returns to Azure and restores every star. Complete another attempt.

## Pause, interruptions and longer play

- [ ] Pause during a jump, the landing window and the space countdown. Motion, timing and visual effects freeze; current sounds stop.
- [ ] Resume without a stuck joystick or jump. Try Retry and Replay Tutorial from Pause.
- [ ] Background the app or lock the phone while holding controls. Returning leaves it paused until Resume; controls release properly.
- [ ] Try Android system Back from gameplay and each menu. Record any unexpected behavior; gesture Back still needs device verification.
- [ ] Test quiet/loud device volume and silent mode. Record whether cue volume is comfortable.
- [ ] Play for 15 minutes. Note stutter, slowdowns, heat, battery drain, crashes and text that is too small or overlaps.
- [ ] Force-close and reopen. Current star/checkpoint progress resets by design; the tutorial completion preference remains. Persistent level progress has not been added.

## Notes template

APK/date:

Phone / Android version:

Failed item and exact steps:

Expected / observed result:

Recording or screenshot filename:
