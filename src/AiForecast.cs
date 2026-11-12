using System;
using System.Linq;
using System.Reflection;
using Model.AI;
using System.Collections.Generic;
using Track;
using Track.Signals;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class AheadSignal {public string Id;public double Distance;public SignalAspect Aspect;}
    public sealed class ForecastTarget {public string Id;public double Distance;public int Speed;public int? Approach;}
    public static class AiForecast {
        public static ForecastTarget Select(SignalAspect current,IEnumerable<AheadSignal> signals){if(current!=SignalAspect.Clear&&current!=SignalAspect.DivergingClear&&current!=SignalAspect.Restricting)return null;foreach(var signal in signals.Where(s=>s.Distance>.1&&s.Distance<=2000).OrderBy(s=>s.Distance)){if(signal.Aspect==SignalAspect.Clear||(current==SignalAspect.DivergingClear||current==SignalAspect.Restricting)&&(signal.Aspect==SignalAspect.DivergingClear||signal.Aspect==SignalAspect.Restricting||signal.Aspect==SignalAspect.Approach||signal.Aspect==SignalAspect.DivergingApproach))continue;int speed=signal.Aspect==SignalAspect.Approach||signal.Aspect==SignalAspect.DivergingApproach||signal.Aspect==SignalAspect.DivergingClear||signal.Aspect==SignalAspect.Restricting?15:0;return new ForecastTarget{Id=signal.Id,Distance=signal.Distance,Speed=speed,Approach=current!=SignalAspect.Clear?(int?)15:null};}return null;}
    }
    public sealed partial class SignalRuntime {
        sealed class Ahead {public CTCSignal Signal;public float Distance;}
        sealed class AheadCache {public double Until;public long Version;public List<Ahead> Signals;}
        readonly Dictionary<string,AheadCache> aheadCache=new Dictionary<string,AheadCache>();
        readonly Dictionary<string,string> forecastStates=new Dictionary<string,string>();
        readonly Dictionary<string,double> forecastTimes=new Dictionary<string,double>();
        string forecastError;
        static readonly FieldInfo PlannerLocomotive=typeof(AutoEngineerPlanner).GetField("_locomotive",BindingFlags.Instance|BindingFlags.NonPublic);
        readonly Dictionary<AutoEngineerPlanner,(string Signal,float Last)> observedStops=new Dictionary<AutoEngineerPlanner,(string,float)>();
        bool stopTraceFailed;

        internal void ObserveStopTarget(AutoEngineerPlanner planner,CTCSignal signal,float distance) {
            if(stopTraceFailed||stopped||!Game.State.StateManager.IsHost||signal==null)return;
            try {
                string id=signal.id;
                float now=Time.realtimeSinceStartup;
                if(observedStops.TryGetValue(planner,out var last)&&now-last.Last<1)return;
                if(last.Signal==id&&now-last.Last<5)return;
                if(observedStops.Count>=256)observedStops.Clear();
                observedStops[planner]=(id,now);
                var loco=PlannerLocomotive?.GetValue(planner) as Model.BaseLocomotive;
                var own=signal as RuntimeSignalProxy;
                float? meters=float.IsNaN(distance)||float.IsInfinity(distance)?(float?)null:distance;
                Entry.RecordSignalEvent("ai_stop_target",new{vehicle_id=loco?.id,signal_id=id,code=own==null?id:Code(own.ObjectId),distance_m=meters,source="AutoEngineerPlanner.SetSignalSpeedRestriction",movement_verified=false});
            } catch(Exception e) {
                stopTraceFailed=true;
                log?.Warning("AE-Haltezieldiagnose: "+e.Message);
            }
        }
        public void ForecastFailure(Exception error){if(forecastError==error.Message)return;forecastError=error.Message;log?.Warning("AE-Vorausschau: "+error.Message);Entry.RecordSignalEvent("ai_forecast_health",new{available=false,error=error.Message});}
        public void ForecastRestriction(ref int? speed,ref int? approach,ref float distance,ref CTCSignal signal,ref bool limited) {
            var own=signal as RuntimeSignalProxy;if(own==null||!Game.State.StateManager.IsHost||stopped||loadFailed)return;var current=NativeAspect(own.ObjectId);if(current!=SignalAspect.Clear&&current!=SignalAspect.DivergingClear&&current!=SignalAspect.Restricting||speed==0)return;
            var graph=Graph.Shared;if(graph==null)return;AheadCache cache;if(!aheadCache.TryGetValue(own.ObjectId,out cache)||Time.realtimeSinceStartup>=cache.Until||cache.Version!=(Entry.Shared?.InstallationVersion??0)){Graph.NamedLocation at;if(!graph.TryGetNamedLocation(own.id,out at))return;cache=new AheadCache{Until=Time.realtimeSinceStartup+.25,Version=Entry.Shared?.InstallationVersion??0,Signals=new List<Ahead>()};foreach(var named in graph.EnumerateTrackMarkers(at.Location,2000,true)){var next=named.GetSignal();if(next==null||!next.isActiveAndEnabled||next==own||Entry.Shared?.IgnoresStock(next.id)==true)continue;float delta=graph.GetDistanceBetweenClose(at.Location,named.Location);if(delta>.1f)cache.Signals.Add(new Ahead{Signal=next,Distance=delta});if(cache.Signals.Count>=32)break;}if(aheadCache.Count>=256)aheadCache.Clear();aheadCache[own.ObjectId]=cache;}
            var forecast=AiForecast.Select(current,cache.Signals.Where(s=>s.Signal!=null&&s.Signal.isActiveAndEnabled).Select(s=>new AheadSignal{Id=s.Signal.id,Distance=s.Distance,Aspect=RuntimeSignalProxy.PublishedAspect(s.Signal)}));if(forecast==null)return;var target=cache.Signals.FirstOrDefault(s=>s.Signal!=null&&s.Signal.id==forecast.Id);if(target==null)return;
            int? cap=approach;if(speed.HasValue&&speed>0)cap=cap.HasValue?Math.Min(cap.Value,speed.Value):speed;if(forecast.Approach.HasValue)cap=cap.HasValue?Math.Min(cap.Value,forecast.Approach.Value):forecast.Approach;speed=forecast.Speed;approach=cap;distance+=(float)forecast.Distance;signal=target.Signal;limited=false;
            if(Entry.WantsSignalEvents){string state=signal.id+":"+speed+":"+approach;string previous;double last;if(!forecastStates.TryGetValue(own.ObjectId,out previous)||previous!=state||!forecastTimes.TryGetValue(own.ObjectId,out last)||Time.realtimeSinceStartup-last>=5){if(forecastStates.Count>=256){forecastStates.Clear();forecastTimes.Clear();}forecastStates[own.ObjectId]=state;forecastTimes[own.ObjectId]=Time.realtimeSinceStartup;Entry.RecordSignalEvent("ai_forecast_target",new{current=own.ObjectId,target=signal.id,target_mph=speed,approach_mph=approach,distance_m=distance,source="native target arguments",movement_verified=false});}}
        }
    }
}
