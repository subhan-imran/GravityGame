# Add a mobile prototype with touch movement and assisted camera

The new `Assets/Scenes/MobilePrototype.unity` scene replaces keyboard-only play with a left joystick, automatic running near the stick's outer edge, a touch-down Jump button, and a right-side drag area. Independent pointer ownership allows movement, jump, and camera touches together. Keyboard controls remain available for editor testing.

The assisted camera follows forward movement and frames a planet ahead during flight; manual drag temporarily takes priority. Physics-based jumping, finite gravity fields, and persistent lost-in-space defeat remain unchanged.

A scaling, safe-area-aware Canvas replaces the prototype text panels. Large pause/resume and retry menus clear held input; app interruptions pause without automatically resuming. Pause owns/restores its clock setting, including when the scene is closed. Runtime mobile settings allow both landscape directions and target 60 FPS. The desktop scenes, build scene list, and package dependencies are preserved.

Validation passed in a graphics-enabled Play mode run: actual rendered jump-versus-look raycast priority, joystick walk/run, independent movement/jump pointers, drag look, pause/resume, focus interruption, safe-area anchors, defeat/retry, clock cleanup, and saved-scene reloading. Gameplay, pause and loss screenshots were rendered and inspected. Desktop physics regressions also passed during the mobile implementation. Repeat with `GravityGame.Editor.MobileDemo.ValidateAll` (headless authored ordering) and `GravityGame.Editor.MobilePlayModeChecks.Run` (rendered routing); the latter exits itself and must not be given `-quit`. The batch editor also logs a Unity Search startup exception before tests; the test run completes with success markers and exit code 0. Real-device multi-touch, platform lifecycle, sustained performance and readability still need hands-on review before release.

Branch: `codex/mobile-prototype`, based on `codex/lost-in-space`. GitHub publication remains pending authentication.

Android build note: the existing experimental live-reload package (`com.unity.pipeline` 0.9.0-exp.1) calls a desktop-only subtarget API on Android. The test build uses a temporary copy with that unused dependency excluded; the main manifest and package lock remain unchanged. The build entry point targets ARM64, OpenGL ES 3 and SDK 36 with debug signing. See README to reproduce.

Android development packaging completed with `MOBILE_ANDROID_BUILD_PASSED` and exit code 0. The local ignored artifact is `Builds/MobilePrototype/GravityGame-Prototype.apk` (43,039,737 bytes). Android tooling confirms package `com.gravitygame.prototype`, minimum SDK 26, target SDK 36, ARM64, and a valid APK v2 signature. Installation and physical-device play have not been tested.
