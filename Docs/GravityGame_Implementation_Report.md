# GravityGame Implementation Report

Detailed project documentation and development handover  
Prepared 3 November 2026

## 1 Project overview

GravityGame is a Unity exploration prototype built around walking on small spherical planets and jumping between their separate gravity fields. It now has a complete playable objective loop: explore, chain three jumps, cross space before oxygen runs out, collect stars, recover from mistakes at a checkpoint, and replay after victory.

The current character is a stranded superhero who has lost the ability to fly. Collecting stars represents charting a route home. The most developed level is First Expedition, containing three planets and six stars. Earlier desktop and mobile scenes remain available for development and comparison.

The project includes mobile touch controls, a guided tutorial, sound and visual feedback, oxygen survival, a color-changing cape, and procedural walking and cape animation. Automated Unity checks and Android package audits are recorded. Real phone testing, measured performance, production packaging, and store publication remain separate work.

### How to read this report

Sections 2 through 11 explain the game and its implementation. Sections 12 through 16 describe tooling, development history, builds, validation, and remaining work. The final section identifies the source files for future maintenance.

Three types of completion need to be distinguished:

- **Implemented in the project:** present in the current scripts, scenes, or assets, including local changes that have not been committed.
- **Previously validated:** supported by saved checks from earlier Unity runs. These are historical results, not a new test run performed for this document.
- **Included in the recorded Android build:** packaged in the October 25 superhero APK. Later walking and cape-motion changes are not included in that recorded artifact.

## 2 Current scenes and level structure

| Scene | Purpose | Planets and objective |
| --- | --- | --- |
| PhaseOne.unity | Original movement and camera demonstration | One spherical planet; free movement without the star objective |
| TwoPlanets.unity | Desktop transfer demonstration | Azure and Ember; free exploration and gravity transfers |
| MobilePrototype.unity | Mobile controls and objective prototype | Two planets; three stars, with two on Azure and one on Ember |
| FirstExpedition.unity | First full exploration level | Azure, Ember, and Verdant; six stars, with two per planet |
| SampleScene.unity | Retained Unity starter scene | Not the developed gameplay level |

All scene files are in Assets/Scenes. The four playable scenes use the superhero visual. The earlier astronaut visuals remain available as alternatives.

### First Expedition

Azure is the blue starting planet with radius 20, Ember is orange with radius 12, and Verdant is green with radius 9. Verdant is created at world position (32, 24, 20). The planets form a triangular exploration layout. Every pair has a short space gap between its gravity fields; the level-layout validator requires each gap to be greater than one and less than five world units.

The player can choose the order of planets and stars. There is no required landing zone, fixed route, or automatic transport. Stars are placed around the surfaces, including far-side locations, so exploration involves moving around each sphere rather than simply reaching it.

The saved level checks cover all six directed routes: Azure to Ember and back, Azure to Verdant and back, and Ember to Verdant and back. Retry checkpoints update after safe landings on the destination planet.

### Build scene selection

The current ProjectSettings/EditorBuildSettings.asset enables MobilePrototype and retains SampleScene disabled. FirstExpedition exists but is not selected in that default scene list. The October 25 recorded Android artifact launches First Expedition through the build setup used for that artifact.

The reusable MobileDemo.BuildAndroid helper explicitly creates and builds MobilePrototype. Running that helper unchanged will produce the two-planet prototype, even though First Expedition is the more developed level. Select the intended launch scene in the relevant build profile or deliberately adjust the build workflow when packaging the newer level.

## 3 Spherical gravity and movement

### Gravity fields

PlanetGravity supplies acceleration toward the center of a planet. Within most of the field, acceleration has a constant strength. It fades near the outer boundary and reaches zero outside the field. This is a controllable gameplay gravity model rather than a simulation of inverse-square gravitational attraction.

The component derives radius from a centered SphereCollider and the object's world scale. Authored planets use uniform scale. The default acceleration is 25 world units per second squared, the field extends six units above the surface, and the outer 1.5 units provide the falloff region. Thin rings visualize the field boundary in the demo scenes. Simple colored landmarks provide surface reference points.

Each source can determine whether a position is within its field, calculate distance above its surface, return local outward up, and return gravitational acceleration. At the exact planet center, a fallback direction avoids an undefined normalized vector.

### Player physics

SphericalPlayer controls a Rigidbody and Y-axis capsule. Unity's global Rigidbody gravity is disabled; custom radial acceleration is applied during physics steps. Rotation is controlled by the script, interpolation smooths the displayed motion, and continuous dynamic collision detection is enabled.

