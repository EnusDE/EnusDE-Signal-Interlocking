using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class GfmDetectionRefresh {
        public static ExistingDetectionResult Clear(DeskLibrary library,IEnumerable<string> deskIds) {
            var ids=new HashSet<string>(deskIds);
            bool Boundary(PanelTile tile,string id)=>tile.CircuitBoundary==id||tile.Reference==id||tile.AlternateSignal==id||tile.Kind==PanelTileKind.Switch&&TrackCircuit.TryPointId(id,out var point,out _)&&tile.Reference==point;
            var result=Build(library,ids,new NetworkResult(),Boundary,null,null,true);result.ResetOnly=true;
            bool all=ids.Count==library.Desks.Count;
            var points=new HashSet<string>(library.Desks.Where(d=>ids.Contains(d.Id)).SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.Switch).Select(t=>t.Reference));
            foreach(var point in result.Library.Points.Where(p=>all||ids.Contains(p.Value.Desk)||points.Contains(p.Key))){point.Value.ShowCircuit=false;point.Value.BoundaryLinks.Clear();}
            result.Library.Automation.AutomaticGfmCatalog=false;result.Library.Validate(library.WorldKey);return result;
        }
        public static IEnumerable<string> References(PanelTile tile)=>new[]{tile.BlockId,tile.SecondBlock}.Concat(tile.CircuitRails?.Values??Enumerable.Empty<string>()).Where(id=>!string.IsNullOrEmpty(id));
        public static ExistingDetectionResult Build(DeskLibrary library,IEnumerable<string> deskIds,NetworkResult network,Func<PanelTile,string,bool> boundary,IReadOnlyDictionary<string,string> aliases,NetworkInput input,bool reset) {
            var ids=new HashSet<string>(deskIds,StringComparer.Ordinal);var next=library.Clone();
            if(ids.Count==0||ids.Any(id=>!next.Desks.Any(d=>d.Id==id)))throw new InvalidOperationException(LocalText.Select("Select an existing interlocking for detection.","Vorhandenes Stellwerk für die Erkennung wählen."));
            var result=new ExistingDetectionResult{Library=next,Rebuilt=reset};
            if(reset) {
                var tiles=next.Desks.Where(d=>ids.Contains(d.Id)).SelectMany(d=>d.Panel.Tiles).ToArray();
                var local=new HashSet<string>(tiles.SelectMany(References));
                var outside=new HashSet<string>(next.Desks.Where(d=>!ids.Contains(d.Id)).SelectMany(d=>d.Panel.Tiles).SelectMany(References));
                bool all=ids.Count==next.Desks.Count;
                var remove=next.Blocks.Values.Where(b=>b.IsCircuit&&!outside.Contains(b.Id)&&(all||local.Contains(b.Id)||
                    tiles.Any(t=>boundary(t,b.BoundaryA)||boundary(t,b.BoundaryB)))).Select(b=>b.Id).ToArray();
                foreach(var tile in tiles) {
                    if(tile.Section!=null||tile.BlockId!=""||tile.SecondBlock!=""||tile.AutoSegment!=""||tile.CircuitRails!=null)result.ResetFields++;
                    tile.Section=null;tile.BlockId=tile.SecondBlock=tile.AutoSegment="";tile.AutoMeters=0;tile.CircuitRails=null;
                }
                foreach(string id in remove)next.Blocks.Remove(id);result.Removed=remove.Length;
            }
            foreach(string id in ids.OrderBy(id=>id,StringComparer.Ordinal)) {
                var detected=ExistingDiagramDetection.Build(next,id,network,boundary,aliases,input);next=detected.Library;
                result.Circuits+=detected.Circuits;result.Points+=detected.Points;result.Fields+=detected.Fields;result.Shared+=detected.Shared;result.Calibrated+=detected.Calibrated;
                string code=next.Desks.Single(d=>d.Id==id).Code;
                foreach(string error in detected.Unresolved)result.Unresolved.Add(code+" · "+error);
                foreach(var issue in detected.Issues)result.Issues.Add(new GfmDetectionIssue{Desk=id,Message=code+" · "+issue.Message,Block=issue.Block});
            }
            result.Library=next;result.Library.Validate(library.WorldKey);return result;
        }
    }
}
