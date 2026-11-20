using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        static object CaptureDiagnosticPart(Func<object> capture){try{return capture();}catch(Exception e){return new{capture_error=e.Message};}}
        static object DiagnosticLocation(Location location) {
            if(!location.IsValid)return new{valid=false};
            var at=location.WithEnd(TrackSegment.End.A);return new{valid=true,segment=at.segment.id,meters=at.distance};
        }
        internal Dictionary<string,object> DiagnosticSnapshot() {
            if(stopped||StateManager.IsUnloading||Entry.Shared?.ConfigurationReady!=true)throw new InvalidOperationException(WindowText("Load the session before exporting diagnostics.","Vor dem Diagnoseexport den Spielstand laden."));
            var shared=Entry.Shared;var runs=VisibleRuns.ToArray();
            var result=new Dictionary<string,object>();
            result["manifest.json"]=new{schema=1,mod_version=Entry.Version,game_version=Application.version,game_assembly=typeof(StateManager).Assembly.GetName().Version.ToString(),utc=DateTime.UtcNow,
                host=StateManager.IsHost,client_live_fresh=shared.LiveFresh,world=shared.WorldKey,desk=deskId,configuration_version=shared.ConfigurationVersion,installation_version=shared.InstallationVersion,
                status=status,last_action_error=workspaceActionError,selected_start=panelButtons.Start,selected_target=panelButtons.End,shunting=shunting,recordings=Entry.RecordingsDirectory,cold_start_policy="Active route authority is not restored; signals start at Stop.",
                instructions_en="Point-in-time diagnostic data, not a backup or live remote control. Last request readings were taken at request completion; current readings are taken during this export. Client readings are not host authority. No files are uploaded automatically.",
                instructions_de="Diagnosemomentaufnahme, keine Sicherung und keine Fernsteuerung. Die Abfragen des letzten Stellversuchs stammen vom Ende der Anforderung; aktuelle Abfragen vom Export. Clientdaten sind keine Host-Freigabe. Es erfolgt kein automatischer Upload."};
            result["installation.json"]=shared.Snapshot();result["desks.json"]=shared.Desks();result["saved-routes.json"]=shared.Routes();
            result["last-request.json"]=engine.LastRequest;
            result["recent-requests.json"]=engine.RecentRequests;
            result["active-routes.json"]=runs.Select(r=>new{definition=r.Route,status=SharedRouteStatus.From(r,engine.RemainingWait(r)),cars=r.TrainCars,start_passage=r.StartPassageVerified,number_handled=r.NumberPassageHandled,passage_number=r.NumberPassageNumber}).ToArray();
            result["direction-state.json"]=new{blocks=directionBlocks.Leases,lines=lineDirections.States,automation=AutomationFrame()};
            result["network.json"]=CaptureDiagnosticPart(()=>CaptureNetwork());
            result["boundary-origins.json"]=CaptureDiagnosticPart(()=>physicalBoundaries.Values.Select(EffectivePhysicalBoundary).Select(b=>new{id=b.Id,caption=CircuitIdentity.Caption(b),source=b.DisplayTitle,enabled=CircuitIdentity.Enabled(b),anchor=b.Anchor,shared_points=SharedSignalPoints(b.Id),
                drawn_on=desks.Desks.Where(d=>d.Panel.Tiles.Any(t=>DiagramBoundary(t,b.Id))).Select(d=>d.Code).ToArray()}).ToArray());
            result["vehicles.json"]=CaptureDiagnosticPart(()=>{
                var cars=TrainController.Shared?.Cars.Where(c=>c!=null&&!c.IsInBardo).Take(4097).ToArray();
                return new{truncated=cars?.Length>4096,cars=cars?.Take(4096).Select(c=>new{id=c.id,front=DiagnosticLocation(c.LocationA),rear=DiagnosticLocation(c.LocationB),derailed=c.IsDerailed,
                    coupled_a=c.CoupledTo(Model.Car.LogicalEnd.A)?.id,coupled_b=c.CoupledTo(Model.Car.LogicalEnd.B)?.id}).ToArray()};
            });
            result["current-readings.json"]=CaptureDiagnosticPart(()=>{
                if(!StateManager.IsHost)return new{unavailable="Export from the host for authoritative occupancy readings."};
                var definitions=runs.Select(r=>r.Route).Concat(engine.LastRequest?.Definitions??Array.Empty<RouteDefinition>()).Concat(routes).ToArray();
                var sections=definitions.SelectMany(r=>r.Sections.Concat(r.Approach).Concat(r.Switches.SelectMany(p=>p.Protection??Enumerable.Empty<TrackSection>())))
                    .GroupBy(s=>s.Key).Select(g=>g.First()).Take(8193).ToArray();
                using(var reader=new SignalWorldBridge(this,transform,8192))return new{truncated=sections.Length>8192,
                    sections=sections.Take(8192).Select(s=>reader.DiagnosticSection(s)).ToArray(),
                    block_limit=2048,blocks=desks.Blocks.Values.Take(2048).Select(b=>{var state=reader.Block(b,out var cars);return new{id=b.Id,boundary_a=b.BoundaryA,boundary_b=b.BoundaryB,point=b.Point,occupancy=state,cars=cars};}).ToArray(),
                    points=definitions.SelectMany(r=>r.Switches).GroupBy(p=>p.Node).Select(g=>g.Key).Select(id=>new{id,position=reader.SwitchPosition(id,out bool position)?(bool?)position:null}).ToArray()};
            });
            result["number-fields.json"]=CaptureDiagnosticPart(()=>{
                var index=NumberBindings(shared);
                var fields=desks.Desks.SelectMany(d=>d.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.TrainNumber).Select(t=>new{desk=d.Id,x=t.X,y=t.Y,reference=t.Reference,alternate=t.AlternateSignal,mirror=t.NumberMirror,
                    field=index.ForField(t),conflicting=index.ConflictingField(t),caption=NumberCaption(t)})).Take(2049).ToArray();
                return new{truncated=fields.Length>2048,display_limit_exceeded=numberDisplayOverflow,fields=fields.Take(2048).ToArray(),field_numbers=shared.NumberFields(),car_numbers=shared.NumberRegistry()};
            });
            return result;
        }
    }
    public sealed partial class SignalWorldBridge {
        internal object DiagnosticSection(TrackSection section) {
            var state=SectionWithCars(section,out var cars);var segment=Graph.Shared?.GetSegment(section.Segment);
            return new{section=section,occupancy=state,cars=cars,train_controller_available=TrainController.Shared!=null,segment_loaded=segment!=null,
                segment_enabled=segment!=null&&segment.Available&&segment.GroupEnabled,segment_length=segment==null?(double?)null:segment.GetLength(),
                occupancy_source="native vehicle track intervals",unresolved_vehicle_location=vehicleOccupancy?.GlobalUnknown??true,unresolved_vehicle_ids=vehicleOccupancy?.UnresolvedVehicles.Take(64).ToArray()};
        }
    }
}
