using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace VRFPS
{
    public sealed class VrScreenFade : MonoBehaviour
    {
        Material material;GameObject shell;MeshRenderer surface;
        int generation;
        public float Alpha {get;private set;}
        public void Configure(Camera camera,Shader shader)
        {
            if(camera==null || shader==null)throw new ArgumentException("Fade camera and shader are required.");
            if(camera.nearClipPlane<=0 || camera.farClipPlane<=Mathf.Max(1,camera.nearClipPlane*4))
                throw new ArgumentException("Fade camera clipping range is too small.");
            if((camera.cullingMask & (1<<gameObject.layer))==0)
                throw new ArgumentException("Fade layer is excluded by the camera.");
            ReleaseVisual();
            material=new Material(shader){name="Intro screen fade (runtime)"};
            shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Intro fade shell";
            shell.layer=gameObject.layer;shell.transform.SetParent(camera.transform,false);
            shell.transform.localScale=Vector3.one*Mathf.Max(1,camera.nearClipPlane*4)*2;
            var collider=shell.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            surface=shell.GetComponent<MeshRenderer>();surface.sharedMaterial=material;
            surface.shadowCastingMode=ShadowCastingMode.Off;surface.receiveShadows=false;
            surface.lightProbeUsage=LightProbeUsage.Off;surface.reflectionProbeUsage=ReflectionProbeUsage.Off;
            Apply(Alpha);
        }
        public void SetImmediate(float alpha)
        {
            ValidateAlpha(alpha);generation++;Apply(alpha);
        }
        public IEnumerator FadeTo(float alpha,float seconds,Func<bool> isPaused,Func<bool> isCancelled)
        {
            ValidateAlpha(alpha);
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(isPaused==null || isCancelled==null)throw new ArgumentNullException("Fade predicates");
            var request=++generation;var from=Alpha;var elapsed=0f;
            while(request==generation && isActiveAndEnabled && !isCancelled())
            {
                if(!isPaused())
                {
                    elapsed+=Time.unscaledDeltaTime;
                    Apply(seconds==0?alpha:Mathf.Lerp(from,alpha,Mathf.Clamp01(elapsed/seconds)));
                    if(elapsed>=seconds)yield break;
                }
                yield return null;
            }
        }
        static void ValidateAlpha(float alpha)
        {
            if(float.IsNaN(alpha)||float.IsInfinity(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
        }
        void Apply(float alpha)
        {
            Alpha=alpha;
            if(material!=null)material.SetFloat("_Alpha",alpha);
            if(surface!=null)surface.enabled=alpha>0;
        }
        void OnDisable(){generation++;Apply(0);}
        void OnDestroy()=>ReleaseVisual();
        void ReleaseVisual()
        {
            if(surface!=null)surface.enabled=false;
            if(shell!=null)Destroy(shell);
            if(material!=null)Destroy(material);
            surface=null;shell=null;material=null;
        }
    }
}
