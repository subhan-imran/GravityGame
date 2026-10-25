# Stranded superhero

![Hero and oxygen cape states](Docs/Art/superhero-cape.png)

The player is a superhero lost in space after losing his ability to fly. He must jump between planets and chart a route home. The current star objective represents finding that route: collecting every star still completes the level, with **Route home found!** as the victory message. The existing levels are legs of the journey, rather than a new home-planet ending.

The default visual is `Assets/Art/Hero/Prefabs/StrandedHero.prefab`: a masked, low-poly hero with a midnight suit, gold gloves/boots, a four-point home-star emblem and a blue cape. `Prefabs/OxygenCape.prefab` is also reusable separately. The model keeps the existing two-unit player scale. The previous astronaut assets are retained, and their existing scene instances are inactive.

The cape is a sculpted, double-sided mesh. Its `OxygenCape` component reads the player's actual `OxygenFraction` and blends continuously:

| Oxygen | Cape |
| --- | --- |
| Full | Blue |
| Partly depleted | Blue blends through purple toward red |
| Empty | Red |

Entering gravity or retrying refills oxygen and restores blue. Pause holds the color because oxygen use is paused. Increasing `oxygenCapacity` automatically extends the color transition over the larger reserve. The color is applied per renderer through a MaterialPropertyBlock, with a small matching emission contribution for visibility in shadow; the shared cape material stays blue. No second survival timer is introduced.

The hero is used in MobilePrototype, FirstExpedition, TwoPlanets and PhaseOne. Their player roots, movement, collisions, camera and three-jump escape rule remain. The tutorial introduces lost flight, the journey home and the blue/red cape cue. Stars can still be collected in any order and landing remains free.

Use **GravityGame > Art > Create Stranded Hero and Apply** to create missing assets and apply the hero. Existing art assets are reused. The batch entry point `GravityGame.Editor.SuperheroAssets.CreateApplyAndPreview` compiles, performs a short oxygen/cape binding check, and renders `Logs/superhero-cape.png`. It does not invoke the full gameplay suite.

Verified October 25, 2026 with Unity 6000.5.9f1: compilation, scene setup and the focused oxygen/color check passed, exit 0 (`Logs/superhero-assets.log`). The hero is 2,168 triangles. The front/back and blue/purple/red preview was visually reviewed. The existing scene object identities were retained. The October 25 First Expedition APK includes this update. See README.md for the verified package receipt.
