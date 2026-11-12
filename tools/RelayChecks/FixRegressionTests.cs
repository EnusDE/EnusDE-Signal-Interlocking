using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

public static class FixRegressionTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Same(PanelPoint a,PanelPoint b)=>Math.Abs(a.X-b.X)<.001&&Math.Abs(a.Y-b.Y)<.001;
    public static void Run(string root) {
        int prepared=0,input=0,pointer=0,objects=0,signs=0,warnings=0,recovery=0;bool broken=true;
        var loop=new PlacementWorkLoop(()=>prepared++,()=>input++,()=>pointer++,()=>objects++,()=>{signs++;if(broken)throw new Exception("Kein Shader für Kartenschild verfügbar.");},(lane,error)=>{if(error==null)recovery++;else warnings++;});
        for(int frame=0;frame<600;frame++)loop.Tick(frame/60d,true);
        Check(prepared==10&&objects==10&&input==600&&pointer>=90&&signs==5,"repeated baked-sign shader failures do not block repository preparation, object drawing, placement input or mouse preview");
        Check(warnings==1&&loop.Failure(PlacementWork.Signs)!=null,"persistent optional appearance failure is recorded once rather than flooding the game log");
        broken=false;loop.Tick(10,true);Check(recovery==1&&loop.Failure(PlacementWork.Signs)==null&&prepared==11,"appearance recovery does not require reinitializing or reinstalling the placement module");
        int idlePointers=pointer;loop.Tick(10.2,false);Check(pointer==idlePointers,"closed placement preview performs no unnecessary pointer raycasts");
        string action="";var cancelled=new PlacementWorkLoop(()=>{},()=>action="cancelled",()=>action="preview",()=>{},()=>{},(lane,error)=>{});cancelled.Tick(0,true);
        Check(action=="cancelled","placement cancellation cannot be overwritten by a stale preview update later in the same frame");
        int continued=0;var failingWorld=new PlacementWorkLoop(()=>throw new Exception("file unavailable"),()=>continued++,()=>{},()=>continued++,()=>continued++,(lane,error)=>{});failingWorld.Tick(0,false);
        Check(continued==3,"a failed work lane cannot prevent unrelated scheduled work from being attempted");
        var requested=new List<string>();object hdrp=new object();object found=ShaderPolicy.Find<object>(name=>{requested.Add(name);return name=="HDRP/Unlit"?hdrp:null;});
        Check(ReferenceEquals(found,hdrp)&&requested.SequenceEqual(new[]{"Universal Render Pipeline/Unlit","HDRP/Unlit"}),"shared shader selection supports an HDRP-only installation instead of requiring URP or legacy Unlit");
        Check(ShaderPolicy.Find<object>(name=>null)==null,"a renderer with no available shader can retain the original sign instead of inventing a shader");
        bool joined=true,corner=true,straight=true,slopes=true;
        var branch=PanelGeometry.Lines(PanelTileKind.Switch)[2].B;var diagonal=PanelGeometry.Lines(PanelTileKind.Diagonal)[0];var bend=PanelGeometry.Lines(PanelTileKind.Curve)[1].B;
        for(int rotation=0;rotation<4;rotation++)foreach(bool mirror in new[]{false,true}) {
            var offset=PanelGeometry.World(new PanelPoint(2,2),rotation,mirror,0,0);int x=(int)Math.Round(offset.X/PanelGeometry.CellWidth),y=(int)Math.Round(-offset.Y/PanelGeometry.CellHeight);
            joined&=Same(PanelGeometry.World(branch,rotation,mirror,0,0),PanelGeometry.World(diagonal.A,rotation,mirror,x,y));
            // Curve rotated 180 degrees connects the lower-left corner to a straight edge.
            var opposite=new PanelPoint(-bend.X,-bend.Y);
            corner&=Same(PanelGeometry.World(branch,rotation,mirror,0,0),PanelGeometry.World(opposite,rotation,mirror,x,y));
            offset=PanelGeometry.World(new PanelPoint(2,0),rotation,mirror,0,0);x=(int)Math.Round(offset.X/PanelGeometry.CellWidth);y=(int)Math.Round(-offset.Y/PanelGeometry.CellHeight);
            straight&=Same(PanelGeometry.World(new PanelPoint(1,0),rotation,mirror,0,0),PanelGeometry.World(new PanelPoint(-1,0),rotation,mirror,x,y));
            var a=PanelGeometry.World(diagonal.A,rotation,mirror,0,0);var b=PanelGeometry.World(diagonal.B,rotation,mirror,0,0);slopes&=Math.Abs(Math.Abs(a.X-b.X)/PanelGeometry.CellWidth-Math.Abs(a.Y-b.Y)/PanelGeometry.CellHeight)<.001;
        }
        Check(joined,"switch branch and neighboring diagonal share an exact endpoint in all rotations and mirrors");
        Check(corner,"switch-to-curve transition shares the same cell-corner endpoint instead of a twelve-pixel gap");
        Check(straight,"straight track connections remain continuous across cell borders after rotation and mirroring");
        Check(slopes,"diagonal track reaches exact opposite corners in the rectangular grid");
        PanelTile current=new PanelTile{X=1,Y=2,Mirror=true};var original=current;var binding=new PanelDraftBinding(current,()=>current);binding.Do(()=>current.Label="W 12");
        Check(binding.Active&&binding.Mirror&&original.Label=="W 12","current editor controls remain bound to their selected tile draft");
        current=null;bool staleWrite=false;binding.Do(()=>staleWrite=true);Check(binding.Mirror&&!binding.Active&&!staleWrite,"deferred native toggle reads remain safe after editor mode clears the draft, and stale actions are ignored");
        current=new PanelTile{X=4,Y=5};binding.Do(()=>current.Reference="wrong");Check(current.Reference==""&&binding.Draft==original,"old editor callbacks cannot change the newly selected tile or its binding");
    }
}
