using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // A layout group of mirrors, never a new physical block, number source or operating authority.
    public static class LineNumberOverview {
        public const int MaxFields=16;
        public static NumberMirrorSource[] Sources(DeskLibrary library)=>library.Desks.OrderBy(d=>d.Code,StringComparer.OrdinalIgnoreCase)
            .SelectMany(d=>d.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.TrainNumber&&!NumberMirrors.IsMirror(t)).OrderBy(t=>t.Y).ThenBy(t=>t.X)
                .Select(t=>new NumberMirrorSource{Desk=d,Tile=t})).ToArray();
        public static string Key(NumberMirrorSource source)=>source.Desk.Id+":"+source.Tile.X+":"+source.Tile.Y;
        public static NumberMirrorSource[] AutomaticSources(DeskLibrary library,LineTrackDefinition line,bool reverse=false) {
            var sources=Sources(library);var result=new List<NumberMirrorSource>();
            foreach(string block in line.BlockSections) {
                var found=sources.Where(s=>s.Tile.BlockId==block).ToArray();var owned=found.Where(s=>s.Desk.Id==line.BlockOwner).ToArray();
                if(owned.Length==1)result.Add(owned[0]);else if(found.Length==1)result.Add(found[0]);else return Array.Empty<NumberMirrorSource>();
            }
            if(reverse)result.Reverse();return result.ToArray();
        }
        public static PanelArea Area(PanelDocument panel,string id)=>panel.Areas.FirstOrDefault(a=>a.Id==id&&a.NumberLine!=null);
        public static bool CanPlace(PanelDocument panel,string id,int x,int y) {
            var area=Area(panel,id);if(area==null||x<0||y<0||x+area.Width>panel.Columns||y+area.Height>panel.Rows)return false;
            return !panel.Tiles.Any(t=>!area.Contains(t.X,t.Y)&&t.X>=x&&t.X<x+area.Width&&t.Y>=y&&t.Y<y+area.Height)&&
                !panel.Areas.Any(a=>a.Id!=id&&(a.BlockLine!=null||a.NumberLine!=null)&&a.X<x+area.Width&&x<a.X+a.Width&&a.Y<y+area.Height&&y<a.Y+a.Height);
        }
        static IEnumerable<PanelRegion> Modules(DeskLibrary library,string target)=>library.Modules.Where(m=>MasterDiagram.Owner(library,m)==target).Select(m=>{
            var doc=library.Desks.Single(d=>d.Id==m.Desk).Panel;return new PanelRegion{X=m.X,Y=m.Y,Width=doc.Columns,Height=doc.Rows};
        });
        static bool Intersects(PanelRegion area,int x,int y,int width,int height)=>area.X<x+width&&x<area.X+area.Width&&area.Y<y+height&&y<area.Y+area.Height;
        public static bool CanPlace(DeskLibrary library,string target,string id,int x,int y) {
            var doc=library.Desks.Single(d=>d.Id==target).Panel;var area=Area(doc,id);
            return CanPlace(doc,id,x,y)&&!Modules(library,target).Any(a=>Intersects(a,x,y,area.Width,area.Height));
        }
        public static void Move(DeskLibrary library,string target,string id,int x,int y) {
            if(!CanPlace(library,target,id,x,y))throw new InvalidOperationException(LocalText.Select("Choose free cells for the complete number overview.","Freie Felder für die vollständige Zugnummernübersicht wählen."));
            Move(library.Desks.Single(d=>d.Id==target).Panel,id,x,y);
        }
        public static void Move(PanelDocument panel,string id,int x,int y) {
            var area=Area(panel,id);
            if(area==null||!CanPlace(panel,id,x,y))throw new InvalidOperationException(LocalText.Select("Choose free cells for the complete number overview.","Freie Felder für die vollständige Zugnummernübersicht wählen."));
            new PanelRegion{X=area.X,Y=area.Y,Width=area.Width,Height=area.Height}.Move(panel,x,y);
        }
        static void Remove(PanelDocument panel,string id) {
            var area=Area(panel,id)??throw new InvalidOperationException(LocalText.Select("The number overview is missing. Select it again.","Die Zugnummernübersicht fehlt. Erneut auswählen."));
            if(panel.Tiles.Any(t=>area.Contains(t.X,t.Y)&&!NumberMirrors.IsMirror(t))||panel.Areas.Any(a=>a.Id!=id&&(a.BlockLine!=null||a.NumberLine!=null)&&a.X<area.X+area.Width&&area.X<a.X+a.Width&&a.Y<area.Y+area.Height&&area.Y<a.Y+a.Height))
                throw new InvalidOperationException(LocalText.Select("Move manually added fields out of the number overview before replacing or removing it.","Manuell ergänzte Felder vor Ersetzen oder Entfernen aus der Zugnummernübersicht verschieben."));
            panel.Tiles.RemoveAll(t=>area.Contains(t.X,t.Y));panel.Areas.Remove(area);
        }
        public static DeskLibrary Delete(DeskLibrary library,string desk,string id) {
            var next=library.Clone();Remove(next.Desks.Single(d=>d.Id==desk).Panel,id);next.Validate(next.WorldKey);return next;
        }
        public static DeskLibrary Create(DeskLibrary library,string lineId,string targetDesk,IEnumerable<NumberMirrorSource> selected,string id=null) {
            var picks=selected?.ToArray()??Array.Empty<NumberMirrorSource>();
            if(picks.Length<1||picks.Length>MaxFields||picks.Any(s=>s?.Tile==null||s.Desk==null)||picks.Select(Key).Distinct().Count()!=picks.Length)
                throw new InvalidOperationException(LocalText.Select("Choose 1–16 different original number fields.","1–16 verschiedene ursprüngliche Zugnummernfelder auswählen."));
            var line=library.FreeLines.SingleOrDefault(l=>l.Id==lineId)??throw new InvalidOperationException(LocalText.Select("Save the line before creating its number overview.","Die Strecke vor dem Anlegen ihrer Zugnummernübersicht speichern."));
            var next=library.Clone();var target=next.Desks.Single(d=>d.Id==targetDesk);var doc=target.Panel;
            var old=id==null?null:Area(doc,id);if(id!=null&&(old==null||old.NumberLine!=lineId))throw new InvalidOperationException(LocalText.Select("Select the number overview again.","Zugnummernübersicht erneut auswählen."));
            // Validate all sources against the captured configuration before removing any old group.
            var sources=picks.Select(p=>{
                var tile=next.Desks.FirstOrDefault(d=>d.Id==p.Desk.Id)?.Panel.At(p.Tile.X,p.Tile.Y);
                if(tile?.Kind!=PanelTileKind.TrainNumber||NumberMirrors.IsMirror(tile)||!tile.SameContent(p.Tile))throw new InvalidOperationException(LocalText.Select("A source field changed. Reopen the overview settings.","Ein Quellenfeld wurde geändert. Übersichtseinstellungen erneut öffnen."));
                return tile;
            }).ToArray();
            if(old!=null)Remove(doc,old.Id);
            int width=picks.Length,x=old?.X??0,y=old?.Y??0;
            doc.Columns=Math.Max(doc.Columns,width);
            bool Fits()=>x+width<=doc.Columns&&y<PanelDocument.MaxRows&&!doc.Tiles.Any(t=>t.Y==y&&t.X>=x&&t.X<x+width)&&
                !Modules(next,targetDesk).Any(a=>Intersects(a,x,y,width,1))&&
                !doc.Areas.Any(a=>(a.BlockLine!=null||a.NumberLine!=null)&&a.Y<=y&&y<a.Y+a.Height&&a.X<x+width&&x<a.X+a.Width);
            if(!Fits()){x=0;y=0;while(y<PanelDocument.MaxRows&&!Fits())y++;}
            if(!Fits())throw new InvalidOperationException(LocalText.Select("No room for the number overview. Make space on the target panel.","Kein Platz für die Zugnummernübersicht. Auf der Zieltafel Platz schaffen."));
            doc.Rows=Math.Max(doc.Rows,y+1);
            for(int i=0;i<sources.Length;i++) {
                var source=sources[i];source.NumberId=source.NumberId??Guid.NewGuid().ToString("N");
                string label=source.Label;
                if(string.IsNullOrWhiteSpace(label))label=source.BlockId!=""&&library.Blocks.TryGetValue(source.BlockId,out var block)?block.Name:picks[i].Desk.Code+" · "+(source.X+1)+" / "+(source.Y+1);
                doc.Tiles.Add(new PanelTile{X=x+i,Y=y,Kind=PanelTileKind.TrainNumber,NumberMirror=source.NumberId,Label=label.Substring(0,Math.Min(label.Length,48))});
            }
            string code=old?.Code??("ZN-"+line.Code.Substring(0,Math.Min(16,line.Code.Length)));string stem=code;int suffix=1;
            while(doc.Areas.Any(a=>string.Equals(a.Code,code,StringComparison.OrdinalIgnoreCase)))code=stem+"-"+suffix++;
            doc.Areas.Add(new PanelArea{Id=old?.Id??Guid.NewGuid().ToString("N"),Code=code,Name=old?.Name??line.Name,Kind="Strecke",NumberLine=lineId,X=x,Y=y,Width=width,Height=1});
            next.Validate(next.WorldKey);return next;
        }
    }
}