Movement is relative to the camera, projected onto the local tangent plane. Local up changes as the character travels around a sphere, allowing movement over poles and the underside. Diagonal input is normalized so diagonal travel does not receive an unintended speed increase. A surface ray probe, a vertical-velocity check, and a short post-takeoff lock determine grounding.

On a surface, releasing movement slows the character toward rest. Inside gravity while airborne, steering is reduced and requires active input. Outside all fields, movement input and the movement motor do not steer or brake the flight. The player coasts with takeoff momentum until another gravity field captures them or oxygen is exhausted.

### Running and input

Desktop development controls use WASD or arrow keys, either Shift key to run, and Space to jump. Touch controls provide analog walking and automatic running near the joystick edge. Running changes travel speed; it does not bypass the three-jump requirement.

For touch running, the controller enters the run state at an input magnitude of 0.85 and leaves it below 0.75. Using different entry and exit thresholds prevents small finger movement from rapidly switching between walking and running. These thresholds apply to the joystick value after its dead-zone adjustment.

### Multiple gravity sources

The player maintains a current planet and an array of available gravity sources. A nearby in-range source can become the active planet. A three-unit switching margin reduces rapid switching in custom scenes with overlapping fields. During either of the first two jumps, the source planet is explicitly retained so those jumps cannot transfer to another planet.

## 4 Consecutive jumps and planet travel

The jump chain is built from successive grounded takeoffs. It is not an airborne double-jump system. The player jumps, lands, and taps again within the landing window to advance the stage.

| Parameter | Default | Meaning |
| --- | --- | --- |
| Walking speed | 7 | Target surface walking speed |
| Running speed | 14 | Target surface running speed |
| Movement acceleration | 35 | Rate of movement velocity adjustment |
| Jump 1 height | 2 | First nominal jump height |
| Jump 2 height | 3.2 | Second nominal jump height |
| Jump 3 height | 8 | Third nominal escape-jump height |
| Consecutive jump window | 1.2 seconds | Grounded time allowed before the next stage |
| Jump input buffer | 0.15 seconds | Short buffer for a recent jump request |
| Air control | 0.3 | Relative steering strength inside gravity while airborne |
| Gravity alignment speed | 360 degrees per second | Player turn rate toward local up |

These values come from the controller defaults; the saved First Expedition scene also uses walking speed 7, running speed 14, second-jump height 3.2, third-jump height 8, and oxygen capacity 3. Inspector values can change balance. The jump heights are nominal settings: lower-jump containment and flight conditions also shape the actual arcs.

### Chain rules

- Jump 1 and jump 2 remain inside the source planet's gravity and return for another landing.
- Jump 3 releases the lower-jump constraint and can leave gravity. Reaching another planet still depends on direction and momentum.
- Only time spent grounded consumes the 1.2-second chain window. Flight time does not consume it.
- Pausing freezes the window. Waiting too long, switching planets, or retrying resets the chain. Landing after jump 3 resets it as well.
- Holding Jump does not auto-repeat. Airborne taps do not directly advance a stage; the short input buffer still applies to recent requests near landing.
- Running is optional for unlocking jump 3. It can help horizontal travel but never authorizes an early escape.

### Lower-jump containment

The first implementation was strengthened to prevent running, unusually high jump settings, large physics steps, or outward impulses from turning jump 1 or 2 into an escape. The player is bound to a spherical height limit inside the full-strength gravity region. Each physics step considers the next position, including tangential travel, gravity, and velocity changes.

The controller adds inward assistance for surface travel during lower jumps, limits the next radial position, and restores the bound after external displacement. This makes the three-stage rule part of the physics behavior rather than just a button label. The third takeoff removes that constraint.

## 5 Camera behavior

PlanetCamera follows the player with a smooth orbit. It carries the camera heading around changes in local up, smooths roll, limits pitch, and follows the active planet as the player changes fields. Desktop users orbit with mouse movement; mobile users drag the free look area.

The camera protects both its focus and its final smoothed position against planet intersections. Obstruction checks consider the registered planets, not only the starting planet. Retry changes a respawn counter that causes the camera to reset its pitch, framing offset, and smoothing state immediately.

The mobile camera also offers assistance. It can follow forward surface movement and frame a planet ahead during flight. It adjusts viewing direction and framing only; it does not change the player's gravity, trajectory, or takeoff momentum. Manual dragging suppresses assistance for approximately 1.4 seconds before automatic framing resumes.

