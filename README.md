# InfiniteDust

WIP attempt to procedurally generate de_dust2 clones. Spatial visualisation nightmare.

Some notes on the scale, properties and proportions of DE_DUST2. Probably will be necessary at some point.

Playable space bounding box: 3840x4128 (okay so, let's say 4096x4096?)

Max ledge height (excluding overpass): 160

Metal crates are primarily found around sites, but there may also be a few in mid
Wooden crates are usually pressed up against walls. They range in size from 48x48 to 144x144.
Larger crates are mostly found as part of stacks of smaller crates, but small crates often exist on their own

Floor types include: road, sand, stone. Road and sand can be mixed freely, but stone may only be connected
to road or sand via steps (typically one or two)
Most paths are defined by the cobbled roads, which ALWAYS terminate in closed double-doors.

The double-door arch is a prefab, which may or may not contain doors. However, custom arches also exist throughout the map,
serving primarily as transitions between underground and overground spaces, or between corridors and chambers
Arches typically begin at 64 units above ground level, but may go up to 80. A special arch door prefab also exists in some walls,
purely as detail. It is usually placed 16 units above ground and given a step which juts out.

Ceilings are usually completely flat for the entirely of the underground space, unless two underground spaces are joined.

Underground spaces are lit with fluorescent bar light prefabs, placed on the wall. Their placement varies,
but it appears to favour the (horizontal) middle of the face, and can be anywhere from 'close to the ceiling'
to 'nearly eye-level'

Protruding corners are sometimes bevelled at 64 units, or (rarely) at 16
They may also be marked by towers, which protrude 32 units out from the wall, are slightly taller than the wall, and measure 128x128 to 192x192.

If one flat area is higher than another flat area, it will typically have a low trimmed wall of 16x32. In rare cases it may also be 16x16 or 32x32.

Walls *can* be perfectly straight for up to 1600 units, but tend to go 300-600 before being broken up by details or changing course.
If they reach their full height in the open air, they tend to have a 16x32 bevel on top, but may also recede in a 1:4 slope
after reaching a certain height (typically a safe 160 units above ground)

Sand may pile up in right-angle corners, producing slightly raised terrain. It may also pile up against a wall in a 4:1 slope.

Try to remember Hammer uses Z-up, won't you?
