# Room GS surface repair plan — 2026-11-01

Goal: user-approved simple surface infill for broken walls/wardrobe seen in three supplied screenshots; preserve door, bed, shelves and room layout.
Architecture: reversible child group of local opaque surfaces and narrowly scoped GaussianCutout volumes. Preserve raw PLY, cooked payload and theme maps. Prototype in restored project first while user's Editor remains open. Existing game/input changes are outside this unit.

- [x] Prototype warm off-white wall panels and muted wood wardrobe side; avoid blanket removal of furniture or replacement of door/switch.
- [x] Capture paired views from same cameras before/after; inspect panel/GS occlusion and retained landmarks. Placement is provisional until images verify it.
- [x] Add repeatable editor authoring, output preview evidence, preserve originals and existing repair-off option.
- [x] Validate actual Room flow/render cleanup and Windows build; independent review, commit/push/draft PR after verified result. Hardware visual acceptance remains separate.

Ruling: do not run RoomRealitySetup.Configure on the user's active scene. Work in D:/VRFPS-Reproduction/room-walk-0088570 for previews first; later apply only repair group to the existing Room scene. Approximate surfaces are authored fill, not recovered scan detail. No raw Gaussian deletion or theme-map edits.