The mobile demo creator uses a wider setup than the base camera defaults: distance 10, default pitch 8 degrees, and field of view 72 degrees. Desktop scenes retain mouse capture and release behavior; mobile gameplay keeps the pointer available for its interface.

## 6 Oxygen survival and checkpoint recovery

### Current oxygen system

The original lost-in-space timer was replaced with an oxygen reserve. Oxygen stays full inside any valid gravity field, drains only outside all fields, and refills immediately on entry into gravity. The default capacity is three seconds of space exposure. The HUD displays a bar and percentage instead of a survival countdown.

At 30 percent or less while outside gravity, the label shows OXYGEN LOW and the bar uses a warning color. At 15 percent or less, the fill is red. At zero, the loss state displays Out of oxygen. Pause and interruption-triggered pause stop depletion.

The serialized field oxygenCapacity replaces lostInSpaceDelay using Unity's FormerlySerializedAs migration support. Increasing capacity permits longer flights without a separate timeout or distance-based death overriding the reserve. This provides a tuning point for future upgrades, but an oxygen upgrade purchase or progression system has not been added.

### Defeat and retry

When oxygen empties, the player stays at the failed location in space. Velocity is cleared, the Rigidbody becomes kinematic, active movement input is cleared, and the interface offers an explicit retry. There is no automatic rescue or teleport.

A checkpoint records the latest safe grounded position and orientation relative to a planet. Retry returns to that checkpoint, clears momentum and held input, restores oxygen, resets the jump chain, and resets the camera. Collected stars stay collected. The player can also request a retry during an active run.

### Retry compared with replay

| Action | Destination | Stars | Oxygen and jump chain |
| --- | --- | --- | --- |
| Retry or Try again | Latest safe landing | Retained | Full oxygen; chain reset |
| Play Again after victory | Original start on Azure | All restored | Full oxygen; chain reset |
| Close and reopen the game | New session | Run progress resets | New run state |

Stars, checkpoints, and run completion are stored in memory for the current session. Tutorial completion is a separate saved preference. Persistent campaign progress and recovery after process termination have not been implemented.

## 7 Stars and victory

StarCollection owns the objective and tracks the registered pickups. CollectibleStar supplies a trigger collider and a rotating, bobbing visual. Touching a valid pickup hides its visual, disables its trigger, increments progress once, and raises the collection event used by feedback and tutorial systems.

The collection path validates the player, the collection owner, membership in the objective, and the star's active and collected state. It refuses collection while paused, defeated, or already victorious. Both trigger entry and trigger stay are handled so a player can collect after resuming while still overlapping a star.

MobilePrototype requires three stars; First Expedition requires six. Stars can be collected in any order. Landing on Ember or another planet does not win the current game. That was an earlier version of the prototype and was replaced by the collectible objective.

Only the final required pickup activates victory. The player stops, normal controls are blocked, and the message Route home found appears with Play Again. Replay restores all registered stars and returns to the original start. The superhero story uses this objective as charting a route home; a separate home-planet finale or campaign ending has not been implemented.

## 8 Mobile interface and application behavior

### Touch controls

MobileTouchControl implements three control types: joystick, look, and jump. Each control remembers the finger or pointer that started it. Another finger cannot release or take over that control. This supports moving, jumping, and looking together without one control stealing another's input.

The joystick has a default dead zone of 0.12, scales the remaining movement range, and moves its knob to reflect finger position. Look dragging scales by the Canvas scale factor. The jump control sends one request on pointer down. Pointer release, drag ending, cancellation, disable, and menu transitions clear held state.

MobileCircleImage draws circular interface geometry without requiring a separate control texture. Text labels are configured not to intercept touch raycasts. The Canvas scales the interface and anchors the control area within Screen.safeArea. The tutorial card changes width to fit the available space.

### HUD and menus

MobileGameUI creates and maintains the gameplay interface, including the current gravity status, oxygen bar and percentage, star counter, jump-stage label, landing-window hint, tutorial, and movement controls. The jump button becomes gold when escape jump 3 is ready.

Separate panels handle Pause, defeat, and victory. Available actions include Resume, retry from checkpoint, Replay Tutorial, and Play Again as appropriate. Clearing input on state changes prevents a joystick or look drag from remaining held under a menu.

### Display orientation and frame target

The mobile game locks to Landscape Left. The lock is applied before the splash screen, during mobile initialization, and after returning from background or focus interruption. Portrait rotation and automatic switching to the other landscape orientation are disabled. The saved Android launch settings were also changed to fixed landscape.

