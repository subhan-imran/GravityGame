# Add a playable two-planet gravity transfer demo

Players can now travel between blue Azure and orange Ember in `Assets/Scenes/TwoPlanets.unity`. E launches toward the other planet from its facing hemisphere. Automatic airborne capture uses surface distance with a switching margin, and launch locking prevents immediate reversal. Gravity and player orientation turn gradually; the camera carries its heading through the turn, smooths its roll, and checks obstruction against both planets.

Landings update a checkpoint on the current planet. R resets there immediately; leaving both gravity fields for three seconds or exceeding the distance limit also respawns the player. Respawning clears momentum and resets the camera. On-screen controls and distinct planet/player materials make the demo ready to try.

The original scenes and build scene list are preserved. The shared controller retains single-planet support. Planets use centered, uniformly scaled SphereColliders and are static in this demo.

Validation passed: Unity 6000.5.9f1 batch simulation via `GravityGame.Editor.TwoPlanetDemo.ValidateAll`, covering the original six-sided landings, two-way transfers, bounded gravity turns, switching hysteresis, automatic capture, checkpoints, timeout/distance recovery, camera clearance throughout travel, and camera reset between physics steps. Both validation success markers are in `Logs/two-planet-verified.log` (ignored). Keyboard/mouse feel and rendered camera transitions need hands-on Play mode review; see README.

Branch: `codex/two-planet-demo`, based on the local Phase 1 feature branch. Target `main` to include both phases, or the Phase 1 branch to review this increment alone. GitHub publication remains pending authentication.
