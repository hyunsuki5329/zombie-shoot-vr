using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VRFPS
{
    // Pinned to UnityGaussianSplatting 2c6fed37. Readiness includes the viewing camera's
    // URP feature, not just metadata. Pipeline selection remains the caller's responsibility.
    public sealed class GaussianRealitySource : RealityRenderSource
    {
        public string dataId;
        public GaussianSplatRenderer splats;
        bool initialized;
        GaussianSplatAsset initializedAsset;
        bool OwnsRenderer => splats && splats.transform.IsChildOf(transform);
        public override string DataId => dataId;
        public override bool IsReady => initialized && isActiveAndEnabled && OwnsRenderer &&
            splats.isActiveAndEnabled && splats.m_Asset == initializedAsset && splats.HasValidAsset &&
            GaussianResourceInventory.IsComplete(splats) && RenderConfigurationError() == null;
        public override bool HasResources => OwnsRenderer && GaussianResourceInventory.HasResources(splats);

        void Awake() { ReleaseNow(); }
        void OnDisable() { ReleaseNow(); }

        public override IEnumerator Initialize()
        {
            if (initialized || HasResources) throw new InvalidOperationException("GS source already owns resources.");
            if (!isActiveAndEnabled || !OwnsRenderer || !splats.gameObject.activeInHierarchy || splats.enabled)
                throw new InvalidOperationException("GS requires an owned, active, initially disabled renderer.");
            if (string.IsNullOrWhiteSpace(dataId) || !splats.HasValidAsset)
                throw new InvalidOperationException("GS data identity or processed asset is missing/invalid.");
            if (!SystemInfo.supportsComputeShaders ||
                (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan))
                throw new InvalidOperationException("This GS integration requires DX12 or Vulkan with compute support.");
            foreach (var shader in new[]{splats.m_ShaderSplats,splats.m_ShaderComposite,splats.m_ShaderDebugPoints,splats.m_ShaderDebugBoxes})
                if (!shader || !shader.isSupported) throw new InvalidOperationException("GS shader is missing or unsupported.");
            if (!splats.m_CSSplatUtilities) throw new InvalidOperationException("GS compute shader is missing.");
            var error=RenderConfigurationError();
            if (error != null) throw new InvalidOperationException(error);
            try
            {
                splats.enabled=true;
                if (!GaussianResourceInventory.IsComplete(splats))
                    throw new InvalidOperationException("GS GPU resource initialization did not complete.");
                initializedAsset=splats.m_Asset;initialized=true;
            }
            catch { ReleaseNow();throw; }
            yield break;
        }
        public override IEnumerator Release()
        {
            ReleaseNow();
            if (HasResources) throw new InvalidOperationException("GS resources remain after release.");
            yield break;
        }
        void ReleaseNow()
        {
            initialized=false;initializedAsset=null;
            if (!OwnsRenderer) return;
            // OnDisable is public and idempotent in the pinned package. Explicit invocation
            // also recovers partial allocation on an already disabled renderer.
            if (splats.enabled) splats.enabled=false;
            else splats.OnDisable();
            GaussianResourceInventory.ClearSorter(splats);
        }
        static string RenderConfigurationError()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline) || pipeline.msaaSampleCount != 1)
                return "GS requires its URP pipeline with MSAA disabled.";
            if (GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()?.enableRenderCompatibilityMode == true)
                return "GS requires URP RenderGraph.";
            var camera=Camera.main;
            if (!camera || !camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                return "GS requires the main rig camera with URP camera data.";
            var features=GaussianResourceInventory.RendererFeatures.GetValue(data.scriptableRenderer) as List<ScriptableRendererFeature>;
            if (features == null || !features.Any(f=>f && f.isActive && f.GetType().FullName == "GaussianSplatting.Runtime.GaussianSplatURPFeature"))
                return "The main rig camera renderer has no active Gaussian Splat feature.";
            return null;
        }
    }

    // The upstream public HasValidRenderSetup checks only three buffers. Audit all owned
    // GPU buffers, sorting scratch buffers, texture, materials and registration instead.
    // Preserve these members in link.xml; version changes require re-auditing this inventory.
    static class GaussianResourceInventory
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static FieldInfo Field(string name) => typeof(GaussianSplatRenderer).GetField(name,Flags)
            ?? throw new InvalidOperationException("Pinned GS resource field missing: "+name);
        static readonly FieldInfo[] Buffers=typeof(GaussianSplatRenderer).GetFields(Flags).Where(f=>f.FieldType==typeof(GraphicsBuffer)).ToArray();
        static readonly FieldInfo[] RequiredBuffers=new[]{"m_GpuPosData","m_GpuOtherData","m_GpuSHData","m_GpuChunks","m_GpuView","m_GpuIndexBuffer","m_GpuSortDistances","m_GpuSortKeys"}.Select(Field).ToArray();
        static readonly FieldInfo[] Objects=new[]{"m_GpuColorData","m_MatSplats","m_MatComposite","m_MatDebugPoints","m_MatDebugBoxes"}.Select(Field).ToArray();
        static readonly FieldInfo Args=Field("m_SorterArgs"),Sorter=Field("m_Sorter"),Registered=Field("m_Registered");
        internal static readonly PropertyInfo RendererFeatures=typeof(ScriptableRenderer).GetProperty("rendererFeatures",Flags)
            ?? throw new InvalidOperationException("Pinned URP renderer feature accessor missing.");
        // Upstream retains this managed cache across OnDisable. A failed sorter or changed
        // compute shader must be reconstructed on the next initialization.
        internal static void ClearSorter(GaussianSplatRenderer renderer) => Sorter.SetValue(renderer,null);
        static bool Valid(GraphicsBuffer buffer) => buffer != null && buffer.IsValid();
        internal static bool IsComplete(GaussianSplatRenderer renderer)
        {
            if (Buffers.Length != 15 || !RequiredBuffers.All(f=>Valid((GraphicsBuffer)f.GetValue(renderer))) ||
                !Objects.All(f=>(UnityEngine.Object)f.GetValue(renderer)!=null) || !(bool)Registered.GetValue(renderer) ||
                !((Sorter.GetValue(renderer) as GpuSorting)?.Valid == true)) return false;
            var args=(GpuSorting.Args)Args.GetValue(renderer);var resources=args.resources;
            return Valid(args.inputKeys) && Valid(args.inputValues) && args.count==renderer.splatCount &&
                Valid(resources.altBuffer) && Valid(resources.altPayloadBuffer) && Valid(resources.passHistBuffer) && Valid(resources.globalHistBuffer);
        }
        internal static bool HasResources(GaussianSplatRenderer renderer)
        {
            if (Buffers.Length != 15) throw new InvalidOperationException("GS buffer inventory changed; re-audit the pinned package.");
            if (Buffers.Any(f=>f.GetValue(renderer)!=null) || Objects.Any(f=>(UnityEngine.Object)f.GetValue(renderer)!=null) || (bool)Registered.GetValue(renderer)) return true;
            var resources=((GpuSorting.Args)Args.GetValue(renderer)).resources;
            return resources.altBuffer!=null || resources.altPayloadBuffer!=null || resources.passHistBuffer!=null || resources.globalHistBuffer!=null;
        }
    }
}