The runtime requests 60 frames per second. This is a target, not a measured performance result. MobileGameUI stores previous frame-rate and orientation settings and restores its owned settings when it is disabled.

### Pause and lifecycle

Pause stops game time, clears touch and camera input, freezes oxygen and jump timing, and stops current feedback sounds. Resume explicitly restores the prior time scale and clears input again. Focus loss or background interruption pauses active gameplay; returning to the app requires the player to resume.

The interface restores its owned pause state when leaving the scene so the next scene is not left with a stopped clock. Android system Back is currently reached through the keyboard Escape input path. Gesture Back and three-button navigation still require device verification, and there is no dedicated in-game exit flow.

### Development controls

| Action | Desktop development input |
| --- | --- |
| Move | WASD or arrow keys |
| Run | Either Shift key |
| Jump | Space |
| Retry | R during gameplay or defeat |
| Restart after victory | R |
| Pause or resume in the mobile scene | Escape |
| Release the pointer in desktop scenes | Escape |
| Recapture the desktop pointer | Left mouse click |

The mobile controls can also be exercised with the mouse in the editor. These desktop paths support development and do not establish how physical multi-touch will feel on a phone.

## 9 Interactive tutorial and gameplay feedback

### Guided learning

MobileTutorial advances through movement, first jump, second jump, third jump, travel, exploration, and completion states. The instructions react to actual movement and jump or landing events rather than relying only on a timed slideshow.

The opening explains that the superhero's flight is gone and teaches joystick movement and running. Subsequent cards teach landing before the next jump, the chain window, aiming the third jump, entering gravity to refill oxygen, and using the cape as a supply indicator. The exploration stage introduces stars and explains that retry preserves them.

The card is nonblocking and has a Skip action. Pause includes Replay Tutorial, which restarts guidance without resetting stars or the checkpoint. Completion and skipping are stored under GravityGame.Tutorial.Completed.v1 on a running device. Editor validation deliberately avoids writing that preference. Event handlers unsubscribe before resubscribing and when disabled to prevent duplicate progress events.

### Sound and visual feedback

MobileGameFeedback listens for actual jump, landing, and pickup events. Each jump stage has a different short chime and message. Landing creates a pulse, and landing after jump 2 highlights the ready escape jump in gold. Star collection produces a gold message, a chime, and a small sparkle burst. The final pickup has its own completion cue.

Chimes are generated from short audio waveforms in code. The pickup effect lazily creates eight reusable sparkle objects using the star's mesh and material. It does not continually create a new group for every pickup. Sounds and visual timing respect pause. Retry clears stale messages, pulses, and bursts; disabling the feedback component removes subscriptions, and generated audio clips are cleaned up on destruction.

These systems supply functional feedback. Sound comfort, visibility in motion, and readability on actual device screens still need hands-on review.

## 10 Character assets and superhero presentation

### Earlier astronaut assets

Assets/Art/Characters contains modular low-poly astronaut assets: AstronautExplorer, AstronautScout, AstronautHelmet, and OxygenBackpack prefabs, with shared meshes and solid-color URP materials. Explorer is white with orange trim; Scout uses blue and cyan. The helmet has a dark visor and radio details, and the backpack has twin oxygen cylinders.

The models use local Y as up, local positive Z as forward, and a two-unit character height. The recorded full-suit triangle count is 2,100; the separate helmet has 516 and the backpack 312. These are visual assets without their own Rigidbody or gameplay colliders.

The astronaut visuals originally replaced the visible capsule while preserving the player root, collider, movement, and camera references. They are now retained inactive in the playable scenes. The astronaut creation menu remains an explicit way to restore that alternative appearance.

### Current stranded superhero

Assets/Art/Hero/Prefabs/StrandedHero.prefab is the current player visual. It is a masked low-poly hero with a midnight suit, gold gloves and boots, a four-point home-star emblem, and a blue cape. The recorded hero triangle count is 2,168. The cape is also available as OxygenCape.prefab.

The visual sits below the existing player physics root. The root movement collider, Rigidbody, gravity, and three-jump rules remain authoritative. Applying the art preserved the existing scene object identities and kept earlier astronaut children available.

### Oxygen cape color

OxygenCape reads SphericalPlayer.OxygenFraction. Full oxygen produces blue, partial depletion blends through purple, and an empty reserve produces red. Gravity entry and retry restore blue because they refill the actual oxygen reserve.

Color is applied with a MaterialPropertyBlock and a small matching emission contribution. This changes the individual renderer without rewriting the shared material asset. The cape uses the same oxygen state as the HUD, so it does not introduce a competing survival timer. Its color timing automatically follows changes to tank capacity.

