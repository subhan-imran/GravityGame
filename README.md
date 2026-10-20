# GravityGame — Two Planet Demo

Unity **6000.5.9f1**, Universal Render Pipeline, Input System.

Open `Assets/Scenes/TwoPlanets.unity` and press Play. You start on blue Azure, facing orange Ember. **Hold Shift + W** to run toward Ember, then press **Space** after a short run-up (about one second). A running jump can clear Azure's gravity boundary; keep your takeoff momentum through space to enter Ember's field and land. **E no longer launches or switches planets.** WASD/arrows move, either Shift key runs, Space jumps, and the mouse orbits. **R** respawns at your last safe landing. Escape releases the pointer; click to capture it again.

Thin rings mark each planet's gravity boundary, six units above its surface. Gravity fades over the outer 1.5 units and is zero outside. The fields are separated by a gap of space. A normal jump stays inside the field; a running jump needs both Shift and actual movement speed built up on the ground. Running preserves takeoff speed, while releasing movement in the air adds no motor braking. In space, neither gravity nor movement input changes your velocity. Entering another field applies its radial pull automatically; player orientation and the camera turn smoothly to follow. Overlapping fields in custom scenes retain a three-unit switching margin.

Leaving every field for three seconds, or drifting more than 120 units beyond the nearest surface, respawns the player. Safe landings update a checkpoint relative to that planet; respawning clears momentum and resets the camera immediately. The on-screen panel shows whether you are in a planet's field or coasting in space. Camera obstruction checks include both planets.

Use **GravityGame > Create Two Planet Demo** to recreate the scene. Materials live in `Assets/Materials/TwoPlanets*.mat`. The original PhaseOne and SampleScene files and build scene list are preserved; add TwoPlanets to your build profile to build this demo.

Validation: run Unity on a closed or temporary copy with `-batchmode -nographics -projectPath <project> -executeMethod GravityGame.Editor.TwoPlanetDemo.ValidateAll -quit -logFile <log>`. It checks the original single-planet demo, then uses a temporary Input System keyboard to test both Shift keys, walking/running speed, normal and running jumps, an unassisted crossing after releasing all keys, zero-gravity momentum, field entry, landing, checkpoints, respawning, and camera clearance/reset. Look for both `PHASE_ONE_VALIDATION_PASSED` and `TWO_PLANET_VALIDATION_PASSED`.

Play mode checklist: compare walking and running with either Shift key; jump without a run-up and confirm you fall back; run toward Ember and jump past the rings, then release movement to coast. Check the status changes to space before Ember captures you. Walk around both planets and jump across their poles; orbit during flight; press R while moving or airborne. Confirm checkpoints follow your latest landing and a missed jump eventually respawns you. Keyboard/mouse feel and rendered camera transitions still require hands-on review.

## Original Phase 1 demo

`Assets/Scenes/PhaseOne.unity` remains available for the original single-planet movement demo.

`PlanetGravity` supplies constant radial acceleration about its transform. `SphericalPlayer` applies gravity in physics steps, aligns a Rigidbody capsule to local up, accelerates along the tangent plane, and probes the assigned planet for grounded jumping. Diagonal input is normalized and airborne steering is reduced. `PlanetCamera` transports its heading with local up, smooths its following position, limits pitch, and prevents the camera from crossing the planet surface.

Assign the starting planet and movement camera in the player Inspector; populate `gravitySources` for multiple planets. Each source uses a centered SphereCollider with uniform scale; the player uses a unit-scale Y-axis capsule. Tune gravity acceleration, influence height, edge falloff, walk/run speeds, normal/running jump heights, air control, gravity turn speed, respawn limits, camera distance, and smoothing in the Inspector. Planets are static in this demo. Empty source lists retain single-planet behavior.

Use **GravityGame > Create Phase 1 Demo** to recreate the demo scene. This prompts to save open scene changes. The original SampleScene and build scene list are preserved; add PhaseOne to a build profile when building the demo.

Manual checks: walk a complete circuit through both poles and the underside; release movement to stop; compare diagonal and straight speed; jump, land, and hold Space to confirm no automatic repeated jump; orbit near the horizon; release and recapture the pointer. Confirm the camera stays outside the planet and the Console has no errors.

Automated validation: with the project closed in the editor, run Unity with `-batchmode -nographics -projectPath <project> -executeMethod GravityGame.Editor.PhaseOneDemo.Validate -quit -logFile <log>`. It recreates PhaseOne, checks scene references and gravity at all six cardinal directions, tests surface probes and the center fallback, and simulates six player drops to confirm grounding. Look for `PHASE_ONE_VALIDATION_PASSED`. Run this on a temporary copy if the project is already open. These checks do not replace the manual input and camera checks above.
