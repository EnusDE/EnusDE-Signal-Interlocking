using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class NumberMirrorSource {
        public DeskDefinition Desk;
        public PanelTile Tile;
        public string Caption=>Desk.Code+" · "+(string.IsNullOrWhiteSpace(Tile.Label)?"Zugnummernfeld":Tile.Label)+" · "+(Tile.X+1)+" / "+(Tile.Y+1);
    }
    // Mirrors own no number, detector, route authority or forwarding endpoint.
    public sealed class NumberMirrorIndex {
        readonly Dictionary<string,NumberMirrorSource> sources=new Dictionary<string,NumberMirrorSource>(StringComparer.Ordinal);
        public NumberMirrorIndex(DeskLibrary library) {
            foreach(var desk in library?.Desks??Enumerable.Empty<DeskDefinition>())foreach(var field in desk.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.TrainNumber&&t.NumberId!=null)) {
                if(NumberMirrors.IsMirror(field)||sources.ContainsKey(field.NumberId))throw new InvalidDataException("Zugnummern-Quellkennung doppelt oder an einer Anzeigekopie vergeben.");
                sources[field.NumberId]=new NumberMirrorSource{Desk=desk,Tile=field};
            }
        }
        public IEnumerable<PanelTile> Inputs(IEnumerable<PanelTile> visible)=>visible.Where(NumberMirrors.IsMirror).Select(t=>Source(t)?.Tile).Where(t=>t!=null).Distinct();
        public string Caption(PanelTile mirror,Func<PanelTile,string> read,bool fresh=true){var source=Source(mirror);return !fresh||source==null?"?":read(source.Tile);}
        public NumberMirrorSource Source(PanelTile mirror)=>NumberMirrors.IsMirror(mirror)&&sources.TryGetValue(mirror.NumberMirror,out var source)?source:null;
    }
    public static class NumberMirrors {
        public static bool IsMirror(PanelTile field)=>field?.Kind==PanelTileKind.TrainNumber&&field.NumberMirror!=null;
        public static void Validate(PanelTile field) {
            if(field.InlineNumberMirror&&!IsMirror(field))throw new InvalidDataException(LocalText.Select("An inline repeated number field needs an original source.","Ein Zugnummern-Anzeigefeld im Gleis benötigt eine ursprüngliche Quelle."));
            if(field.NumberId!=null&&(!Guid.TryParseExact(field.NumberId,"N",out _)||field.Kind!=PanelTileKind.TrainNumber||field.NumberMirror!=null)||
                field.NumberMirror!=null&&(!Guid.TryParseExact(field.NumberMirror,"N",out _)||field.Kind!=PanelTileKind.TrainNumber))
                throw new InvalidDataException("Ungültige Zugnummern-Quellverknüpfung.");
            if(IsMirror(field)&&(field.Reference!=""||field.AlternateSignal!=null||field.Section!=null||field.BlockId!=""||field.SecondBlock!=""||field.CircuitRails!=null||field.AutoSegment!=""||field.AutoMeters!=0||field.Number!=""||field.Prefix!=""||field.Ports!=0))
                throw new InvalidDataException("Eine Zugnummern-Anzeigekopie darf keine eigene Nummer, Signal-, Gleis- oder Abschnittsbindung besitzen.");
        }
        public static PanelTile Detached(PanelTile field) {
            var copy=field.Clone();copy.NumberMirror=null;copy.NumberId=null;copy.InlineNumberMirror=false;return copy;
        }
        public static PanelDocument CopyPanel(PanelDocument document) {
            var copy=document.Clone();foreach(var field in copy.Tiles)field.NumberId=null;return copy;
        }
        public static DeskLibrary Link(DeskLibrary library,string targetDesk,PanelTile target,string sourceDesk,int x,int y) {
            if(target?.Kind!=PanelTileKind.TrainNumber)throw new InvalidOperationException("Zuerst ein Zugnummernfeld als Ziel wählen.");
            if(targetDesk==sourceDesk&&target.X==x&&target.Y==y)throw new InvalidOperationException("Ein Feld kann nicht seine eigene Anzeigekopie sein.");
            var next=library.Clone();var sourceOwner=next.Desks.SingleOrDefault(d=>d.Id==sourceDesk)??throw new InvalidOperationException("Quellstellwerk fehlt.");
            var source=sourceOwner.Panel.At(x,y);if(source?.Kind!=PanelTileKind.TrainNumber||IsMirror(source))throw new InvalidOperationException("Ein ursprüngliches Zugnummernfeld als Quelle wählen.");
            var owner=next.Desks.SingleOrDefault(d=>d.Id==targetDesk)??throw new InvalidOperationException("Zielstellwerk fehlt.");var old=owner.Panel.At(target.X,target.Y);
            if(old?.NumberId!=null&&next.Desks.SelectMany(d=>d.Panel.Tiles).Any(t=>t.NumberMirror==old.NumberId))
                throw new InvalidOperationException("Dieses Quellenfeld wird bereits gespiegelt. Zuerst dessen Anzeigekopien ändern oder entfernen.");
            if(old!=null&&old.Kind!=PanelTileKind.TrainNumber)throw new InvalidOperationException("Das Zielfeld ist inzwischen anders belegt.");
            source.NumberId=source.NumberId??Guid.NewGuid().ToString("N");
            var mirror=target.Clone();mirror.NumberId=null;mirror.NumberMirror=source.NumberId;
            mirror.Reference=mirror.BlockId=mirror.SecondBlock=mirror.AutoSegment=mirror.Number=mirror.Prefix="";mirror.AlternateSignal=null;mirror.Section=null;mirror.CircuitRails=null;mirror.AutoMeters=0;
            mirror.Ports=mirror.Stem=mirror.Normal=0;owner.Panel.Put(mirror);next.Validate(next.WorldKey);return next;
        }
    }
}
