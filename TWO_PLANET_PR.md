# Add momentum-based planet travel and lost-in-space defeat

Players travel between blue Azure and orange Ember in `Assets/Scenes/TwoPlanets.unity` by holding either Shift key to run, then jumping after a run-up. E launch assistance is removed. Normal jumps remain in the source field; running jumps are higher and preserve tangential momentum. Each planet has a finite field with edge falloff, visible boundary rings, and a gap of space between fields. No gravity, steering, or motor braking changes velocity in space; entry into a new field automatically captures the player. Physical gravity pulls radially while character and camera orientation turn smoothly.

Landings update a checkpoint on the current planet. Leaving both fields starts a three-second countdown; its expiration or exceeding the distance limit causes persistent defeat. The character stays frozen in space, movement is disabled, and the mouse is released for the Lost in space panel. R or the Retry button explicitly returns to the last landing, clears momentum, and resets the camera. No automatic rescue occurs.

The original scenes and build scene list are preserved. The shared controller retains single-planet support. Planets use centered, uniformly scaled SphereColliders and are static in this demo.

Validation passed in Unity 6000.5.9f1 via `GravityGame.Editor.TwoPlanetDemo.ValidateAll`: original six-sided landings, both Shift keys, normal/run-up jumps, ballistic capture and landing, the grace countdown, persistent timeout/distance defeat without automatic teleport, blocked movement after defeat, explicit retry, and camera clearance/reset. Both success markers appear in ignored `Logs/lost-in-space-validation.log`. Keyboard/mouse feel, rendered camera transitions, and loss-panel interaction need hands-on Play mode review; see README.

Branch: `codex/lost-in-space`, based on the local running-jump feature branch. Target `main` to include all local gameplay work, or `codex/run-jump-gravity` to review this increment alone. GitHub publication remains pending authentication.
