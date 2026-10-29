# Local PLY import lifetime patch

Embedded from UnityGaussianSplatting package 1.1.1, revision
`2c6fed37da67a217367261fcfcd3316d34c73e76` (MIT; see LICENSE.md).
Runtime/shader files and Unity metadata are unchanged. Package manager cache fingerprint removed.

Changes: GaussianFileReader releases raw PLY bytes on success and exceptions,
and releases converted splats if postprocessing fails. PLYFileReader releases its
allocation if reading fails, resetting the out value to prevent double disposal.

Reproduce in batch with `-executeMethod GaussianImportLifetimeCheck.Run -quit`.
Enable stack traces (the check does this) and inspect the shutdown log as well as
its success marker. Before patch: 30 Persistent leaks over ten valid, ten missing
property, and ten truncated imports. This checks Editor import lifetime, not
runtime VRAM, performance, SPZ failure handling, or whole-process memory safety.