## 11 Walking animation and cape motion

### Procedural locomotion

HeroLocomotion adds shoulder and hip pivots and swings opposite arms and legs. Actual tangential velocity controls gait strength and cadence. Running increases stride, bob, and forward lean. The art rotates smoothly toward travel independently of the physics root and camera, including sideways and backward movement input.

The animation settles when the hero stops or becomes airborne, freezes during pause, and resets after retry. It does not need a rig import, Animator controller, or extra physics bodies. Using actual velocity avoids making a blocked character run in place simply because the joystick is held.

### Procedural cape movement

CapeFlutter adds an idle ripple and stronger lift during movement. Vertex weighting holds the shoulder seam in place while allowing the lower cape to move. The component clones a private dynamic mesh and reuses vertex buffers. It restores the original mesh and destroys its private instance when disabled.

The cape movement is a procedural mesh effect, not a cloth simulation. Its shape motion is independent of OxygenCape's color behavior. Pause, defeat, and victory stop flutter updates; retry resets the animation state. Mesh normals and bounds are updated as the cape moves, so performance still needs measurement on target phones.

### Packaging and source-control status

These animation changes exist in the working project and have recorded focused checks. At review time, HeroLocomotion.cs, CapeFlutter.cs, HeroAnimationSetup.cs, and HeroAnimationChecks.cs and their metadata are untracked files. The shared hero and cape prefabs, SuperheroAssets.cs, SUPERHERO.md, and the phone checklist also have local modifications associated with the newer work.

The latest recorded committed Android build is October 25 and predates the animation update. A new package is required before the animation can be tested on a phone. The focused animation validation passed, but the full gameplay suite was not rerun after this addition.

## 12 Project architecture and editor tools

### Runtime component responsibilities

| Script in Assets/Scripts | Main responsibility |
| --- | --- |
| PlanetGravity.cs | Radial acceleration, surface distances, and finite influence fields |
| SphericalPlayer.cs | Movement, jumps, gravity selection, oxygen, checkpoints, defeat, and victory state |
| PlanetCamera.cs | Orbit, local-up transitions, assistance, obstruction checks, and retry reset |
| MobileTouchControl.cs | Pointer ownership and joystick, jump, and look input |
| MobileCircleImage.cs | Circular control geometry for the mobile interface |
| MobileGameUI.cs | HUD, menus, safe-area layout, display settings, pause, and lifecycle handling |
| MobileTutorial.cs | Guided learning, skip, replay, and completion preference |
| MobileGameFeedback.cs | Jump and landing cues, chimes, messages, and pickup sparkles |
| StarCollection.cs | Registered objective progress, pickup guards, and completion |
| CollectibleStar.cs | Trigger collection and pickup visual animation |
| OxygenCape.cs | Per-renderer cape color tied to the oxygen reserve |
| HeroLocomotion.cs | Speed-driven limb animation and visual heading |
| CapeFlutter.cs | Private cape mesh deformation and cleanup |

The player exposes events such as Jumped and Landed. The star objective exposes StarCollected. The tutorial and feedback systems subscribe to these events, allowing actual gameplay state to drive their presentation.

### Editor creation and upgrade tools

The GravityGame editor menu provides Create Phase 1 Demo, Create Two Planet Demo, Create Mobile Prototype, and Create First Expedition Level. Art tools create and apply the astronaut or superhero and add hero walking and cape motion. StarDemo supports objective setup; FirstExpedition creates the three-planet layout from the saved mobile template.

The creators make the demonstrations reproducible. Interactive scene creation prompts the user to save open edits, and recreating an existing First Expedition asks before replacement. Some batch helpers recreate or upgrade saved scenes without an interactive dialog. Run them on a temporary copy or a closed, deliberately backed-up project.

UpgradeStarScene and UpgradeOxygenScenes update the saved mobile scenes in place. The oxygen upgrade rebuilds the UI for both MobilePrototype and FirstExpedition while retaining authored object identities. Shared character prefabs let the four scenes inherit the art and animation setup.

### Engine and packages

The saved editor version is Unity 6000.5.9f1. The manifest includes Universal Render Pipeline 17.5.0, Input System 1.20.1, Unity UI 2.5.0, and Test Framework 1.7.0. Other installed packages include development, AI, navigation, timeline, collaboration, and visual-scripting tools. Package presence does not mean multiplayer, AI-driven enemies, navigation gameplay, or a timeline-based story has been implemented.

