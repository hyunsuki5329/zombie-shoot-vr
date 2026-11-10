# Classroom GS alignment and rectangular cleanup

User request (2026-11-06): inspect Downloads/classroom.ply, trim/repair to the classroom rectangle, match the classroom FBX handedness. Preserve accepted Room GS and both user-edited theme maps.

Input: 459968 splats, 62 float fields, SHA256 f8d591a9f6ddb478f003f46b0ad015751f4925e62938624407581485e93ff9b2. Positions/scales/rotations finite; 1658 positive-infinite opacity logits require the same valid saturation interpretation as the room capture.

Reference: original FBX intake SHA9d257fca recorded in classroom-scan-intake.json. Original Desktop path is absent; saved FBX-derived cropped mesh/UV/texture and prior coordinate conversion remain in the original project tmp/classroom-inspection. Do not pretend the missing FBX was reopened. Prior approximate dimensions14 by6.3m guide the frame, not precision survey claims.

Sequence: compare rotation/reflection candidates using furniture and wall landmarks; import verified original into private Classroom folder; create a separate observation scene with positive calibration parent and explicit oriented data child; use reversible rectangular cutout and restrained surface backing for missing boundary only; preserve desks and visible classroom detail. Review rendered views before deciding crop limits. Connect classroom profile only after the scene validates. Validate source lifetime, handness/anchors/crop and both GS paths; build a new output directory and retain original sources/overlay. Record limits and provide preview for user visual acceptance; keep PR draft for unverified headset changes.

No performance logging requested; presentation remains user-owned. Unknown room app/version and perpendicular dimension remain unknown, not pending mandatory remeasurement.
