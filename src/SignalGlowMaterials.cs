using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalModels {
        readonly Material[] glowLens=new Material[5],glowHalo=new Material[5];
        bool glowEnabled=true,haloEnabled=true,viewKnown;
        float radiance=6,haloStrength=.75f;
        int glowRevision;
        Vector3 viewPosition;
        Mesh glareMesh;
        Texture2D glareTexture;
        public string GlowStatus {get;private set;}
        static Color LampColor(int index)=>index==1?Color.red:index==2?Color.green:index==3?Color.yellow:Color.white;
        public void ApplyGlow(SignalGlowOptions value) {
            value=(value??new SignalGlowOptions()).Normalize();if(glowEnabled==value.Enabled&&haloEnabled==value.Halo&&radiance==(float)value.Radiance&&haloStrength==(float)value.HaloStrength)return;
            glowEnabled=value.Enabled;haloEnabled=value.Halo;radiance=(float)value.Radiance;haloStrength=(float)value.HaloStrength;glowRevision++;UpdateGlowMaterials();
        }
        public void View(Vector3? position){viewKnown=position.HasValue;if(viewKnown)viewPosition=position.Value;}
        Material LensMaterial(SignalLensColor color,bool installed){int index=(int)color;if(index==0)return off;if(installed&&glowEnabled&&glowLens[index]!=null)return glowLens[index];return color==SignalLensColor.Red?red:color==SignalLensColor.Green?green:color==SignalLensColor.Yellow?yellow:white;}
        Material HaloMaterial(SignalLensColor color)=>color!=SignalLensColor.Off&&glowHalo[(int)color]!=null?glowHalo[(int)color]:off;
        static void Colored(Material material,Color color,float strength,bool lensColor) {
            bool unlit=material.shader.name.IndexOf("unlit",StringComparison.OrdinalIgnoreCase)>=0||material.shader.name=="Sprites/Default";var bright=new Color(color.r*strength,color.g*strength,color.b*strength,1);
            foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(material.HasProperty(key))material.SetColor(key,unlit?bright:color);
            foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(material.HasProperty(key)){material.SetColor(key,bright);if(key=="_EmissionColor")material.EnableKeyword("_EMISSION");}
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        void UpdateGlowMaterials(){for(int i=1;i<5;i++){if(glowLens[i]!=null)Colored(glowLens[i],LampColor(i),radiance,true);if(glowHalo[i]!=null)Colored(glowHalo[i],LampColor(i),radiance*haloStrength*.2f,false);}}
        void BuildGlow(Shader shader) {
            for(int i=1;i<5;i++){glowLens[i]=ColorOf(shader,LampColor(i));glowLens[i].enableInstancing=true;}
            UpdateGlowMaterials();
            // The soft overlay is optional; lens emission remains usable when a transparent shader is absent.
            var probe=new Material(shader);bool transparent;try{transparent=new[]{"_BaseMap","_Surface","_SrcBlend","_DstBlend","_ZWrite"}.All(probe.HasProperty);}finally{UnityEngine.Object.Destroy(probe);}
            Shader haloShader=transparent?shader:Shader.Find("Sprites/Default");if(haloShader==null||!haloShader.isSupported){GlowStatus="Signalleuchten: "+shader.name+" · Leuchtlinsen; transparenter Lichthof nicht verfügbar.";return;}
            glareTexture=new Texture2D(32,32,TextureFormat.RGBA32,false,true){name="EnusDE radial signal glow",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var pixels=new Color[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=new Color(1,1,1,(float)SignalGlowOptions.HaloAlpha((x+.5)/16-1,(y+.5)/16-1));glareTexture.SetPixels(pixels);glareTexture.Apply(false,true);
            for(int i=1;i<5;i++) {
                var value=ColorOf(haloShader,LampColor(i));glowHalo[i]=value;value.enableInstancing=true;value.renderQueue=(int)RenderQueue.Transparent;value.SetOverrideTag("RenderType","Transparent");
                if(value.HasProperty("_BaseMap"))value.SetTexture("_BaseMap",glareTexture);if(value.HasProperty("_MainTex"))value.SetTexture("_MainTex",glareTexture);
                if(transparent){Set(value,"_Surface",1);Set(value,"_Blend",2);Set(value,"_SrcBlend",(float)BlendMode.SrcAlpha);Set(value,"_DstBlend",(float)BlendMode.One);Set(value,"_SrcBlendAlpha",(float)BlendMode.One);Set(value,"_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);Set(value,"_ZWrite",0);value.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");if(value.HasProperty("_ReceiveFog")){Set(value,"_ReceiveFog",1);value.EnableKeyword("_TRANSPARENT_RECEIVE_FOG");}}
                foreach(string pass in new[]{"ShadowCaster","DepthOnly","DepthNormals","MotionVectors","XRMotionVectors","GBuffer"})value.SetShaderPassEnabled(pass,false);
            }
            UpdateGlowMaterials();glareMesh=new Mesh{name="EnusDE shared signal glow quad"};glareMesh.vertices=new[]{new Vector3(-.25f,-.25f,0),new Vector3(-.25f,.25f,0),new Vector3(.25f,.25f,0),new Vector3(.25f,-.25f,0)};glareMesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};glareMesh.triangles=new[]{0,1,2,0,2,3};glareMesh.RecalculateBounds();glareMesh.RecalculateNormals();GlowStatus="Signalleuchten: "+shader.name+" · Lichthof: "+haloShader.name+" · gemeinsame Materialien, Nahbereich 250 m.";
        }
        static void Set(Material material,string property,float value){if(material.HasProperty(property))material.SetFloat(property,value);}
    }
}