An unused experimental com.unity.pipeline editor/live-reload package was removed after it blocked Android preprocessing by querying a desktop-only subtarget. This is separate from Universal Render Pipeline, which remains installed for graphics. The project uses a mobile rendering configuration with render scale 0.8; actual visual quality and cost must be measured on devices.

## 13 Development milestones

The following dates are the dates shown in the repository's commit log. Individual narrative notes or copied log timestamps can use different dates or time zones; they should not be treated as a precise record of working hours.

| Commit date | Milestone | Reference |
| --- | --- | --- |
| October 20 | Initial Unity project | 6bb99b3 |
| October 21 | Spherical gravity, surface movement, jumping, and camera | a8bcf9d |
| October 21 | Two-planet transfers and respawn | 7e42ec1 |
| October 21 | Running and ballistic travel through finite fields | 90a902b |
| October 21 | Missed-jump defeat and explicit retry | 9437c63 |
| October 21 | Mobile controls, assisted camera, pause, and retry interface | c25c088 |
| October 21 | Earlier landing-based victory and fresh-run restart | f2a818a |
| October 21 | Android build blocker fix and readiness audit | bada0a1 |
| October 22 | Fixed horizontal Android launch and return | b1ac80a |
| October 22 | Collectible-star victory and consecutive three-jump travel | d336b51 |
| October 22 | Tutorial, feedback, and stricter lower-jump containment | d7025ef |
| October 22 | First Expedition and deferred phone checklist | b7ebdd9 |
| October 25 | Oxygen reserve and bar replace the space timer | 7f87eba |
| October 25 | Modular astronaut assets | 8c8ef8f |
| October 25 | Stranded superhero and oxygen-reactive cape | 6fafd06 |
| October 25 | Recorded First Expedition superhero Android APK | 47a272c |
| Later local update | Walking animation and cape motion | Current local files and hero-motion-final.log |

### Behaviors that changed during development

The current game should be understood through its latest implementation. Landing-based victory was replaced by collecting every star. The old survival countdown and distance-limit loss were replaced by oxygen depletion. Running alone no longer represents the escape requirement; the grounded three-jump chain controls escape. The plain capsule and then the astronaut were superseded by the superhero visual, while the player physics root was retained.

Older PR notes and logs remain useful historical records, but some describe these superseded rules. In particular, older route logs mention the previous timer or distance-based loss; the later oxygen validation records the replacement behavior.

## 14 Android build and package verification

### Latest recorded superhero artifact

The README records a successful October 25 development APK that launches First Expedition with three planets, six stars, the superhero, the oxygen HUD, and the oxygen-reactive cape. Its recorded size is 61,719,646 bytes, approximately 61.7 MB. The recorded destination is C:/Users/Artemis/OneDrive/Apps/game.apk. This is a historical output location, not a guarantee that the file is present at that path on this machine.

Logs/hero-android-build.log contains MOBILE_ANDROID_BUILD_PASSED. Logs/hero-android-apk-audit.json records the package identity and checks below.

| Recorded property | Value or result |
| --- | --- |
| Application identifier | com.gravitygame.prototype |
| Minimum SDK | 26 |
| Target SDK | 36 |
| Architecture | ARM64 |
| Backend | IL2CPP |
| Build type | Development and debuggable APK |
| Display | Fixed landscape |
| Signature check | Verified |
| ZIP alignment | 16 KB compatible |
| Native library alignment | All six audited ARM64 libraries have 16 KB LOAD alignment |

Recorded SHA-256:

`FFFD7F8B7FB3AAA557ECB29D1A8D0A0FF9F64B63FF61D5EB26B02AB17BF5C546`

The audit lists INTERNET and the application's internal signature-protected dynamic-receiver permission. It does not list camera, microphone, location, or storage permission. Package metadata and signature checks do not establish performance, crash behavior, real touch quality, release signing, or store acceptance.

### Reproducible prototype build and audit

MobileDemo.BuildAndroid configures a development ARM64 IL2CPP APK, OpenGL ES 3, target SDK 36, fixed landscape, the prototype identifier, and no custom keystore. It writes Builds/GravityGame-Prototype.apk and builds MobilePrototype. It is designed for an isolated project copy and requires the Android toolchain.

Tools/Verify-AndroidApk.ps1 checks the package identifier, target API, ARM64 presence, signature, ZIP alignment, and native library alignment. RequireFixedLandscape also rejects an APK that does not launch in the required orientation. RequireRelease rejects a debuggable package. These checks do not create a release AAB or complete signing and publication.

