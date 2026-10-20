# GravityGame — Phase 1

Unity **6000.5.9f1**, Universal Render Pipeline, Input System.

Open `Assets/Scenes/PhaseOne.unity` and press Play. WASD or arrow keys move relative to the camera, Space jumps, and the mouse orbits. Escape releases the pointer; click to capture it again.

`PlanetGravity` supplies constant radial acceleration about its transform. `SphericalPlayer` applies gravity in physics steps, aligns a Rigidbody capsule to local up, accelerates along the tangent plane, and probes the assigned planet for grounded jumping. Diagonal input is normalized and airborne steering is reduced. `PlanetCamera` transports its heading with local up, smooths its following position, limits pitch, and prevents the camera from crossing the planet surface.

Assign the planet and movement camera in the player Inspector. The planet needs a collider; the player uses a unit-scale Y-axis capsule. Tune acceleration, speed, jump height, air control, camera distance, and smoothing in the Inspector. Gravity currently targets one assigned planet; switching planets is outside Phase 1.

Use **GravityGame > Create Phase 1 Demo** to recreate the demo scene. This prompts to save open scene changes. The original SampleScene and build scene list are preserved; add PhaseOne to a build profile when building the demo.

Manual checks: walk a complete circuit through both poles and the underside; release movement to stop; compare diagonal and straight speed; jump, land, and hold Space to confirm no automatic repeated jump; orbit near the horizon; release and recapture the pointer. Confirm the camera stays outside the planet and the Console has no errors.

Automated validation: with the project closed in the editor, run Unity with `-batchmode -nographics -projectPath <project> -executeMethod GravityGame.Editor.PhaseOneDemo.Validate -quit -logFile <log>`. It recreates PhaseOne, checks scene references and gravity at all six cardinal directions, tests surface probes and the center fallback, and simulates six player drops to confirm grounding. Look for `PHASE_ONE_VALIDATION_PASSED`. Run this on a temporary copy if the project is already open. These checks do not replace the manual input and camera checks above.
