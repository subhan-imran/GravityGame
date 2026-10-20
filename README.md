# GravityGame — Two Planet Demo

Unity **6000.5.9f1**, Universal Render Pipeline, Input System.

Open `Assets/Scenes/TwoPlanets.unity` and press Play. You start on blue Azure, facing orange Ember. Press **E** to launch to Ember, wait to land, then press **E** again to return. Transfers work from the hemisphere facing the other planet. WASD or arrow keys move, Space jumps, and the mouse orbits. **R** respawns at your last safe landing. Escape releases the pointer; click to capture it again. The in-game panel repeats these controls.

Gravity captures an airborne player when another planet's surface is at least three units closer. A one-second launch lock prevents an immediate switch back. Player orientation and gravity direction turn at a bounded rate; the camera follows that turn with its own roll and position smoothing. Camera obstruction checks include both planets. Leaving every gravity field for three seconds, or drifting more than 120 units beyond the nearest surface, respawns the player. Safe landings update a checkpoint relative to that planet; respawning clears momentum and resets the camera immediately.

Use **GravityGame > Create Two Planet Demo** to recreate the scene. Materials live in `Assets/Materials/TwoPlanets*.mat`. The original PhaseOne and SampleScene files and build scene list are preserved; add TwoPlanets to your build profile to build this demo.

Validation: run Unity on a closed or temporary copy with `-batchmode -nographics -projectPath <project> -executeMethod GravityGame.Editor.TwoPlanetDemo.ValidateAll -quit -logFile <log>`. It checks the original single-planet demo, generates the new demo, simulates outbound and return landings, checks the rotation limit, switching margin and automatic capture, and verifies checkpoint, timeout/distance respawning, finite camera transforms, camera reset, and clearance from both planets throughout travel. Look for both `PHASE_ONE_VALIDATION_PASSED` and `TWO_PLANET_VALIDATION_PASSED`.

Play mode checklist: perform a round trip with E; walk around both planets and jump across their poles; try E from an outward-facing side (it should refuse to launch through the planet); orbit during transfer; press R while moving or airborne; confirm checkpoints follow your most recent landing. To try automatic rescue, temporarily move the player beyond both fields during Play mode. Keyboard/mouse feel and rendered camera transitions still require hands-on review.

## Original Phase 1 demo

`Assets/Scenes/PhaseOne.unity` remains available for the original single-planet movement demo.

`PlanetGravity` supplies constant radial acceleration about its transform. `SphericalPlayer` applies gravity in physics steps, aligns a Rigidbody capsule to local up, accelerates along the tangent plane, and probes the assigned planet for grounded jumping. Diagonal input is normalized and airborne steering is reduced. `PlanetCamera` transports its heading with local up, smooths its following position, limits pitch, and prevents the camera from crossing the planet surface.

Assign the starting planet and movement camera in the player Inspector; populate `gravitySources` for multiple planets. Each source uses a centered SphereCollider with uniform scale; the player uses a unit-scale Y-axis capsule. Tune acceleration, influence height, speed, jump height, air control, gravity turn speed, transfer speed, respawn limits, camera distance, and smoothing in the Inspector. Planets are static in this demo. Empty source lists retain single-planet behavior.

Use **GravityGame > Create Phase 1 Demo** to recreate the demo scene. This prompts to save open scene changes. The original SampleScene and build scene list are preserved; add PhaseOne to a build profile when building the demo.

Manual checks: walk a complete circuit through both poles and the underside; release movement to stop; compare diagonal and straight speed; jump, land, and hold Space to confirm no automatic repeated jump; orbit near the horizon; release and recapture the pointer. Confirm the camera stays outside the planet and the Console has no errors.

Automated validation: with the project closed in the editor, run Unity with `-batchmode -nographics -projectPath <project> -executeMethod GravityGame.Editor.PhaseOneDemo.Validate -quit -logFile <log>`. It recreates PhaseOne, checks scene references and gravity at all six cardinal directions, tests surface probes and the center fallback, and simulates six player drops to confirm grounding. Look for `PHASE_ONE_VALIDATION_PASSED`. Run this on a temporary copy if the project is already open. These checks do not replace the manual input and camera checks above.
