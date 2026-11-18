# Phone playtest checklist

Run this later when you have access to an Android phone. Mark each item Pass / Fail / Not tested. Record the APK filename/date, phone model, Android version and any failed steps. A short screen recording helps with timing or camera problems.

The latest October 25 APK (`OneDrive/Apps/game.apk`) launches **First Expedition**: three planets (Azure, Ember, Verdant), six stars, the superhero and the oxygen-reactive cape. The earlier October 22 tutorial APK had two planets and three stars. Use the matching count when testing. Editor automation does not replace these phone checks.

The oxygen update replaces the survival countdown with a bar and percentage. It is included in the latest APK; earlier tutorial APKs still show the countdown.

The walking/running and cape-motion project update comes after the October 25 APK. Test the animation items below once a new APK includes it.

## First launch and tutorial

- [ ] Launch with the phone held upright and auto-rotate disabled. The game opens horizontally and controls fit inside the screen cutout/gesture area.
- [ ] On a fresh install, follow the tutorial without skipping. The movement, landing and third-jump instructions are readable while playing.
- [ ] Use Skip. Pause and choose Replay Tutorial: guidance returns, collected stars and the checkpoint remain.
- [ ] Close and reopen after Skip/completion. The tutorial stays hidden; Replay Tutorial still works. Reinstalling is unnecessary to replay it.

## Movement, jumps and camera

- [ ] Partial joystick movement walks; the outer edge runs automatically. Releasing it stops movement on the surface.
- [ ] With the animation update: arms and legs swing while walking; running has a larger, faster stride. The hero turns toward travel, including sideways/backward joystick movement. Stopping settles the pose.
- [ ] With the animation update: the cape gently ripples while idle and lifts more while running. Its shoulders stay attached, its oxygen colour still changes, and it does not flicker or hide the character.
- [ ] With the animation update: limbs settle in the air; pause freezes limbs/cape; retry restores a neutral pose. Check movement on the underside of a planet too.
- [ ] Hold full movement through jump 1 and jump 2 on every planet. Both return to that planet and cannot escape gravity or switch planets.
- [ ] Tap again after landing within 1.2 seconds. Jumps become higher; the second landing shows a gold escape cue. Jump 3 can leave gravity.
- [ ] Repeated taps in the air do not skip a stage. Holding Jump does not repeatedly jump. Waiting more than 1.2 seconds after landing resets to jump 1.
- [ ] Move, jump and drag the camera together with separate fingers. No control gets stuck when a finger slides off it or another finger lifts.
- [ ] Walk around the underside and poles. The character stays upright relative to the surface and the camera stays outside the planet.
- [ ] Cross between planets in both directions. Camera transitions feel smooth; manual camera dragging temporarily overrides assistance.
- [ ] First Expedition only: try Azure <-> Ember, Azure <-> Verdant and Ember <-> Verdant. Choose different launch and landing points; no landing zone is required.

## Feedback and the full game loop

- [ ] The player is the masked superhero. The tutorial explains that his flight is lost and he is finding a way home.
- [ ] The cape is blue with full oxygen, blends through purple during depletion, and becomes red when oxygen is empty. Check it on the underside of a planet too.
- [ ] Refilling oxygen or retrying restores the blue cape. Pausing holds its current color.

- [ ] Jump stages have distinct, comfortable sounds. Each actual landing produces one pulse, with no repeated sound while standing still.
- [ ] Collect stars in different orders and on opposite sides of each planet. Each disappears once, creates sparkles and increments the counter once.
- [ ] Landing on another planet does not win. Only collecting the last star opens Route home found! and blocks movement underneath the screen.
- [ ] Oxygen stays at 100% while exploring inside gravity. Jumps 1 and 2 do not drain the tank.
- [ ] After jump 3 leaves every gravity field, the oxygen bar shrinks and the percentage falls. No survival timer is displayed.
- [ ] A nearly empty tank shows amber/red fill and an OXYGEN LOW label. The bar stays readable and does not block touch controls.
- [ ] Enter another planet's gravity before depletion. Oxygen immediately refills to 100%.
- [ ] Miss all planets after jump 3. An empty tank opens Out of oxygen without returning automatically.
- [ ] Retry returns to the last safe landing with full oxygen, clears momentum and starts the jump chain at 1. Collected stars remain collected.
- [ ] Play Again after victory returns to Azure and restores every star. Complete another attempt.

## Pause, interruptions and longer play

- [ ] Pause during a jump, the landing window and oxygen drain. Motion, oxygen, timing and visual effects freeze; current sounds stop.
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
