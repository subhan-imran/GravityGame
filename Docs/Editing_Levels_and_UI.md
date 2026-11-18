# Editing levels and UI in Unity

The saved scenes are the source of level content. Open a scene, edit its objects and Inspector values, save, and press Play. You do not need to run a creation script to apply changes.

## Where to start

Open `Assets/Scenes/FirstExpedition.unity` for the three-planet level, or `MobilePrototype.unity` for the two-planet level. Each playable scene has a **Level** root with a **Level Authoring** component. It groups the planets, player rig, star objective, interface, and lighting. The desktop scenes use the same organization and retain their free-play behavior.

The Level Inspector exposes the starting planet, player, camera, star objective, and optional mobile interface. **Refresh Level References** updates the player gravity-source list, star list, camera links, and touch bindings. **Validate Level** reports missing or inconsistent links. Reference discovery also runs before gameplay, so duplicating or deleting active planets and pickups beneath Level updates the objective automatically. Objects outside this Level and inactive children are excluded.

Set the player's saved Transform to the desired start position as well as assigning Starting Planet. Changing the starting planet alone does not teleport the saved start position to it. Keep the Level and Player Rig roots at unit scale.

## Reusable prefabs

Reusable game objects are in `Assets/Prefabs/Gameplay`:

- `PlanetAzure`, `PlanetEmber`, and `PlanetVerdant`: planet mesh, material, collider, radial gravity, boundary rings, and grouped landmarks.
- `MobilePlayerRig` and `DesktopPlayerRig`: the player physics root with the existing nested hero/cape prefabs, plus the camera. Movement and camera fields remain editable on their components.
- `CollectibleStar`: visual mesh, material, pickup collider, collection behavior, and surface anchor.
- `SurfaceLandmark`: the small surface marker used in the prototype planets.

The original single-planet scene also supplies its own planet prefab. `Assets/Prefabs/UI/MobileInterface.prefab` contains the full Canvas, controls, panels, tutorial, feedback, and EventSystem. Scene references are bound by Level Authoring; reusable prefab assets do not depend on a particular saved scene.

Edit a prefab in Prefab Mode to change its shared appearance or components. Edit a scene instance to make a level-specific override. Apply selected overrides deliberately if you want the changes shared. Avoid applying a scene's player/planet/objective references to a prefab asset.

## Move or resize planets

1. Select the planet beneath Level / Planets.
2. Move it with the standard Scene tools. Change its name in the Hierarchy to change the name shown by the HUD.
3. Edit acceleration, gravity influence height, and edge falloff in Planet Gravity.
4. Change **Surface Radius** in its Inspector, or adjust uniform Transform scale. Keep all three scale axes equal.
5. Edit the MeshRenderer material for color and surface appearance.

Gravity Boundary updates its ring radius after changes to the planet radius or influence height. Ring visibility, materials, segment count, and width are editable. Landmarks are children of the planet, so they move and scale with it.

Stars with Planet Surface Anchor enabled follow their assigned planet's movement, rotation, and radius while retaining their height above the surface. Their root objects stay beneath the objective so the hierarchy is easy to scan. Travel between moving planets is not a newly added gameplay mechanic; this anchoring supports level layout editing.

## Add or edit stars

Duplicate an existing star in the Hierarchy, or drag CollectibleStar from the Project window beneath Level / Collect all stars. Assign its **Planet**. Move it in the Scene view, then use **Snap Selected Stars To Their Planet** in the Collectible Star Inspector. The default height is 1.35 world units above the surface.

The Planet Surface Anchor component also exposes a direction relative to the planet and height above the surface. Edit those values to position the pickup numerically. Dragging an existing star records a new direction and height; the snap button restores a consistent pickup altitude. Keep star roots at unit world scale for consistent pickup size. To make a free-positioned pickup that does not follow a planet, turn off Follow Planet.

After adding, removing, or disabling pickups, select Level and refresh references to preview the registered star count. Collection references are assigned automatically. Every registered star is required for victory; duplicating one increases the objective count. Duplicated stars placed on top of each other are still separate objectives, so place them deliberately.

## Edit player and camera settings

Expand Player Rig and select Player. Spherical Player exposes walking/running speed, acceleration, jump heights, chain timing, air control, gravity alignment, and oxygen capacity. The existing physics rules remain: the first two jumps stay bound to the source planet and the third can escape.

Select Main Camera to edit orbit distance, smoothing, pitch, sensitivity, field of view, and assistance. Initializing the mobile interface no longer forces the camera assistance setting back on. HeroLocomotion, CapeFlutter, and OxygenCape remain in the nested character prefabs.

## Edit the interface visually

Open the MobileInterface prefab in Prefab Mode, or expand the scene's Mobile Interface / Mobile Canvas / Safe Area. Use normal RectTransform, Text, Image, Button, CanvasScaler, and material fields to edit the interface.

