# Astronaut prototype assets

The active player is now the stranded superhero described in [SUPERHERO.md](SUPERHERO.md). These astronaut assets remain available as alternatives; their existing scene instances are retained inactive.

The character assets live in `Assets/Art/Characters`. They use shared low-poly meshes and solid-color URP materials, with a two-unit height, local Y up and local +Z forward.

![Astronaut suits and oxygen backpack](Docs/Art/astronaut-assets.png)

| Prefab | Use |
| --- | --- |
| `Prefabs/AstronautExplorer.prefab` | Default white suit with safety-orange trim, dark visor and oxygen backpack. |
| `Prefabs/AstronautScout.prefab` | Blue suit with cyan trim; shares the helmet, backpack and geometry. |
| `Prefabs/AstronautHelmet.prefab` | Separate helmet, gold visor rim, dark visor and radio hardware. Pivot at helmet center. |
| `Prefabs/OxygenBackpack.prefab` | Separate backpack with twin oxygen cylinders. Pivot at pack center; tanks face local -Z. |

The Explorer was the default **Astronaut Visual** beneath Player in MobilePrototype, FirstExpedition, TwoPlanets and PhaseOne. That child is now inactive while **Hero Visual** is active. The original capsule mesh renderer is disabled. The existing root capsule collider, Rigidbody, movement, gravity and camera references are retained. The visual prefabs contain no colliders or Rigidbody components. These are static prototype models, ready for later animation work.

To switch suits, replace the Astronaut Visual child with the Scout prefab, keep its local position/rotation zero and local scale one, and name it Astronaut Visual. Keep the player root and its gameplay components. Helmet and backpack are nested prefabs, so edits to those assets carry into both suits. Material assets provide the suit, accent, visor, trim and tank colors.

**GravityGame > Art > Create Astronaut Assets and Apply** creates any missing assets, activates the Explorer in the four scenes and deactivates the hero. It asks to save open scene edits first. Existing art assets and an existing Astronaut Visual child are reused, so rerunning it does not overwrite subsequent art edits. Current desktop/mobile demo creators use the superhero by default; the astronaut menu remains an explicit alternative.

The batch entry point is `GravityGame.Editor.CharacterAssets.CreateApplyAndPreview`. Run in an isolated project copy with graphics enabled and `-quit`; it compiles, checks prefab setup/triangle budgets and player collision dimensions, applies the visuals, and renders `Logs/astronaut-assets.png`. It does not run the full gameplay suite. Generated caches, logs and previews are ignored by Git.

Verified October 25, 2026 in Unity 6000.5.9f1: compilation and setup passed, exit 0 (`Logs/character-assets.log`). Each full suit has 2,100 triangles; the helmet has 516 and the backpack 312. The saved prefab transforms and front/back studio render were reviewed. The documentation preview above is retained in Git; the scenes have not been packaged into a new APK yet.
