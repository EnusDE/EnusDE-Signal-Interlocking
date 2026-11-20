using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        string selectedMowWarehouse;
        public void OpenMaterials(){page=Page.Materials;ShowTool();}
        void SupplyAction(Action action){Act(()=>{action();EnsureRepository();RefreshScene();status=LocalText.Get("Materialvorgang abgeschlossen.");RebuildTool();});}
        void BuildMaterials(UIPanelBuilder c) {
            var shared=Entry.Shared;if(shared?.Ready!=true){c.AddLabel(LocalText.Select("Installation is loading.","Anlage wird geladen."));return;}
            var snapshot=shared.Snapshot();var pending=snapshot.Objects.Markers.Where(m=>m.Planned).ToArray();
            c.AddTitle(LocalText.Select("Materials and work orders","Material und Bauaufträge"),"");
            c.AddField(LocalText.Select("Open work orders","Offene Bauaufträge"),pending.Length.ToString());
            c.AddField(LocalText.Select("Materials still required","Noch benötigtes Material"),ConstructionPolicy.TotalMissing(snapshot).ToString());
            c.AddLabel(LocalText.Select("Order captive supply wagons through the game's Equipment page. Select Signal building supplies for an FL/FM flat car or Signal equipment for an XM boxcar. Deliver to a MOW warehouse, reload when needed, then couple the loaded wagons to an active MOW work car.","Materialwagen über die Fahrzeugbestellung des Spiels als captive Wagen bestellen. Signalbaumaterial für FL/FM-Flachwagen oder Signaltechnik für XM-Güterwagen wählen. Am MOW-Lager entladen, bei Bedarf wieder beladen und mit einem aktiven MOW-Arbeitswagen zur Baustelle bringen."));
            if(StateManager.IsHost)c.AddButton(LocalText.Select("Open native wagon ordering","Normale Fahrzeugbestellung öffnen"),()=>SupplyAction(NativeMaterialLogistics.OpenOrders));
            c.AddLabel(LocalText.Select("Buying cargo is a separate native wagon destination: Buy at an interchange, under EnusDE Signal & Interlocking. Bring the wagon to the interchange and allow its normal service/return cycle. No chat command is required.","Fracht kaufen ist ein eigenes natives Wagenziel: Kaufen am Interchange, unter EnusDE Signal & Interlocking. Den Wagen zur Übergabe bringen und deren normale Bedienung/Rücklieferung abwarten. Dafür ist kein Chatbefehl nötig."));
            var warehouses=shared.MaterialWarehouses.ToArray();
            if(shared.NativeMaterialError!=null)c.AddLabel(LocalText.Select("MOW storage link: ","MOW-Lageranbindung: ")+shared.NativeMaterialError);
            c.AddSection(LocalText.Select("MOW warehouses","MOW-Lager"));
            if(warehouses.Length==0)c.AddLabel(LocalText.Select("No matching MOW storage is available yet. Check the map's construction-material storage and its unlock conditions.","Noch kein passendes MOW-Lager verfügbar. Baustofflager und Freischaltungen der Karte prüfen."));
            if(warehouses.Length>0) {
                if(!warehouses.Any(w=>w.Id==selectedMowWarehouse))selectedMowWarehouse=warehouses[0].Id;
                c.AddSearchChoice(LocalText.Select("MOW warehouse","MOW-Lager"),warehouses.Select(w=>w.Name).ToList(),Array.FindIndex(warehouses,w=>w.Id==selectedMowWarehouse),i=>{selectedMowWarehouse=warehouses[i].Id;RebuildTool();});
                var store=warehouses.First(w=>w.Id==selectedMowWarehouse);
                c.AddLabel(store.Name);
                c.AddField(LocalText.Select("Stock","Bestand"),()=>NativeMaterialAmounts.Packages(store.Quantity(false))+" / "+NativeMaterialAmounts.Packages(store.Quantity(true)),UIPanelBuilder.Frequency.Periodic);
            }
            c.AddLabel(LocalText.Select("Stock: building supplies / signal equipment. Native warehouse loading and unloading handles the wagons; no second mod depot is required.","Bestand: Signalbaumaterial / Signaltechnik. Be- und Entladen erfolgt über das Lager des Spiels; ein zusätzliches Mod-Lager ist nicht nötig."));
            if(StateManager.IsHost)foreach(var depot in snapshot.Economy.Depots.Where(d=>d.Stock.B>0||d.Stock.T>0)) {
                var legacy=depot;
                c.AddSection(LocalText.Select("Retained legacy stock: ","Erhaltener alter Bestand: ")+legacy.Name);
                c.AddLabel(legacy.Stock.ToString());
                if(warehouses.Length>0)c.AddButton(LocalText.Select("Transfer to selected MOW warehouse","In das gewählte MOW-Lager übernehmen"),()=>SupplyAction(()=>shared.TransferLegacyStock(legacy.Id,selectedMowWarehouse)));
            }
            c.AddSection(LocalText.Select("Construction sites","Baustellen"));
            if(pending.Length==0)c.AddLabel(LocalText.Select("No planned installations. Place a construction plan in build mode to see its material requirements here.","Keine Anlagen geplant. Im Baumodus einen Bauplan platzieren; sein Materialbedarf erscheint anschließend hier."));
            foreach(var plan in pending) {
                string id=plan.Id;
                c.AddLabel(plan.DisplayTitle+" · "+MaterialEconomy.Missing(snapshot,plan));
                c.AddField(LocalText.Select("Status","Status"),()=>shared.SignalWorkStatus(id),UIPanelBuilder.Frequency.Periodic);
                if(StateManager.IsHost&&StateManager.IsSandbox)c.AddButton(plan.DisplayTitle+LocalText.Select(" · finish free (Sandbox)"," · kostenlos fertigstellen (Sandbox)"),()=>SupplyAction(()=>shared.FinishSandbox(id)));
            }
            c.AddLabel(LocalText.Select("Activate the MOW equipment and stop within 250 track metres of the site. Closing build mode retains all confirmed work orders. Shared construction is managed by the host.","MOW-Ausrüstung aktivieren und innerhalb von 250 Gleismetern bei der Baustelle anhalten. Das Beenden des Baumodus erhält bestätigte Bauaufträge. Gemeinsamen Bau verwaltet der Host."));
            c.AddButton(LocalText.Get("Anzeige aktualisieren"),RebuildTool);
        }
        void BuildReadOnly(UI.Builder.UIPanelBuilder c) {var row=Selected;if(row==null){c.AddLabel(LocalText.Get("Objekt nicht mehr vorhanden."));return;}c.AddTitle(row.Name,MarkerRecord.KindLabel(row.Kind));c.AddLabel(row.Planned?"Baustelle · noch nicht betriebsbereit":"Signalbild: "+Entry.SignalAspect(row.Id));if(StateManager.IsHost)c.AddButton(LocalText.Select("Edit in build mode","Im Baumodus bearbeiten"),()=>{int side=directionTab;page=Page.Configure;LoadDraft();directionTab=side;ShowTool();});else c.AddLabel(LocalText.Get("Die Konfiguration übernimmt der Host."));}
    }
}
