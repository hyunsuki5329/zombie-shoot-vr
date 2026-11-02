# Room mirror and FBX wardrobe implementation plan

Goal: Apply the user-requested horizontal GS reflection and reuse the original FBX wardrobe texture/geometry, preserving edited theme maps and raw captures.
Architecture: Reflect the GS renderer under a positive calibrated data container; reflect collision positions, wall panels/cutouts, observation and virus positions consistently. Extract only wardrobe triangles/UVs from verified local Room.fbx into private generated assets, rigidly align to reflected GS. Keep raw FBX/PLY and theme scenes unchanged.
User decision: mirror GS and use FBX wardrobe, explicitly requested 2026-11-02 after reference comparison. No new approval required.

- [x] Capture failing private-asset orientation/wardrobe contract test.
- [x] Author reversible batch migration with backup/calibration/source checks; preview isolated project and adjust wardrobe fit.
- [x] Validate reflected landmark, positive player anchor, collision consistency, textured mesh, renderer lifecycle, Room/Classroom transition regression.
- [x] Inspect images; package separate build and local restoration overlay; retain VR visual acceptance as pending.
- [x] Independent review and targeted commit completed; draft PR updated after validation. Merge remains gated on latest VR acceptance.

Risks: renderer reflection covariance/SH; cutout space; wardrobe overlap/seams; missed geometry; old repair tests assume unreflected positions. No performance measurement or presentation work.
