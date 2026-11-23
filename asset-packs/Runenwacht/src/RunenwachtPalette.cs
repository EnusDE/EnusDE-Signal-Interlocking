using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace EnusDE.Runenwacht {
    // Shared opaque PBR resources; each signal owns only its animated light materials.
    public sealed class RunenwachtPalette:IDisposable {
        static RunenwachtPalette shared;static int users;
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<CrystalChannel,Material> bodies=new Dictionary<CrystalChannel,Material>();
        readonly Shader shader;
        public readonly Material Steel,Copper,Ceramic,Stone,Reflector;
        public readonly string Pipeline;
        public static RunenwachtPalette Acquire(){if(shared==null)shared=new RunenwachtPalette();users++;return shared;}
        public static void Release(){if(users<=0)return;if(--users==0){shared.Dispose();shared=null;}}
        RunenwachtPalette() {
            Pipeline=GraphicsSettings.currentRenderPipeline?.GetType().FullName??"Built-in";
            string shaderName=ShaderNameFor(Pipeline);
            shader=Shader.Find(shaderName)??Shader.Find("Unlit/Color");if(shader==null)throw new InvalidOperationException("Runenwacht requires a supported active-pipeline shader.");
            Steel=Surface(new Color(.42f,.46f,.48f),.25f,.4f,"steel");Copper=Surface(new Color(.8f,.47f,.23f),.5f,.48f,"copper");Ceramic=Surface(new Color(.87f,.81f,.66f),.02f,.57f,"ceramic");Stone=Surface(new Color(.61f,.6f,.55f),0,.14f,"stone");Reflector=Surface(new Color(.58f,.64f,.66f),.36f,.61f,"reflector");
            foreach(CrystalChannel channel in Enum.GetValues(typeof(CrystalChannel))){var hue=Colour(channel);var baseColour=Color.Lerp(new Color(.42f,.47f,.5f),hue,.32f);bodies[channel]=Surface(baseColour,.05f,.79f,"crystal");}
        }
        public static string ShaderNameFor(string pipeline)=>string.IsNullOrEmpty(pipeline)||pipeline=="Built-in"?"Standard":pipeline.IndexOf("HDRender",StringComparison.OrdinalIgnoreCase)>=0?"HDRP/Lit":pipeline.IndexOf("Universal",StringComparison.OrdinalIgnoreCase)>=0?"Universal Render Pipeline/Lit":"Unlit/Color";
        public Material Body(CrystalChannel channel)=>bodies[channel];
        public Material Animated(CrystalChannel channel){var m=new Material(shader);Configure(m);Paint(m,Color.Lerp(new Color(.3f,.36f,.39f),Colour(channel),.32f));if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.7f);return m;}
        static void Configure(Material m) {
            m.enableInstancing=true;
            foreach(string key in new[]{"_Surface","_SurfaceType","_Metallic"})if(m.HasProperty(key))m.SetFloat(key,0);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_ALPHATEST_ON");m.renderQueue=2000;
        }
        Material Surface(Color colour,float metallic,float smoothness,string surface) {
            var m=new Material(shader);owned.Add(m);Configure(m);Paint(m,colour);
            foreach(string key in new[]{"_Metallic"})if(m.HasProperty(key))m.SetFloat(key,metallic);
            foreach(string key in new[]{"_Smoothness","_Glossiness"})if(m.HasProperty(key))m.SetFloat(key,smoothness);
            var albedo=Texture(surface,false);var mask=Texture(surface,true);
            foreach(string key in new[]{"_BaseMap","_BaseColorMap","_MainTex"})if(m.HasProperty(key))m.SetTexture(key,albedo);
            if(m.HasProperty("_MaskMap")){m.SetTexture("_MaskMap",mask);m.EnableKeyword("_MASKMAP");}
            m.DisableKeyword("_EMISSION");foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(m.HasProperty(key))m.SetColor(key,Color.black);
            return m;
        }
        readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        Texture2D Texture(string surface,bool mask) {
            string key=surface+(mask?"-mask":"");if(textures.TryGetValue(key,out var previous))return previous;
            const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGBA32,true,mask){name="Runenwacht original "+key,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                uint h=unchecked((uint)(x*374761393+y*668265263+9187));h=(h^(h>>13))*1274126177;float noise=(h&65535)/65535f;
                float grain=.78f+noise*.2f;bool scratch=(x*7+y*13)%113<2;
                if(surface=="steel"||surface=="copper")grain=(scratch?.57f:grain)*(.94f+.06f*(float)Math.Sin(y*.7));
                if(surface=="crystal")grain=.65f+.3f*noise+.1f*(float)Math.Sin(x*.085+y*.04);
                float metallic=surface=="copper"?.5f:surface=="steel"?.25f:surface=="reflector"?.36f:surface=="crystal"?.05f:0;
                float smoothness=surface=="stone"?.12f:surface=="crystal"?.77f:surface=="ceramic"?.57f:surface=="reflector"?.61f:.44f;
                var c=new Color(grain,grain,grain,1);
                if(surface=="copper"&&noise<.04f)c=new Color(.38f,.56f,.46f,1);
                pixels[y*size+x]=mask?new Color(metallic,.88f+noise*.12f,0,smoothness-(scratch?.14f:0)):c;
            }
            tex.SetPixels(pixels);tex.Apply(true,true);owned.Add(tex);textures[key]=tex;return tex;
        }
        public static Color Colour(CrystalChannel c)=>c==CrystalChannel.Red?new Color(.9f,.06f,.025f):c==CrystalChannel.Amber?new Color(1,.52f,.04f):c==CrystalChannel.Green?new Color(.04f,.86f,.65f):c==CrystalChannel.Violet?new Color(.64f,.13f,.9f):new Color(.8f,.9f,1);
        public static void Paint(Material m,Color c){c.a=1;foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(m.HasProperty(key))m.SetColor(key,c);}
        public static void Light(Material m,CrystalChannel channel,float level,float strength,bool vein) {
            var hue=Colour(channel);Paint(m,Color.Lerp(new Color(.26f,.32f,.35f),hue,.26f+level*.25f));
            m.EnableKeyword("_EMISSION");foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(m.HasProperty(key))m.SetColor(key,hue*(level*strength*(vein?1.2f:.38f)));
            if(m.HasProperty("_EmissiveIntensity"))m.SetFloat("_EmissiveIntensity",1);
        }
        public void Dispose(){foreach(var obj in owned)if(obj!=null)UnityEngine.Object.Destroy(obj);owned.Clear();}
    }
}