- Change button labels, game title, pause/loss/victory headings and descriptions directly on their Text components.
- Move and resize the joystick, jump button, status cards, tutorial card, and menus with RectTransform.
- Change fonts and font sizes on Text components; initialization no longer resets the hint font size.
- Toggle menu overlays temporarily in Edit mode to inspect them, then return to the intended saved state. Gameplay controls panel visibility at runtime.
- Keep the Safe Area wrapper: it intentionally adapts its anchors to phone cutouts. Control positions inside it remain authored.
- Keep control images raycastable and decorative text/images nonraycastable so touch input reaches the intended button.

**Authored Layout** is enabled on migrated interfaces. It prevents the old BuildUI helpers from rebuilding or repositioning the saved interface. Runtime initialization uses the assigned references and reports missing required bindings rather than replacing your layout. **Fit Tutorial To Safe Area** is off by default in the migrated scenes; enable it only if you want the original automatic tutorial-width behavior to override the tutorial card's size.

Some values are dynamic during play. The oxygen fill and percentage, star counter, jump stage, tutorial content, and feedback messages change with gameplay. Editing their displayed Text value alone will not replace their dynamic templates.

## Edit dynamic text and colors

Select **Settings** on Mobile Game UI, or open `Assets/Settings/DefaultGameUI.asset`. It holds jump colors, oxygen colors and warning thresholds, gravity labels, star-counter format, jump/hint messages, and tutorial text. The Inspector button **Select Dynamic UI Settings** selects this asset.

Supported HUD tokens are `{planet}`, `{next}`, `{stage}`, `{remaining}`, `{count}`, and `{total}`. Tutorial timing messages support `{remaining}` and `{window}`. Preserve the tokens you want to update with gameplay. Text without a token is shown literally. Set Critical Threshold at or below Low Threshold.

Duplicate the UI Settings asset and assign the copy to a scene's interface for level-specific wording or colors. Shared settings affect both mobile levels; a scene-specific copy affects only that assignment. For feedback toasts and landing colors, edit Mobile Game Feedback's Messages And Cue Colors fields on the interface. Static panel text remains on the saved Canvas.

## Make another level

Use **GravityGame > Authoring > Duplicate Current Level**, choose a new filename, and edit the duplicate. Prefab connections and scene bindings are retained; the original scene is preserved. Alternatively, duplicate a scene asset in the Project window.

Set the new level's display name, move/duplicate/delete planets, place stars, position the player, and choose its starting planet. Refresh and validate the Level, save the scene, then press Play. Add it to the intended Build Profile when you want it packaged. The Android build helper now uses the enabled saved scenes instead of recreating MobilePrototype.

The player and UI prefabs can also be dragged into a new scene, with a Level Authoring root used to connect them. An empty new scene still needs its own lighting, starting planet, correctly positioned player, and StarCollection objective if it is meant to use the star victory loop. Duplicating an existing authored scene is the quickest complete starting point.

## Legacy tools and migration

The original scene creation menus are under **GravityGame > Legacy Prototypes**. They remain available for reproducible prototype tests and can replace authored scene content. They are not part of the normal editing workflow. Do not run them to apply an Inspector change.

**GravityGame > Authoring > Organize Saved Scenes Into Prefabs** performs the one-time conversion. It keeps existing transforms, physics settings, scene object identities, art assets, and scene GUIDs. Already organized scenes are validated and skipped on later runs, so editing their prefabs is safe from migration regeneration. New reusable assets are created only if absent.

## Verification

`GravityGame.Editor.AuthoringChecks.Run` checks prefab references, preservation of custom static UI text and control positions through initialization and saved-scene reload, custom dynamic settings, discovery after adding/removing planets and stars, surface anchoring, and boundary resizing. `GravityGame.Editor.AuthoringChecks.RunPhysics` runs the existing physics and UI-state checks on the saved First Expedition scene without recreating it. `GravityGame.Editor.MobilePlayModeChecks.RunAuthored` is the rendered Play mode entry point; it exits itself and must be launched without `-quit`. Run any scene-generating legacy checks on an isolated project copy.

Verified on 19 November 2026 in Unity 6000.6.7f1 using a temporary project copy:

- Four saved scenes migrated successfully with geometry, UI layout, scene GUIDs, and shared nested hero prefab links retained.
- Nine reusable prefabs generated; prefab assets contain no scene references.
- Authoring checks passed, including custom text, control positions, settings round trips, hierarchy additions/removals, ring resizing, and pickup anchoring.
- Saved-level edit-mode physics and UI-state checks passed, including the six directed planet routes, jump-chain stress checks, tutorial/feedback, oxygen, pickup guards, checkpoint recovery, pause/interruption handling, and replay.
- The rendered Play mode attempt did not complete while Unity was refreshing and indexing the temporary project. Its successful authoring portion does not establish a completed rendered Play mode result. No Android build or phone test was performed for this refactor.

Evidence is in `Logs/prefab-authoring-layout.log`, `Logs/prefab-authoring-playchecks.log`, and `Logs/prefab-authoring-physics-final.log`. The original saved scenes and their metadata were backed up under `Logs/AuthoringOriginalScenes` before the converted scenes were copied into the main project. Logs and these backups are local, ignored outputs.

This change reorganizes authoring. It does not add campaign progression or persistent run saves.
