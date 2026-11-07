# Imported pixel art — 2026-11-05

Author: ansimuz. Both official project pages list CC0 1.0 Universal.

| Pack | Official source | Free file downloaded | Integrated |
|---|---|---|---|
| Warped City | https://ansimuz.itch.io/warped-city | Warped City Assets Files, 2,743,743 bytes | Metal tiles, machinery, signs and doorway |
| Synth Cities / Cyberpunk Street | https://ansimuz.itch.io/cyberpunk-street-environment | cyberpunk-street-files.zip, 16,736,056 bytes | Three night-city parallax layers from Version 1 |

No paid add-ons were downloaded. No executable demo or external package was imported into Unity. Original archives remain in the task workspace under `day-two/downloads/`; the Unity project contains selected PNG assets in `Assets/ThirdParty/Ansimuz/` and the Warped City public-license PDF.

The original cat artwork and animations remain in use. Rats retain their artwork and behaviour, with their prefab scale reduced from 0.72 to 0.55. The full Warped City archive also contains animated enemies for a later design decision; they are not gameplay-ready NPC replacements.

Imports use 16 pixels per unit, Point filtering, no mipmaps, no texture compression and Full Rect sprite meshes. Tile slicing is stored by the Unity Sprite Editor provider, preserving sprite IDs on re-run. The backdrop is intentionally darker than actors and walkable edges. Decoration creates no solid colliders; authored room colliders define traversal, with the pre-existing bridge spike hazard retained.
