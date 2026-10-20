# Add running jumps and momentum-driven travel between planet gravity fields

Players travel between blue Azure and orange Ember in `Assets/Scenes/TwoPlanets.unity` by holding either Shift key to run, then jumping after a run-up. E launch assistance is removed. Normal jumps remain in the source field; running jumps are higher and preserve tangential momentum. Each planet has a finite field with edge falloff, visible boundary rings, and a gap of space between fields. No gravity, steering, or motor braking changes velocity in space; entry into a new field automatically captures the player. Physical gravity pulls radially while character and camera orientation turn smoothly.

Landings update a checkpoint on the current planet. R resets there immediately; leaving both fields for three seconds or exceeding the distance limit also respawns the player. Respawning clears momentum and resets the camera. The on-screen panel shows gravity/space status and the new controls.

The original scenes and build scene list are preserved. The shared controller retains single-planet support. Planets use centered, uniformly scaled SphereColliders and are static in this demo.

Validation passed in Unity 6000.5.9f1 via `GravityGame.Editor.TwoPlanetDemo.ValidateAll`: original six-sided landings, both Shift keys through an Input System keyboard, walk/run speed, normal/run-up jumps, a ballistic crossing with all keys released, zero-gravity momentum, automatic capture, checkpoints, recovery, and camera clearance/reset. The successful crossing used no respawn or injected launch velocity. Both validation success markers are in ignored `Logs/run-jump-verified.log`. Keyboard/mouse feel and rendered camera transitions need hands-on Play mode review; see README.

Branch: `codex/run-jump-gravity`, based on the local two-planet feature branch. Target `main` to include all local gameplay work, or `codex/two-planet-demo` to review this increment alone. GitHub publication remains pending authentication.
