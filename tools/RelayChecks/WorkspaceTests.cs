using System;
using System.Linq;
using EnusDE.Signaltechnik;
static class WorkspaceTests {
    static void Check(bool condition,string text){if(!condition)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Inside(WorkspaceBox b,double w,double h)=>b.X>=0&&b.Y>=0&&b.Width>0&&b.Height>0&&b.X+b.Width<=w+.001&&b.Y+b.Height<=h+.001;
    public static void Run() {
        foreach(var size in new[]{(W:784d,H:650d),(W:1084d,H:800d),(W:1630d,H:1010d),(W:3824d,H:2110d)}){
            var layout=MasterLayoutGeometry.For(size.W,size.H);var boxes=new[]{layout.Header,layout.Sidebar,layout.Diagram,layout.Footer};
            Check(boxes.All(b=>Inside(b,size.W,size.H))&&layout.Sidebar.X+layout.Sidebar.Width<layout.Diagram.X&&layout.Header.Y+layout.Header.Height<layout.Diagram.Y&&layout.Diagram.Y+layout.Diagram.Height<layout.Footer.Y,"master module controls, header, status and diagram remain separate and bounded at "+size.W+"x"+size.H);
        }
        foreach(var size in new[]{(W:784d,H:508d),(W:1264d,H:908d),(W:1904d,H:1038d),(W:3824d,H:2118d),(W:704d,H:1228d)}){
            var a=WorkspaceLayout.For(size.W,size.H);var b=WorkspaceLayout.For(size.W,size.H,true,99999,99999);
            Check(new[]{a.Diagram,a.Header,a.Status,a.Popup,b.Popup}.All(r=>Inside(r,size.W,size.H)),"own workspace retains visible diagram, status and bounded normal/large popups at "+size.W+"x"+size.H);
        }
        var normal=WorkspaceLayout.For(1264,908);var large=WorkspaceLayout.For(1264,908,true,-999,-999);
        Check(normal.Diagram.Width==large.Diagram.Width&&normal.Diagram.Height==large.Diagram.Height&&normal.Diagram.Height>908*.8,"opening or resizing contextual controls never shrinks the large diagram or adds a whole-window scroll area");
        Check(large.Popup.X>=large.Diagram.X&&large.Popup.Y>=large.Diagram.Y&&large.Popup.Y+large.Popup.Height<=large.Status.Y,"dragged contextual windows remain inside the work area above the status strip");
        var compact=WorkspaceLayout.For(1264,908,compact:true);
        Check(compact.Popup.Height<100&&normal.Popup.Height>compact.Popup.Height*3&&compact.Diagram.Height==normal.Diagram.Height,
            "signal actions use a single compact popup row without shrinking the track diagram");
        bool rejected=false;try{WorkspaceLayout.For(double.NaN,900);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"unresolved or nonfinite window geometry is rejected before positioning UI controls");
        Check(new[]{"DISPATCH","BUILD","MAPEDIT","TRACK","REGION","STW","GO","EDIT","FS","BAU","ID","?"}.All(k=>WorkspaceSymbols.For(k).Count>=4&&WorkspaceSymbols.For(k).All(l=>l.Length==4&&l.All(v=>v>=0&&v<=32)&&(l[0]!=l[2]||l[1]!=l[3]))),"own vector tool symbols stay within their interactive button and contain no degenerate drawing segments");
        var modes=new[]{WorkspaceMode.Dispatcher,WorkspaceMode.Build,WorkspaceMode.Editor};
        var host=new ModePickerAccess(true,true,true,true);
        var client=new ModePickerAccess(false,true,true,true);
        var loading=new ModePickerAccess(true,false,false,false);
        Check(modes.All(host.CanOpen),"mode picker offers all three existing workspaces to a ready host");
        Check(client.CanOpen(WorkspaceMode.Dispatcher)&&!client.CanOpen(WorkspaceMode.Build)&&!client.CanOpen(WorkspaceMode.Editor),"mode picker preserves client viewing and denies build and editor access");
        Check(!modes.Any(loading.CanOpen),"mode picker waits for ready workspaces");
        var buildOnly=new ModePickerAccess(true,false,true,false);
        var operatingOnly=new ModePickerAccess(true,true,false,false);
        Check(buildOnly.CanOpen(WorkspaceMode.Build)&&!buildOnly.CanOpen(WorkspaceMode.Editor)&&operatingOnly.CanOpen(WorkspaceMode.Dispatcher)&&!operatingOnly.CanOpen(WorkspaceMode.Editor),"mode picker does not couple the availability of building and operating");
        Check(ModePickerText.UsesGerman(" de-DE ",true)&&ModePickerText.UsesGerman("de",true)&&!ModePickerText.UsesGerman("en",true)&&!ModePickerText.UsesGerman("fr",true)&&!ModePickerText.UsesGerman(null,true)&&!ModePickerText.UsesGerman("de",false),"mode picker honors the optional German interface setting and defaults to English");
        Check(modes.Select(m=>ModePickerText.Caption(m,false)).SequenceEqual(new[]{"Dispatcher","Build mode","Editor"})&&modes.Select(m=>ModePickerText.Caption(m,true)).SequenceEqual(new[]{"Fahrdienstleiter","Baumodus","Editor"})&&ModePickerText.Status(client,true).Contains("nur zur Ansicht")&&ModePickerText.Status(client,false).Contains("View only"),"mode captions and client read-only explanation are available in English and German");
        Check(ModePickerText.ToolbarCaption(true)=="STW"&&ModePickerText.ToolbarCaption(false)=="IXL","toolbar abbreviation follows the selected interface language");
        var rasters=modes.Select(ModeIconRaster.Alpha).ToArray();
        int iconSize=ModeIconRaster.Size;
        Check(rasters.All(alpha=>{
            var opaque=Enumerable.Range(0,alpha.Length).Where(i=>alpha[i]>180).ToArray();
            return alpha.Length==iconSize*iconSize&&opaque.Length>100&&opaque.Length<alpha.Length/2
                &&opaque.Select(i=>i%iconSize).Distinct().Count()>iconSize/2&&opaque.Select(i=>i/iconSize).Distinct().Count()>iconSize/2;
        })&&rasters.Select(Convert.ToBase64String).Distinct().Count()==3,"all three picker images render as distinct two-dimensional glyphs with transparent backgrounds");
        Check(new[]{(W:784d,H:498d),(W:1264d,H:798d),(W:1904d,H:1038d)}.All(s=>{
            var editor=EditorLayout.For(s.W,s.H);
            return new[]{editor.Tools,editor.Diagram,editor.Details,editor.Footer,editor.Header,editor.Catalogue}.All(b=>Inside(b,s.W,s.H))
                &&editor.Tools.X+editor.Tools.Width<editor.Diagram.X&&editor.Diagram.X+editor.Diagram.Width<editor.Details.X;
        }),"independent editor layout gives the catalogue and working panes stable, non-overlapping geometry");
        foreach(var size in new[]{(W:640d,H:420d),(W:784d,H:498d),(W:1904d,H:1038d)}) {
            var closed=EditorLayout.For(size.W,size.H,false,false);var properties=EditorLayout.For(size.W,size.H,true,false);
            Check(Inside(closed.Diagram,size.W,size.H)&&Inside(properties.Diagram,size.W,size.H)&&Inside(properties.Details,size.W,size.H)&&
                closed.Diagram.X==12&&closed.Diagram.Width==size.W-24&&properties.Diagram.Width<closed.Diagram.Width&&
                properties.Diagram.X+properties.Diagram.Width<properties.Details.X,
                "context editor reclaims both side rails while closed and keeps requested properties separate at "+size.W+"x"+size.H);
        }
        var empty=new PanelDocument{WorldKey="editor",Columns=512,Rows=320};
        Check(EditorLayout.Fit(empty,480,336).Zoom==1&&EditorLayout.Fit(empty,320,300).Zoom>=.65&&EditorLayout.GridStride(3)==4,
            "empty editor fits a workable visible grid instead of shrinking to the entire reserved canvas");
        empty.Put(new PanelTile{X=400,Y=250});empty.Put(new PanelTile{X=410,Y=252});
        var fitted=EditorLayout.Fit(empty,520,340);
        Check(Math.Abs(fitted.Zoom-520d/(13*72))<.001&&fitted.CenterX==405.5&&fitted.CenterY==251.5,
            "editor zoom centers on existing drawing cells even in a largely empty canvas");
    }
}
