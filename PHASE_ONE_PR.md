# Add Phase 1 spherical gravity, surface movement, and smooth camera

Target: `main`  
Branch: `codex/phase-1-spherical-gravity`

The starter project had no player or gravity implementation. This change adds constant gravity toward an assigned planet, camera-relative movement on its tangent plane, local-up Rigidbody alignment, grounded jumping with a short input buffer, and reduced airborne steering. A smooth mouse-orbit camera carries its heading around the sphere and clamps against the planet to avoid clipping through it.

Open `Assets/Scenes/PhaseOne.unity` to try the separate playable demo. WASD/arrows move, Space jumps, mouse orbits, Escape releases the pointer, and clicking captures it again. Inspector fields expose movement and camera tuning. The original SampleScene, package dependencies, and build scene list are preserved.

Validation passed in Unity 6000.5.9f1 on an isolated project copy: compilation, scene wiring, six gravity directions, surface probes, the gravity-center fallback, and six simulated landings. The batch entry point is `GravityGame.Editor.PhaseOneDemo.Validate`; the log contains `PHASE_ONE_VALIDATION_PASSED`. Whitespace checks also passed. See README for the command and manual checklist.

Hands-on Play mode review is still required for input responsiveness, full circuits across both poles, jumping, and camera feel. Phase 1 supports one assigned spherical planet and a unit-scale Y-axis player capsule.

GitHub publication: local Git push authentication was unavailable; publish the feature branch after signing in, then create a pull request with this title and description targeting `main`.
