using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace EnusDE.Signaltechnik {
    public enum BlockViewDetail { Full,TrackAndNumbers,NumbersOnly }
    public sealed class BlockViewOptions {
        public BlockViewDetail Detail;
        public bool Reverse,Turn180;
        public int FirstSection,SectionCount;
        public BlockViewOptions Clone()=>(BlockViewOptions)MemberwiseClone();
        public void Validate() {
            if(!Enum.IsDefined(typeof(BlockViewDetail),Detail)||FirstSection<0||FirstSection>15||SectionCount<0||SectionCount>16)throw new InvalidDataException(LocalText.Select("Invalid block view settings.","Ungültige Einstellungen der Blockansicht."));
        }
    }
    public sealed class BlockViewProjection {public List<PanelTile> Tiles;public int Width,Height;}
    public static class BlockDiagramViews {
        public static bool LabelAt(PanelArea area,PanelTile tile)=>tile.Y==(area.BlockView?.Turn180==true?area.Y+area.Height-1:area.Y)&&tile.X==((area.BlockView?.Reverse==true)^(area.BlockView?.Turn180==true)?area.X+area.Width-1:area.X);
        public static BlockViewOptions Effective(BlockViewOptions saved,int sections,bool owner) {
            var value=saved?.Clone()??new BlockViewOptions();value.Validate();
            if(owner){value.Detail=BlockViewDetail.Full;value.FirstSection=value.SectionCount=0;}
            else {value.FirstSection=Math.Min(value.FirstSection,sections-1);if(value.SectionCount>0)value.SectionCount=Math.Min(value.SectionCount,sections-value.FirstSection);}
            return value;
        }
        public static BlockViewProjection Project(PanelDocument template,IReadOnlyList<TrackBlock> blocks,BlockViewOptions value) {
            value.Validate();int first=value.FirstSection,count=value.SectionCount==0?blocks.Count-first:value.SectionCount;
            if(first>=blocks.Count||count<1||first+count>blocks.Count)throw new InvalidOperationException(LocalText.Select("Choose existing sections for this view.","Vorhandene Abschnitte für diese Ansicht wählen."));
            var result=new BlockViewProjection{Width=value.Detail==BlockViewDetail.NumbersOnly?count:count*6+1,Height=value.Detail==BlockViewDetail.NumbersOnly?1:2,Tiles=new List<PanelTile>()};
            if(value.Detail==BlockViewDetail.NumbersOnly) {
                for(int i=0;i<count;i++) {
                    var number=template.Tiles.Single(t=>NumberMirrors.IsMirror(t)&&t.X==(first+i)*6+3).Clone();number.X=i;number.Y=0;number.InlineNumberMirror=false;result.Tiles.Add(number);
                }
            } else {
                foreach(var original in template.Tiles) {
                    var tile=original.Clone();
                    if(tile.Kind==PanelTileKind.Label){tile.X=0;result.Tiles.Add(tile);continue;}
                    if(tile.Kind==PanelTileKind.LinePermission){tile.X=4;result.Tiles.Add(tile);continue;}
                    if(tile.X<first*6||tile.X>(first+count)*6)continue;tile.X-=first*6;
                    if(value.Detail==BlockViewDetail.TrackAndNumbers&&tile.Kind==PanelTileKind.Signal)tile=new PanelTile{X=tile.X,Y=tile.Y,Kind=PanelTileKind.Straight,Ports=17,BlockId=tile.BlockId};
                    result.Tiles.Add(tile);
                }
                result.Tiles.Single(t=>t.X==0&&t.Y==1).CircuitBoundary=blocks[first].BoundaryA;
                result.Tiles.Single(t=>t.X==result.Width-1&&t.Y==1).CircuitBoundary=blocks[first+count-1].BoundaryB;
            }
            foreach(var tile in result.Tiles)Orient(tile,value,result.Width,result.Height);
            return result;
        }
        public static void Reflect(PanelTile tile,int width) {
            tile.X=width-1-tile.X;
            if(tile.Kind!=PanelTileKind.Label&&!NumberMirrors.IsMirror(tile)){tile.Rotation=(6-tile.Rotation)%4;tile.Mirror=!tile.Mirror;if(tile.MainSignalDirection.HasValue)tile.MainSignalDirection=(6-tile.MainSignalDirection.Value)%4;if(tile.ShuntSignalDirection.HasValue)tile.ShuntSignalDirection=(6-tile.ShuntSignalDirection.Value)%4;}
        }
        public static void Orient(PanelTile tile,BlockViewOptions value,int width,int height) {
            if(value.Reverse)Reflect(tile,width);
            if(value.Turn180) {
                tile.X=width-1-tile.X;tile.Y=height-1-tile.Y;
                if(tile.Kind!=PanelTileKind.Label){tile.Rotation=(tile.Rotation+2)%4;if(tile.MainSignalDirection.HasValue)tile.MainSignalDirection=(tile.MainSignalDirection.Value+2)%4;if(tile.ShuntSignalDirection.HasValue)tile.ShuntSignalDirection=(tile.ShuntSignalDirection.Value+2)%4;}
            }
        }
        public static DeskLibrary Edit(DeskLibrary library,string lineId,string deskId,BlockViewOptions options) {
            options.Validate();var line=library.FreeLines.Single(l=>l.Id==lineId);bool owner=line.BlockOwner==deskId;
            if(options.FirstSection>=line.BlockSections.Count||options.SectionCount>line.BlockSections.Count-options.FirstSection)throw new InvalidOperationException(LocalText.Select("Choose existing sections for this view.","Vorhandene Abschnitte für diese Ansicht wählen."));
            if(owner&&(options.Detail!=BlockViewDetail.Full||options.FirstSection!=0||options.SectionCount!=0))throw new InvalidOperationException(LocalText.Select("The operating interlocking retains all signals and original number fields. Choose an additional display interlocking for a reduced view.","Das bedienende Stellwerk behält alle Signale und ursprünglichen Nummernfelder. Für eine reduzierte Ansicht eine zusätzliche Anzeigetafel wählen."));
            var next=library.Clone();var document=next.Desks.Single(d=>d.Id==deskId).Panel;var area=BlockDiagram.Area(document,lineId)??throw new InvalidOperationException(LocalText.Select("Select an existing block assembly.","Vorhandenen Blockverbund auswählen."));
            int count=options.SectionCount==0?line.BlockSections.Count-options.FirstSection:options.SectionCount;
            var bounds=new PanelRegion{X=area.X,Y=area.Y,Width=options.Detail==BlockViewDetail.NumbersOnly?count:count*6+1,Height=options.Detail==BlockViewDetail.NumbersOnly?1:2};
            var growth=CanvasGrowth.For(document,bounds,0);if(growth.Changed)growth.Apply(document);
            BlockDiagram.Rebuild(next,line,line.BlockSections.Select(id=>next.Blocks[id]).ToArray(),new Dictionary<string,BlockViewOptions>{{deskId,options}});next.Validate(next.WorldKey);return next;
        }
    }
}