Earlier records cover the first mobile prototype, landing-victory version, landscape correction, star exploration, and tutorial/feedback APKs. Generated builds and Logs are ignored by Git, so their existence on one machine does not make them part of a clean repository checkout.

### Settings that still differ from the artifact

The main project still shows DefaultCompany, product name GravityGame, version 0.1.0, Android version code 1, and the template Android identifier com.UnityTechnologies.com.unity.template.urpblank. The default Android target SDK setting is Automatic, while the prototype builder pins 36. A release build needs an intentional identity, version, launch scene, and toolchain configuration rather than relying on the recorded prototype artifact.

## 15 Validation already performed

The repository contains custom Unity editor and play-mode validation entry points. Saved logs record successful runs of the following checks. This documentation task reviewed the evidence and implementation; it did not rerun Unity gameplay checks or rebuild an APK.

| Validation area | Recorded coverage | Main evidence |
| --- | --- | --- |
| Original physics | Scene wiring, six gravity directions, surface probes, center fallback, and six simulated landings | phase-one-verified.log and oxygen-validation.log |
| Two-planet travel | Running, ballistic crossings, field entry, landing, explicit recovery, camera clearance and reset | two-planet-verified.log and oxygen-validation.log |
| Jump chain | Grounded timing, pause, increasing heights, airborne tapping, running, and lower-jump containment | oxygen-validation.log |
| Tutorial and feedback | Actual movement and takeoffs, landing cues, skip and replay, paused effects, event cleanup, and sparkle lifecycle | tutorial-validation.log and oxygen-validation.log |
| First Expedition | Six directed real crossings, space gaps, free landings, and destination checkpoints | first-expedition-validation.log and oxygen-validation.log |
| Stars and replay | Real trigger pickups, arbitrary order, duplicate and paused-state guards, final-star victory, retry retention, and full reset | oxygen-validation.log |
| Oxygen | Drain, HUD width, warning, gravity refill, pause, defeat, retry, replay, and increased-capacity survival | oxygen-validation.log |
| Mobile UI | Joystick thresholds, pointer independence, touch priority, look dragging, pause, interruptions, safe area, and clock cleanup | oxygen-validation.log |
| Rendered play mode | Actual Canvas raycasts, saved-scene reload, and gameplay, pause, defeat, and victory previews | oxygen-validation.log |
| Character art | Prefab setup, collision dimensions, triangle budgets, cape color binding, and material isolation | character-assets.log and superhero-assets.log |
| Hero motion | Real joystick and physics movement, stride, heading, airborne settling, pause, retry, and cape mesh isolation | hero-motion-final.log |
| Android packaging | Successful build and fixed landscape, SDK, architecture, signature, and alignment audit | hero-android-build.log and hero-android-apk-audit.json |

The log files named above are under Logs. Saved markers include PHASE_ONE_VALIDATION_PASSED, TWO_PLANET_VALIDATION_PASSED, JUMP_CHAIN_VALIDATION_PASSED, TUTORIAL_FEEDBACK_VALIDATION_PASSED, FIRST_EXPEDITION_VALIDATION_PASSED, STAR_VALIDATION_PASSED, OXYGEN_VALIDATION_PASSED, MOBILE_VALIDATION_PASSED, MOBILE_PLAY_MODE_CHECKS_PASSED, and HERO_ANIMATION_PASSED.

The documented First Expedition stress coverage contains 72 lower-jump flights across three planets, six poles, two timestep sizes, and both lower stages. Checks include held running, large configured jump heights, and outward impulses. The oxygen suite also tests that increasing capacity survives beyond the previous time and distance loss limits.

### Running future checks

Use an isolated project copy where possible. Some validation entry points recreate demonstrations or upgrade saved scenes. Standard nonrendered checks include PhaseOneDemo.Validate, TwoPlanetDemo.ValidateAll, and MobileDemo.ValidateAll under GravityGame.Editor.

Rendered entry points include MobilePlayModeChecks.Run, RunFirstExpedition, and RunOxygen. These require graphics, manage their own completion, and should be launched without Unity's -quit argument. RunOxygen upgrades the mobile scenes before checking the authored expedition. HeroAnimationSetup.Apply performs the focused prefab and motion workflow. The existing README documents batch invocation and expected result markers.

### Limits of the evidence

The latest focused animation check does not replace a full regression run after all local edits. The October 25 packaging task repeated build and package checks, not the full gameplay suite. No recorded phone test establishes multi-touch feel, frame rate, thermal behavior, battery use, or device lifecycle correctness. A passing historical check is evidence for that tested version and environment.

