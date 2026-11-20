using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    internal static class PreviewMaterials {
        public static void Configure(Material material) {
            var color=new Color(.06f,.65f,1f,.32f);
            foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(material.HasProperty(key))material.SetColor(key,color);
            foreach(string key in new[]{"_SurfaceType","_Surface"})if(material.HasProperty(key))material.SetFloat(key,1);
            foreach(string key in new[]{"_ZWrite","_TransparentZWrite"})if(material.HasProperty(key))material.SetFloat(key,0);
            foreach(string key in new[]{"_SrcBlend","_AlphaSrcBlend"})if(material.HasProperty(key))material.SetFloat(key,(float)BlendMode.SrcAlpha);
            foreach(string key in new[]{"_DstBlend","_AlphaDstBlend"})if(material.HasProperty(key))material.SetFloat(key,(float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue=(int)RenderQueue.Transparent;
        }
    }
}
