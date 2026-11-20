using System.Collections.Generic;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        readonly Dictionary<string,Texture2D> buildIcons=new Dictionary<string,Texture2D>();
        static readonly string[] BuildKinds={MarkerRecord.Main,MarkerRecord.Distant,MarkerRecord.Shunt,MarkerRecord.Speed,MarkerRecord.Clearance,MarkerRecord.CircuitB,MarkerRecord.CircuitC};
        void BuildPalette(UIPanelBuilder builder) {
            for(int offset=0;offset<BuildKinds.Length;offset+=3) {
                var row=builder.HStack(_=>{});row.GetComponent<HorizontalLayoutGroup>().spacing=6;
                for(int i=offset;i<System.Math.Min(offset+3,BuildKinds.Length);i++) {
                    string kind=BuildKinds[i];
                    var created=UIBuilderAssets.Shared.CreateButton(UIBuilderAssets.ButtonStyle.Compact,row,()=>Act(()=>StartHomePlacement(kind)));
                    var rect=created.Item1;created.Item2.gameObject.SetActive(false);
                    var size=rect.GetComponent<LayoutElement>()??rect.gameObject.AddComponent<LayoutElement>();size.minWidth=76;size.preferredWidth=100;size.flexibleWidth=1;size.minHeight=size.preferredHeight=58;
                    rect.Tooltip(LocalText.Get(MarkerRecord.KindLabel(kind)),"");
                    var icon=WorkspaceSurface.Rect(rect,"Build tool icon");icon.anchorMin=icon.anchorMax=icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(42,42);icon.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
                    var image=icon.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
                    if(!buildIcons.TryGetValue(kind,out var texture)) {
                        var alpha=ModeIconRaster.ForSymbol(BuildIcon(kind));var pixels=new Color32[alpha.Length];for(int p=0;p<pixels.Length;p++)pixels[p]=new Color32(255,255,255,alpha[p]);
                        texture=new Texture2D(ModeIconRaster.Size,ModeIconRaster.Size,TextureFormat.RGBA32,false){name="EnusDE build "+kind,hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};texture.SetPixels32(pixels);texture.Apply(false,true);buildIcons[kind]=texture;
                    }
                    image.texture=texture;image.color=IsPlacing&&edit.Kind==kind?Color.cyan:WorkspaceSurface.Ink;
                }
            }
            builder.AddField(LocalText.Select("Selected tool","Gewähltes Werkzeug"),()=>IsPlacing?LocalText.Get(MarkerRecord.KindLabel(edit.Kind)):LocalText.Select("Select a build symbol","Bausymbol auswählen"),UIPanelBuilder.Frequency.Periodic);
        }
        static string BuildIcon(string kind)=>kind==MarkerRecord.Main?"BUILD_MAIN":kind==MarkerRecord.Distant?"BUILD_DISTANT":kind==MarkerRecord.Shunt?"BUILD_SHUNT":kind==MarkerRecord.Speed?"BUILD_SPEED":kind==MarkerRecord.Clearance?"BUILD_CLEARANCE":kind==MarkerRecord.CircuitC?"BUILD_GFM_C":"BUILD_GFM";
        void DisposeBuildIcons(){foreach(var icon in buildIcons.Values)if(icon!=null)UnityEngine.Object.Destroy(icon);buildIcons.Clear();}
    }
}