## 16 Remaining work and practical handover

### Integration and packaging

Review and commit the local animation implementation together with its shared-prefab changes. The working tree also contains changes in rendering assets, package files, project settings, and build selection; these require deliberate review and should not all be assumed to belong to the animation feature. Rebuild the intended First Expedition package with the latest animation, then rerun appropriate gameplay checks and the package audit.

### Phone acceptance testing

PHONE_PLAYTEST_CHECKLIST.md already provides a detailed manual checklist. It covers fresh launch, tutorial skip and replay, analog walking and running, three-finger control, lower-jump containment, all planet routes, star collection, oxygen HUD and cape behavior, retry and replay, pause, backgrounding, lock and unlock, Android Back, safe areas, and longer sessions.

The animation-specific checks require a new APK because the recorded October 25 build predates them. Test the hero on poles and the underside, check shoulder attachment and cape visibility, and verify that pause and retry produce the intended poses. Record the APK version, phone model, Android version, reproduction steps, and recordings for any failures.

Measure CPU and GPU frame times, memory, garbage collection, heat, and battery use during sustained play. The 60 FPS target, simple scene geometry, and low polygon counts do not establish a stable frame rate. Review recurring HUD formatting and cape mesh updates if profiling identifies cost.

### Release preparation

The existing Android readiness audit identifies production work: choose the publisher-owned application identifier, replace template branding, prepare icons and store art, set release versioning, produce a nondevelopment AAB, configure owner-controlled signing, and complete publication requirements. Its store-policy references are historical and should be rechecked when preparing an actual submission.

The audit also recorded unresolved references in the shared volume profile and older desktop probe-debug resources. Rendering assets have since been modified locally, so inspect their present state in Unity before treating the historical findings as either still open or resolved. A new asset-reference scan and a clean build should establish the current position.

The recorded artifact has INTERNET permission, and the project includes engine and development services. Current privacy behavior requires a release-configuration and traffic review. The older audit's diagnostics observation should not be assumed to describe every current local setting. No privacy-policy implementation or completed store declaration is established by this project review.

### Product features that remain future work

The reviewed game code does not provide a persistent campaign save, a multi-level progression system, an oxygen-upgrade economy, a separate home-planet finale, accounts, multiplayer gameplay, advertisements, or purchases. iOS packaging and physical iOS testing are also not recorded. These are possible extensions, not completed features implied by installed packages or the story premise.

## 17 Source guide

All paths below are relative to the GravityGame project root. They let a future developer trace the report back to the actual implementation and saved evidence.

### Primary documentation

- README.md explains the latest gameplay, scenes, oxygen system, Android build receipt, and validation workflows.
- SUPERHERO.md describes the hero, cape color, local animation update, art menus, and focused checks.
- CHARACTER_ASSETS.md documents the earlier astronaut prefabs, dimensions, triangle counts, and alternative-art workflow.
- PHONE_PLAYTEST_CHECKLIST.md contains the deferred manual device checks and notes template.
- ANDROID_READINESS_AUDIT.md records the Android investigation, package fixes, historical findings, and release tasks.
- PHASE_ONE_PR.md, TWO_PLANET_PR.md, MOBILE_PROTOTYPE_PR.md, and TUTORIAL_FEEDBACK_PR.md describe earlier development stages. Some behavior has since been replaced.

### Gameplay and assets

- Assets/Scripts contains the thirteen runtime components listed in Section 12.
- Assets/Scenes contains the starter and four playable scenes listed in Section 2.
- Assets/Art/Hero contains hero meshes, materials, and shared prefabs.
- Assets/Art/Characters contains the retained astronaut alternatives.
- Assets/Materials and Assets/Settings hold scene materials and rendering configuration.
- Docs/Art/superhero-cape.png and Docs/Art/astronaut-assets.png are retained character presentation images.

### Tools and settings

- Assets/Editor contains the scene creators, art tools, and custom gameplay validations.
- Tools/Verify-AndroidApk.ps1 performs the Android package audit.
- Packages/manifest.json and packages-lock.json record dependencies.
- ProjectSettings/ProjectVersion.txt identifies the Unity version.
- ProjectSettings/EditorBuildSettings.asset records the default scene list.
- ProjectSettings/ProjectSettings.asset and UnityConnectSettings.asset record platform and service settings.
- Logs contains saved validation and build evidence on this working copy. These generated files are ignored by Git.

This report documents the current working project, its recorded development milestones, and the known boundary between implemented gameplay, historical verification, packaged Android content, and outstanding acceptance work.
