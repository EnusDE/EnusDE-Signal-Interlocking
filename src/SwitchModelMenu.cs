using System;
using System.Collections.Generic;
using System.Linq;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        NativeSwitchModels switchModels;
        DeskLibrary switchModelLibrary;
        Graph switchModelGraph;
        readonly HashSet<string> displayedElectricStands=new HashSet<string>(StringComparer.Ordinal);
        bool ElectricStand(string id)=>PointDrivePolicy.Electric(desks,id);
        void RefreshSwitchModels() {
            var graph=Graph.Shared;if(graph==null||desks==null)return;
            if(ReferenceEquals(desks,switchModelLibrary)&&ReferenceEquals(graph,switchModelGraph))return;
            var wanted=new HashSet<string>(desks.Points.Keys.Where(id=>PointDrivePolicy.Electric(desks,id)),StringComparer.Ordinal);
            if(!ReferenceEquals(graph,switchModelGraph))displayedElectricStands.Clear();
            var manager=TrackObjectManager.Instance;if(manager==null)return;
            foreach(var id in wanted.Union(displayedElectricStands).Where(id=>wanted.Contains(id)!=displayedElectricStands.Contains(id))) {
                var node=graph.GetNode(id);if(node!=null)manager.SetNeedsRebuild(node);
            }
            displayedElectricStands.Clear();displayedElectricStands.UnionWith(wanted);switchModelLibrary=desks;switchModelGraph=graph;
        }
        void RestoreSwitchModels() {
            switchModels?.Dispose();
            if(!Game.State.StateManager.IsUnloading&&TrackObjectManager.Instance!=null&&Graph.Shared!=null)
                foreach(var id in displayedElectricStands){var node=Graph.Shared.GetNode(id);if(node!=null)TrackObjectManager.Instance.SetNeedsRebuild(node);}
            displayedElectricStands.Clear();switchModelLibrary=null;switchModelGraph=null;
        }
        internal void ConfigureSwitchModel(string point,bool electric,bool wholeDesk) {
            RequireEdit();if(switchModels==null)throw new InvalidOperationException("Native Weichenmodelle nicht verfügbar.");
            if(!desks.Points.TryGetValue(point,out var selected))throw new InvalidOperationException("Weiche nicht mehr verfügbar.");
            string owner=PointDrivePolicy.LinkedDesk(desks,point);
            if(electric&&owner==null)throw new InvalidOperationException(LocalText.Select("Assign the point to exactly one local interlocking before enabling an electric drive.","Die Weiche vor dem elektrischen Antrieb genau einem örtlichen Stellwerk zuordnen."));
            var next=desks.Clone();foreach(var pair in next.Points.Where(p=>wholeDesk?owner!=null&&PointDrivePolicy.LinkedDesk(next,p.Key)==owner:p.Key==point))pair.Value.ElectricStand=electric;
            deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;RefreshSwitchModels();
        }
    }
}
