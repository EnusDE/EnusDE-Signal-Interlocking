using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class LineTargetBinding {
        public string Target,Station,Arrival,Error;
        public int Direction;
        public LineTrackDefinition Line;
    }
    public static class LineTargetBindings {
        public static Dictionary<string,LineTargetBinding> Index(DeskLibrary library) {
            var result=new Dictionary<string,LineTargetBinding>();if(library==null)return result;
            var targets=AuxiliaryTarget.Index(library.Desks.SelectMany(d=>d.Panel.Tiles));
            var modules=library.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.LinePermission&&!t.ReadOnlySignal&&t.PermissionAuxiliary!=null&&t.Reference!="").ToLookup(t=>t.PermissionAuxiliary);
            foreach(var target in targets.Values.Where(t=>t.Function==AuxiliaryFunction.RouteDestination)) {
                var ids=modules[target.Id].Select(t=>t.Reference).Concat(new[]{target.PermissionLine}).Where(id=>id!="").Distinct().ToArray();if(ids.Length==0)continue;
                if(!target.Train&&target.PermissionLine=="")continue;
                var line=library.FreeLines.SingleOrDefault(l=>l.Id==ids[0]);
                var owners=library.Desks.Where(d=>!d.IsMaster&&d.Panel.Tiles.Any(t=>!t.ReadOnlySignal&&t.Target?.Id==target.Id)).ToArray();var owner=owners.Length==1?owners[0]:null;
                string error=ids.Length!=1?LocalText.Select("Auxiliary destination is linked to different line permissions.","Hilfsziel ist mit unterschiedlichen Streckenerlaubnissen verknüpft."):line==null?LocalText.Select("The destination's line permission is missing.","Die Streckenerlaubnis des Ziels fehlt."):
                    !target.Train||owner==null||owner.Id!=line.StationA&&owner.Id!=line.StationB?LocalText.Select("Assign this train destination to one neighbouring local interlocking.","Dieses Zugziel einem der örtlichen Nachbarstellwerke zuordnen."):null;
                int direction=owner?.Id==line?.StationA?1:-1;
                result[target.Id]=new LineTargetBinding{Target=target.Id,Station=owner?.Id,Line=line,Error=error,Direction=direction,Arrival=line==null?"":direction==1?line.ForwardEnd:line.ReverseEnd};
            }
            return result;
        }
        public static string Signature(LineTargetBinding binding)=>SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(binding.Target+"|"+binding.Line?.Id+"|"+binding.Station+"|"+binding.Arrival+"|"+binding.Direction));
        public static void Tag(RouteChainPlan plan,LineTargetBinding binding) {plan.Routes=plan.Routes.Select(r=>r.Clone()).ToList();foreach(var route in plan.Routes){route.LineTarget=binding.Target;route.LineTargetSignature=Signature(binding);}}
        public static RouteChainPlan ClearPrefix(RouteChainPlan plan,LineTargetBinding binding,Func<RouteDefinition,(TrackOccupancy State,string Reason)> read) {
            var blockSignals=binding.Line.BlockUnits.Select(unit=>binding.Direction==1?unit.Forward:unit.Reverse).Where(id=>id!="").ToHashSet();
            if(blockSignals.Count==0)return plan;
            for(int i=0;i<plan.Routes.Count;i++) {
                var (state,reason)=read(plan.Routes[i]);
                if(state==TrackOccupancy.Clear)continue;
                if(state!=TrackOccupancy.Occupied||i==0||!blockSignals.Contains(plan.Routes[i-1].Exit))return plan;
                return new RouteChainPlan{Routes=plan.Routes.Take(i).ToList(),EndReason=LocalText.Select(
                    "Only as far as the last clear block signal. Remainder not set. ","Nur bis zum letzten erreichbaren Blocksignal. Rest nicht gestellt. ")+reason};
            }
            return plan;
        }
        public static void RequirePlan(DeskLibrary library,IReadOnlyList<RouteDefinition> plan) {
            var index=Index(library);
            foreach(string id in plan.Select(r=>r.LineTarget??(index.ContainsKey(r.Exit)?r.Exit:null)).Where(id=>id!=null).Distinct()) {
                if(!index.TryGetValue(id,out var binding))throw new InvalidOperationException(LocalText.Select("The requested destination lost its line-permission binding. Request the route again.","Das angeforderte Ziel hat seine Erlaubnisbindung verloren. Fahrstraße erneut anfordern."));
                if(binding.Error!=null)throw new InvalidOperationException(binding.Error);
                if(plan.Any(r=>r.LineTarget==id&&r.LineTargetSignature!=Signature(binding)))throw new InvalidOperationException(LocalText.Select("Line-destination assignment changed. Recalculate the complete request.","Streckenzielzuordnung geändert. Vollständige Anforderung neu berechnen."));
                if(plan.Any(r=>r.Shunting)||StationAutomationPolicy.Owner(library,plan[0].Entry)?.Id!=binding.Station)throw new InvalidOperationException(LocalText.Select("Use this line destination for a train departure from its assigned neighbouring interlocking.","Dieses Streckenziel für eine Zugausfahrt vom zugeordneten Nachbarstellwerk verwenden."));
                if(plan.Last().Exit==binding.Target)throw new InvalidOperationException(LocalText.Select("This old local destination route must be recalculated through the block signals to the neighbour's arrival signal.","Diesen alten örtlichen Zielfahrweg durch die Blocksignale bis zum Ankunftssignal des Nachbarn neu berechnen."));
                if(plan.Last().Exit==binding.Arrival&&GfmCoverage.Gaps(binding.Line.Axis.Select(p=>p.Section),plan.SelectMany(r=>r.Path??new List<DirectedRouteSection>()).Select(p=>p.Section)).Count>0)
                    throw new InvalidOperationException(LocalText.Select("The requested route does not cover the complete assigned line.","Der angeforderte Fahrweg deckt die zugeordnete Strecke nicht vollständig ab."));
            }
        }
        public static bool CanControlBlocks(DeskLibrary library,string desk,IReadOnlyList<RouteDefinition> plan) {
            if(plan.Count==0||StationAutomationPolicy.Owner(library,plan[0].Entry)?.Id!=desk)return false;
            var targets=plan.Select(r=>r.LineTarget).Where(id=>id!=null).Distinct().ToArray();if(targets.Length!=1||!Index(library).TryGetValue(targets[0],out var binding)||binding.Error!=null||binding.Station!=desk)return false;
            var blocks=new HashSet<string>(binding.Line.BlockUnits.Select(u=>binding.Direction==1?u.Forward:u.Reverse).Where(id=>id!=""));
            return plan.All(r=>StationAutomationPolicy.Owner(library,r.Entry)?.Id==desk||blocks.Contains(r.Entry)&&StationAutomationPolicy.Owner(library,r.Entry)?.Id==binding.Line.BlockOwner);
        }
        static string Shape(LineTargetBinding b)=>b.Line?.Id+"|"+b.Station+"|"+b.Arrival+"|"+b.Error;
        public static string[] ChangedTargets(DeskLibrary before,DeskLibrary after,out string[] lines) {
            var a=Index(before);var b=Index(after);var ids=a.Keys.Union(b.Keys).Where(id=>!a.ContainsKey(id)||!b.ContainsKey(id)||Shape(a[id])!=Shape(b[id])).ToArray();
            lines=ids.SelectMany(id=>(a.TryGetValue(id,out var old)?new[]{old.Line?.Id}:Array.Empty<string>()).Concat(b.TryGetValue(id,out var next)?new[]{next.Line?.Id}:Array.Empty<string>())).Where(id=>id!=null).Distinct().ToArray();return ids;
        }
    }
    public sealed partial class SignalRuntime {
        DeskLibrary lineTargetLibrary;
        Dictionary<string,LineTargetBinding> lineTargets=new Dictionary<string,LineTargetBinding>();
        LineTargetBinding BoundLineTarget(string id) {
            if(!ReferenceEquals(desks,lineTargetLibrary)){lineTargets=LineTargetBindings.Index(desks);lineTargetLibrary=desks;}
            return id!=null&&lineTargets.TryGetValue(id,out var value)?value:null;
        }
        string ResolveLineDestination(string source,string target,RouteKind kind) {
            var binding=BoundLineTarget(target);if(binding==null)return target;
            if(binding.Error!=null)throw new InvalidOperationException(binding.Error);
            if(kind==RouteKind.Shunt||SignalStation(source)?.Id!=binding.Station)throw new InvalidOperationException(WindowText("This bound destination requires a train departure from its assigned interlocking.","Dieses gebundene Ziel erfordert eine Zugausfahrt vom zugeordneten Stellwerk."));
            var arrival=Object(binding.Arrival);if(arrival?.Kind!=MarkerRecord.Main||arrival.Planned)throw new InvalidOperationException(WindowText("Assign an available main arrival signal at the other end of this line.","Am anderen Ende dieser Strecke ein verfügbares Hauptsignal als Ankunft zuordnen."));
            RefreshLineDirections();lineDirections.RequirePermission(binding.Line.Id,binding.Direction);return arrival.Id;
        }
        LineTargetBinding RouteLineTarget(RouteDefinition route) {
            var binding=BoundLineTarget(route.LineTarget??route.Exit);
            if(route.LineTarget!=null&&(binding==null||route.LineTargetSignature!=LineTargetBindings.Signature(binding)))throw new InvalidOperationException(WindowText("The route's line destination changed or is no longer linked. Recalculate the request.","Das Streckenziel der Fahrstraße wurde geändert oder ist nicht mehr verknüpft. Anforderung neu berechnen."));return binding;
        }
        internal bool LineTargetPathValid(RouteDefinition route) {try{var binding=RouteLineTarget(route);return binding==null||binding.Error==null;}catch{return false;}}
    }
}
