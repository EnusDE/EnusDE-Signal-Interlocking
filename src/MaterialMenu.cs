using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        string freightId,materialUnits="5",depotName="Materiallager 1";
        bool buyTechnology;
        public void OpenMaterials(){page=Page.Materials;ShowTool();}
        void SupplyAction(Action action){Act(()=>{action();EnsureRepository();RefreshScene();status=LocalText.Get("Materialvorgang abgeschlossen.");RebuildTool();});}
        void BuildMaterials(UIPanelBuilder c) {
            var shared=Entry.Shared;if(shared?.Ready!=true){c.AddLabel(shared?.Error??"Anlagenbestand wird noch geladen.");return;}var snapshot=shared.Snapshot();
            if(!shared.ConstructionUnlocked)c.AddLabel(LocalText.Select("Unlock construction in Milestones first; map prerequisites and any purchase price apply. Existing installations remain usable.","Signalbau zuerst unter Meilensteine freischalten; Kartenvoraussetzungen und ein möglicher Kaufpreis gelten. Bestehende Anlagen bleiben nutzbar."));
            c.AddField(LocalText.Select("Total material still required","Gesamter offener Materialbedarf"),ConstructionPolicy.TotalMissing(snapshot).ToString());
            c.AddTitle(LocalText.Get("Material und Bauaufträge"),snapshot.Economy.Enabled?"Wirtschaft aktiv · Mengenfaktor "+snapshot.Economy.Factor:"Kostenloser Testbetrieb");
            if(snapshot.Economy.Depots.Count>0){var depots=snapshot.Economy.Depots.ToArray();if(!depots.Any(d=>d.Id==shared.SelectedDepotId))shared.SelectedDepotId=depots[0].Id;c.AddSearchChoice(LocalText.Get("Materiallager"),depots.Select(d=>d.Name).ToList(),Array.FindIndex(depots,d=>d.Id==shared.SelectedDepotId),i=>shared.SelectedDepotId=depots[i].Id);}
            c.AddField(LocalText.Get("Bestände"),()=>{var data=Entry.Shared?.Snapshot();return data==null?"Nicht verfügbar":string.Join("\n",data.Economy.Depots.Select(d=>d.Name+": "+d.Stock));},UIPanelBuilder.Frequency.Periodic);
            if(StateManager.IsHost){
                var cars=TrainController.Shared==null?new Model.Car[0]:TrainController.Shared.Cars.Where(car=>car!=null&&car.IsOwnedByPlayer&&(car.CarType.ToString().StartsWith("FM",StringComparison.Ordinal)||car.CarType.ToString().StartsWith("XM",StringComparison.Ordinal))).OrderBy(car=>car.Ident.ToString()).ToArray();
                if(cars.Length>0){if(!cars.Any(car=>car.id==freightId))freightId=cars[0].id;c.AddSearchChoice(LocalText.Get("Eigener Güterwagen"),cars.Select(car=>car.Ident+" · "+car.CarType).ToList(),Array.FindIndex(cars,car=>car.id==freightId),i=>{freightId=cars[i].id;RebuildTool();});
                    c.AddField(LocalText.Get("Lagernamen"),c.AddInputField(depotName,v=>depotName=v,"Materiallager",48));c.AddButton(LocalText.Get("Materiallager am stehenden Wagen anlegen"),()=>SupplyAction(()=>shared.CreateDepot(freightId,depotName)));
                    c.AddField(LocalText.Get("Fracht"),c.AddDropdown(new List<string>{LocalText.Get("Signalbaumaterial · FM-Flachwagen"),LocalText.Get("Signaltechnik · XM-Güterwagen")},buyTechnology?1:0,i=>{buyTechnology=i==1;RebuildTool();}));c.AddField(LocalText.Get("Baupakete"),c.AddInputField(materialUnits,v=>materialUnits=v,"1–100",3));
                    string buyToken=Guid.NewGuid().ToString("N"),unloadToken=Guid.NewGuid().ToString("N");
                    c.AddButton(LocalText.Get("Am Übergabegleis kaufen und beladen"),()=>SupplyAction(()=>{int units;if(!int.TryParse(materialUnits,out units))throw new InvalidOperationException("Ganze Baupaketmenge eingeben.");shared.Buy(freightId,buyTechnology,units,buyToken);}));
                    c.AddButton(LocalText.Get("In das gewählte Materiallager entladen"),()=>SupplyAction(()=>shared.Unload(freightId,shared.SelectedDepotId,unloadToken)));
                    c.AddLabel(LocalText.Get("Kauf nur auf einem aktiven Übergabegleis, eigener stehender Wagen ohne Frachtauftrag. Baumaterial: 250 $/Paket · Signaltechnik: 500 $/Paket. Entladen bis 30 m vom Lager; Bauen bis 250 m."));
                }else c.AddLabel(LocalText.Get("Für Lieferungen einen eigenen FM-Flachwagen oder XM-Güterwagen bereitstellen."));
            }
            var plans=snapshot.Objects.Markers.Where(m=>m.Planned).ToArray();c.AddSection(LocalText.Get("Geplante Anlagen"));if(plans.Length==0)c.AddLabel(LocalText.Get("Keine offenen Bauaufträge."));foreach(var plan in plans){string id=plan.Id;c.AddLabel(plan.Name+" · "+MarkerRecord.KindLabel(plan.Kind)+" · benötigt "+MaterialEconomy.Missing(snapshot,plan));if(StateManager.IsHost) {
                    c.AddButton(plan.Name+" fertigstellen",()=>SupplyAction(()=>shared.Finish(id,shared.SelectedDepotId)));
                    if(snapshot.Economy.Enabled&&StateManager.IsSandbox)
                        c.AddButton(plan.Name+" ohne Material fertigstellen (Sandbox)",()=>SupplyAction(()=>shared.FinishSandbox(id)));
                }
            }
            if(StateManager.IsHost)c.AddLabel(LocalText.Get("Betriebsart und Materialfaktor: Auswahlfenster → Einstellungen. Tafelzeichnen und Tafelkopien sind kostenlos."));
            c.AddButton(LocalText.Get("Anzeige aktualisieren"),RebuildTool);
        }
        void BuildReadOnly(UI.Builder.UIPanelBuilder c) {var row=Selected;if(row==null){c.AddLabel(LocalText.Get("Objekt nicht mehr vorhanden."));return;}c.AddTitle(row.Name,MarkerRecord.KindLabel(row.Kind));c.AddLabel(row.Planned?"Baustelle · noch nicht betriebsbereit":"Signalbild: "+Entry.SignalAspect(row.Id));c.AddLabel(LocalText.Get("Die Konfiguration übernimmt der Host."));}
    }
}
