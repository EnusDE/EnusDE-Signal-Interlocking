using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using Game.State;
using Railroader.Modding;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace EnusDE.Signaltechnik {
    // Construction and model view; passive diagnostics and interlocking stay in separate modules.
    public sealed partial class MarkerRuntime : MonoBehaviour {
        const string WindowId="enusde.signaltechnik.grenzzeichen";
        readonly MarkerTrackGeometry geometry=new MarkerTrackGeometry();
        readonly MarkerEditSession edit=new MarkerEditSession();
        MarkerScene scene;
        MarkerRepository repository;
        MarkerRecord[] records=new MarkerRecord[0];
        MarkerOptions options=new MarkerOptions(),previewOptions=new MarkerOptions();
        readonly ExistingSignDisplay existingSigns=new ExistingSignDisplay();
        readonly HashSet<string> shown=new HashSet<string>();
        SignUnitPreference units;
        string speedInput="20";
        bool speedInKmh;
        bool speedDirty,useHostInput;
        double draftSpeed;
        string lateralInput,heightInput,scaleInput;
        MastConfiguration mastDraft;
        enum Page { Build,Objects,Configure,Placement,Materials,ReadOnly,Stock }
        Page page;
        int directionTab;
        string codeInput="",selectedEditFor,linkedInput,displayNameInput,labelScaleInput;
        bool facingInput;
        double placeAfter;
        ModePicker modePicker;
        Track.Location? buildTrack;
        public MarkerRecord[] Objects=>records.Select(r=>r.Clone()).ToArray();
        public string WorldKey=>scope;
        public bool IsPlacing=>edit.Aiming;
        internal bool GermanInterface=>modePicker?.German==true;
        MarkerOptions Defaults=>CurrentDefaults();
        WindowManager manager;
        CustomWindowDefinition definition;
        IModLogger log;
        string directory,selectedId,status="Spielstand wird vorbereitet.",scope,repositoryError;
        string keyPath,keyMap,cachedKey;
        PlacementWorkLoop workLoop;
        string signWarning;
        int unresolved;
        bool stopped;
        public void Initialize(IModLogger logger,string path) {
            log=logger;
            directory=Path.Combine(path,"Markers");
            scene=new MarkerScene(geometry,transform);
            modePicker=new ModePicker(PickerAccess,OpenFromPicker);
            units=new SignUnitPreference(Path.Combine(Game.UserPaths.RailroaderDirectory,"ModSettings","enusde-translation.json"));
            units.Refresh();
            ApplySettings(Entry.SettingsSnapshot());
            workLoop=new PlacementWorkLoop(PreparePlacement,ReadPlacementInput,UpdateAim,RefreshScene,RefreshSignDisplay,ReportPlacementFailure);
        }
        ModePickerAccess PickerAccess() {
            bool active=!stopped&&!StateManager.IsUnloading;
            return new ModePickerAccess(StateManager.IsHost,
                active&&Entry.WorkspaceAvailable,
                active&&options.Visible&&repository!=null&&Entry.Shared?.WorldKey==scope,
                active&&Entry.WorkspaceEditable);
        }
        void OpenFromPicker(WorkspaceMode mode) {
            if(mode==WorkspaceMode.Build) {
                Entry.CloseWorkspaceMode();
                buildTrack=null;
                Command("open");
                return;
            }
            if(edit.Aiming){edit.Cancel();scene.HidePreview();}
            if(CustomWindowHandle.Create(WindowId)?.IsOpen()==true)CloseTool();
            Entry.OpenWorkspaceMode(mode==WorkspaceMode.Editor);
        }
        public void ApplySettings(DiagnosticSettings settings){options=settings.Markers;scene?.ApplyGlow(options.Glow);if(!options.Visible){edit.Cancel();scene?.SetVisible(false);}}
        string CurrentScope() {
            var state=StateManager.Shared;var save=state?.SaveManager;
            if(state==null||save==null||string.IsNullOrWhiteSpace(save.RailroadPath)||string.IsNullOrWhiteSpace(state.CurrentMapIdentifier))return null;
            if(keyPath!=save.RailroadPath||keyMap!=state.CurrentMapIdentifier){keyPath=save.RailroadPath;keyMap=state.CurrentMapIdentifier;cachedKey=MarkerRepository.Key(keyPath,keyMap);}
            return cachedKey;
        }
        long sharedVersion=-1;
        void EnsureRepository() {
            var shared=Entry.Shared;if(shared==null)return;if(!shared.Ready&&StateManager.IsHost)shared.ImportIfNeeded();
            if(!shared.Ready){status=shared.Error??"Anlagenbestand wird geladen / vom Host übertragen.";return;}
            string key=shared.WorldKey;
            if(scope!=null&&scope!=key){buildTrack=null;DisposeCrosses();edit.Cancel();scene.Dispose();scene=new MarkerScene(geometry,transform);repository=null;records=new MarkerRecord[0];repositoryError=null;selectedId=null;sharedVersion=-1;}
            scope=key;
            if(StateManager.IsHost&&repository==null)repository=new MarkerRepository(directory,key,StateManager.Shared.CurrentMapIdentifier,shared.Objects,shared.PutObjects);
            if(sharedVersion!=shared.InstallationVersion){records=shared.Objects().Markers.Select(m=>m.Clone()).ToArray();sharedVersion=shared.InstallationVersion;status=StateManager.IsHost?"Gemeinsamer Anlagenbestand bereit. Objekte: "+records.Length:"Anlagenansicht vom Host. Objekte: "+records.Length;}
        }
        bool CanEdit(out string reason) {
            reason=null;
            if(stopped||StateManager.IsUnloading){reason="Spielwelt nicht bereit.";return false;}
            if(!StateManager.IsHost){reason="Bau und Konfiguration nur als Host. Mitspieler erhalten die gemeinsame Anlagenansicht.";return false;}
            EnsureRepository();
            if(repository==null||Entry.Shared==null||Entry.Shared.WorldKey!=scope){reason=repositoryError??"Kein lokaler Spielstandpfad verfügbar. Spielstand speichern und erneut versuchen.";return false;}
            if(!options.Visible){reason="Objektanzeige unter Spieloptionen > Mods zuerst einschalten.";return false;}
            return true;
        }
        void Update() {
            if(stopped||StateManager.IsUnloading)return;
            workLoop?.Tick(Time.realtimeSinceStartup,edit.Aiming);
            try{RefreshCrosses();}catch(Exception e){crossError=e.Message;log?.Warning("Ungültigkeitskreuz: "+e.Message);nextCross=Time.realtimeSinceStartup+30;}
        }
        void PreparePlacement() {
            try{EnsureRepository();EnsureWindow();}
            finally{modePicker.Update(options.ShowBuildButton);}
        }
        void ReadPlacementInput() {
            if(!edit.Aiming||Time.realtimeSinceStartup<placeAfter)return;
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            if(mouse!=null&&mouse.rightButton.wasPressedThisFrame||keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){edit.Cancel();scene.HidePreview();status="Platzierung abgebrochen.";}
            else if(mouse!=null&&mouse.leftButton.wasPressedThisFrame&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()))Act(Commit);
        }
        void RefreshSignDisplay() {
            units.Refresh();existingSigns.Refresh(options.AdaptExistingSpeedSigns,units.Mode);
            if(edit.Candidate!=null)RenderPreview();
            if(existingSigns.Error!=null)throw new InvalidOperationException(existingSigns.Error);
        }
        void ReportPlacementFailure(PlacementWork work,string error) {
            if(work==PlacementWork.Signs){if(signWarning==error)return;signWarning=error;}
            else if(error!=null)status="Signalbau: "+error;
            if(error!=null)log?.Warning((work==PlacementWork.Signs?"Optionale Schildanzeige: ":"Signalbau "+work+": ")+error);
        }
        void UpdateAim() {
            MarkerAnchor anchor;string why;
            if(!geometry.Pointer(previewOptions,out anchor,out why)){edit.Offer(null);scene.HidePreview();status=why;return;}
            AlignPlacement(anchor,previewOptions);
            edit.Offer(anchor);RenderPreview();status="Vorschau folgt der Maus. Linksklick setzt; Rechtsklick/Escape bricht ab.";
        }
        void RenderPreview()=>scene.ShowPreview(edit.Candidate,edit.Kind,edit.SpeedMph,units.Mode,edit.FacingReverse);
        void PrepareSpeedInput(){speedInKmh=units.Mode!="original";speedInput=SignUnits.Number(edit.SpeedMph*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;}
        public void FormatSignTooltip(Game.Mow.PlayerSign sign,ref TooltipInfo tooltip){if(options.AdaptExistingSpeedSigns&&units.Mode!="original"&&sign.Kind==Game.Mow.PlayerSign.KindSpeedRestriction&&sign.Mph.HasValue&&SignUnits.ValidSpeed(sign.Mph.Value))tooltip.Title="Geschwindigkeitsbeschränkung: "+SignUnits.Description(sign.Mph.Value,units.Mode);}
        public void FromTrack(Track.Location location,string kind) {
            if(kind=="build") {
                if(edit.Aiming){edit.Cancel();scene.HidePreview();RefreshScene();}
                buildTrack=location;
                page=Page.Build;
                status="Gleisposition vorgemerkt. Bauobjekt auswählen.";
                ShowTool();
                return;
            }
            if(kind=="edit"){page=Page.Objects;ShowTool();return;}
            if(kind=="interlocking"){Entry.OpenInterlocking();return;}
            if(kind=="identifiers"){Entry.InspectSignalTrack(location);return;}
            if(kind=="assign-point"){Entry.AssignSignalPoint(location);return;}
            Act(()=>{
                string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
                if(kind!=MarkerRecord.Clearance&&kind!=MarkerRecord.Speed&&kind!=MarkerRecord.Main&&kind!=MarkerRecord.Distant&&kind!=MarkerRecord.Shunt&&kind!=MarkerRecord.CircuitA&&kind!=MarkerRecord.CircuitB)throw new InvalidOperationException("Unbekannter Objekttyp.");
                if(records.Length>=MarkerRepository.Maximum)throw new InvalidOperationException("P0-Grenze von 128 Objekten erreicht.");
                if(!location.IsValid)throw new InvalidOperationException("Angeklicktes Gleis ist nicht mehr gültig.");
                var at=location.WithEnd(Track.TrackSegment.End.A);
                var defaults=Defaults;var anchor=new MarkerAnchor{Segment=at.segment.id,Meters=at.distance,Lateral=defaults.Lateral,Height=defaults.Height,Scale=defaults.Scale};
                Vector3 p;Quaternion q;if(!geometry.Resolve(anchor,out p,out q,out why))throw new InvalidOperationException(why);
                edit.Start(defaults,kind);AlignPlacement(anchor,defaults);edit.Offer(anchor);edit.Freeze();PrepareSpeedInput();RenderPreview();
                status="Angeklickte Gleisposition übernommen. Vorschau prüfen und bestätigen.";
            });
            RebuildTool();Command("open");
        }
        string lastGlowStatus;
        void RefreshScene() {
            var current=new HashSet<string>(records.Select(r=>r.Id));foreach(string id in shown.Where(id=>!current.Contains(id)).ToArray()){scene.Remove(id);shown.Remove(id);}foreach(string id in current)shown.Add(id);
            if(!options.Visible){scene.SetVisible(false);return;}
            var glow=options.Glow;var camera=glow.Enabled&&glow.Halo?Camera.main:null;scene.GlowView(camera==null?(Vector3?)null:camera.transform.position);
            unresolved=0;foreach(var row in records){if(row.Id==edit.EditingId){scene.Hide(row.Id);continue;}string why;if(!scene.Show(row,out why,units.Mode))unresolved++;}
            if(scene.GlowStatus!=null&&scene.GlowStatus!=lastGlowStatus){lastGlowStatus=scene.GlowStatus;log?.Info(lastGlowStatus);Entry.RecordSignalEvent("signal_glow_renderer",new{renderer=lastGlowStatus,scope="local_visual_only",weather_compatibility_verified=false});}
        }
        void RefreshRecords() {
            records=repository.Records.ToArray();if(!records.Any(x=>x.Id==selectedId))selectedId=records.FirstOrDefault()?.Id;
        }
        MarkerRecord Selected=>records.FirstOrDefault(x=>x.Id==selectedId);
        public string ObjectCaption(string id) {
            var row=records.FirstOrDefault(x=>x.Id==id);
            return row==null?"Signaltechnik":row.IsCircuitBox?CircuitIdentity.Caption(row):row.Name;
        }
        public void OpenObject(string id,int direction=0) {
            if(!records.Any(r=>r.Id==id))return;
            if(!StateManager.IsHost){selectedId=id;page=Page.ReadOnly;ShowTool();return;}
            edit.Cancel();scene.HidePreview();selectedId=id;selectedEditFor=null;LoadDraft();directionTab=direction==1?1:0;page=Page.Configure;ShowTool();
        }
        void LoadDraft() {
            var row=Selected;if(row==null)return;
            selectedEditFor=row.Id;codeInput=row.Name;displayNameInput=row.DisplayName??"";labelScaleInput=SignUnits.Number(row.LabelScale??1);facingInput=row.FacingReverse;linkedInput=row.LinkedSignal;mastDraft=row.Kind==MarkerRecord.Main?MastConfiguration.Effective(row):null;
            useHostInput=row.UseHostPlacement;lateralInput=SignUnits.Number(row.Anchor.Lateral);heightInput=SignUnits.Number(row.Anchor.Height);scaleInput=SignUnits.Number(row.Anchor.Scale);
            draftSpeed=row.SpeedMph;speedInKmh=units.Mode!="original";speedInput=SignUnits.Number(draftSpeed*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;directionTab=0;
        }
        void ShowTool() {
            EnsureWindow();
            if(manager==null||StateManager.IsUnloading)return;
            bool cached=manager.GetCustomWindow(WindowId)!=null;
            CustomWindowHandle.Create(WindowId)?.Show();
            if(cached)RebuildTool();
        }
        public void ApplyHostPlacement(){string why;if(!CanEdit(out why))throw new InvalidOperationException(why);repository.ApplyHostPlacement(Defaults,Entry.MastCanModify);RefreshRecords();selectedEditFor=null;RefreshScene();status="Host-Vorgaben auf bestehende Masten mit Host-Bindung angewendet.";RebuildTool();}
        void StartPreview()=>StartPreview(MarkerRecord.Clearance);
        void StartPreview(string kind) {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            if(records.Length>=MarkerRepository.Maximum)throw new InvalidOperationException("P0-Grenze von 128 Zeichen erreicht.");
            previewOptions=Defaults;
            edit.Start(previewOptions,kind);placeAfter=Time.realtimeSinceStartup+.25;PrepareSpeedInput();UpdateAim();CloseTool();
        }
        void Commit() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            var original=records.FirstOrDefault(r=>r.Id==edit.EditingId);if(original!=null&&!Entry.MastCanModify(original))throw new InvalidOperationException("Mast ist inzwischen durch eine Fahrstraße verschlossen. Vorschau abbrechen.");
            if(edit.Kind==MarkerRecord.Speed&&speedDirty){edit.SpeedMph=SignUnits.Input(speedInput,speedInKmh);speedDirty=false;}
            if(edit.Aiming)UpdateAim();
            var row=edit.Commit(repository,a=>{Vector3 p;Quaternion q;string error;return geometry.Resolve(a,out p,out q,out error);});
            scene.HidePreview();RefreshRecords();selectedId=row.Id;string reason;
            bool drawn=scene.Show(row,out reason,units.Mode);status=row.Name+" gespeichert."+(drawn?"":" Darstellung: "+reason);
            Entry.RecordMarkerEvent("placed_or_moved",row);selectedEditFor=null;page=Page.Configure;LoadDraft();RebuildTool();
        }
        void FromVehicle() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            if(records.Length>=MarkerRepository.Maximum&&edit.EditingId==null)throw new InvalidOperationException("P0-Grenze von 128 Zeichen erreicht.");
            var car=Game.Scripting.Modules.Railroader.World.getSelectedCar()??Game.Scripting.Modules.Railroader.World.getInspectorCar();
            if(car==null)throw new InvalidOperationException("Zuerst eine Lok oder einen Wagen auswählen.");
            var location=car.LocationF;if(!location.IsValid)throw new InvalidOperationException("Fahrzeug hat keine gültige Gleisposition.");
            location=location.WithEnd(Track.TrackSegment.End.A);
            var defaults=Defaults;var anchor=new MarkerAnchor{Segment=location.segment.id,Meters=location.distance,Lateral=defaults.Lateral,Height=defaults.Height,Scale=defaults.Scale};
            Vector3 p;Quaternion q;if(!geometry.Resolve(anchor,out p,out q,out why))throw new InvalidOperationException(why);
            string kind=edit.Kind,existing=edit.EditingId;double speed=edit.SpeedMph;bool reverse=edit.FacingReverse,useHost=edit.UseHostPlacement;
            if(existing!=null)edit.Edit(new MarkerRecord{Id=existing,Kind=kind,SpeedMph=speed,FacingReverse=reverse,UseHostPlacement=useHost,Anchor=anchor});
            else{edit.Start(defaults,kind);AlignPlacement(anchor,defaults);edit.Offer(anchor);edit.Freeze();edit.SpeedMph=speed;}
            page=Page.Placement;PrepareSpeedInput();RenderPreview();status="Fahrzeugfront übernommen. Noch nicht gespeichert.";RebuildTool();
        }
        void EditSelected() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            var row=Selected??throw new InvalidOperationException("Kein Grenzzeichen ausgewählt.");
            if(!Entry.MastCanModify(row))throw new InvalidOperationException("Mast ist in einer aktiven Fahrstraße verschlossen.");
            edit.Edit(row);PrepareSpeedInput();previewOptions=new MarkerOptions{Lateral=row.Anchor.Lateral,Height=row.Anchor.Height,Scale=row.Anchor.Scale};scene.Hide(row.Id);RenderPreview();page=Page.Placement;status=row.Name+" verschieben; zum Übernehmen bestätigen.";RebuildTool();
        }
        void RemoveSelected() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            var row=Selected??throw new InvalidOperationException("Kein Grenzzeichen ausgewählt.");
            if(!Entry.MastCanModify(row))throw new InvalidOperationException("Mast ist in einer aktiven Fahrstraße verschlossen.");
            repository.Remove(row.Id);edit.Cancel();scene.HidePreview();scene.Remove(row.Id);RefreshRecords();page=Page.Objects;selectedEditFor=null;status=row.Name+" entfernt.";Entry.RecordMarkerEvent("removed",row);RebuildTool();
        }
        void Adjust(double along,double lateral) {
            if(edit.Candidate==null)throw new InvalidOperationException("Zuerst eine Vorschau wählen.");
            edit.Freeze();var next=edit.Candidate.Clone();next.Meters+=along;next.Lateral+=lateral;next.Validate();Vector3 p;Quaternion q;string why;
            if(!geometry.Resolve(next,out p,out q,out why))throw new InvalidOperationException(why);
            edit.Edit(new MarkerRecord{Id=edit.EditingId,Anchor=next,Kind=edit.Kind,SpeedMph=edit.SpeedMph,FacingReverse=edit.FacingReverse,UseHostPlacement=edit.UseHostPlacement&&lateral==0});RenderPreview();status="Vorschau geändert; zum Übernehmen bestätigen.";
        }
        public string Command(string command) {
            try {
                switch((command??"open").Trim().ToLowerInvariant()) {
                    case "open":case "öffnen":case "oeffnen":EnsureRepository();if(edit.Aiming){if(edit.Candidate!=null)edit.Freeze();else edit.Cancel();}page=edit.Candidate==null?Page.Build:Page.Placement;ShowTool();return status;
                    case "vorschau":StartPreview();break;
                    case "fahrzeug":FromVehicle();break;
                    case "setzen":Commit();break;
                    case "abbrechen":edit.Cancel();scene.HidePreview();RefreshScene();status="Vorschau abgebrochen.";break;
                    case "status":break;
                    default:return "Verwendung: /enus_grenzzeichen [vorschau|fahrzeug|setzen|abbrechen|status]";
                }
            }catch(Exception e){status=e.Message;}
            return status;
        }
        void Act(Action operation){try{operation();}catch(Exception e){status=e.Message;}}
        void EnsureWindow() {
            if(manager==null)manager=WindowManager.Shared;if(manager==null)return;
            if(definition!=null&&ReferenceEquals(manager.GetDefinition(WindowId),definition))return;
            definition=new CustomWindowDefinition{Identifier=WindowId,Title="Signaltechnik – Baumenü",BuilderAction=Build,
                Config=WindowConfig.Resizable(new Vector2Int(600,640),new Vector2Int(420,350),new Vector2Int(1100,1000),Window.Position.CenterRight)};
            manager.RegisterCustomWindow(definition);
        }
        void CloseTool(){if(manager!=null&&!StateManager.IsUnloading)manager.CloseWindow(WindowId);}
        void RebuildTool(){if(manager!=null&&!StateManager.IsUnloading)CustomWindowHandle.Create(WindowId)?.SetNeedsRebuild();}
        string AnchorText(MarkerAnchor anchor)=>anchor==null?"Keine gültige Vorschau.":anchor.Segment+" · "+anchor.Meters.ToString("0.00")+" m ab Ende A · seitlich "+anchor.Lateral.ToString("0.00")+" m";
        readonly UIState<bool> technicalExpanded=new UIState<bool>(false);
        void Navigate(Page next) {
            if(page==Page.Placement&&next!=Page.Placement){edit.Cancel();scene.HidePreview();RefreshScene();}
            page=next;RebuildTool();
        }
        void Build(UIPanelBuilder b) {
            b.VScrollView(c=>{
                c.HStack(row=>{
                    if(StateManager.IsHost){row.AddButtonSelectable("Bauen",page==Page.Build,()=>Navigate(Page.Build));row.AddButtonSelectable("Objekte",page==Page.Objects||page==Page.Configure,()=>Navigate(Page.Objects));}
                    row.AddButton("Material",OpenMaterials);
                    row.AddButton("Stellwerk",Entry.OpenInterlocking);
                });
                c.AddLabel(()=>status,UIPanelBuilder.Frequency.Periodic);
                c.AddLabel(()=>signWarning==null?"":"Schildanzeige eingeschränkt: "+signWarning,UIPanelBuilder.Frequency.Periodic);
                switch(page) {
                    case Page.Materials:BuildMaterials(c);break;
                    case Page.ReadOnly:BuildReadOnly(c);break;
                    case Page.Stock:BuildStock(c);break;
                    case Page.Objects:BuildObjects(c);break;
                    case Page.Configure:BuildConfiguration(c);break;
                    case Page.Placement:BuildPlacement(c);break;
                    default:BuildHome(c);break;
                }
                c.AddButtonCompact("Schließen",CloseTool);
            });
        }
        void StartHomePlacement(string kind) {
            if(buildTrack.HasValue) {
                var at=buildTrack.Value;
                buildTrack=null;
                FromTrack(at,kind);
            } else StartPreview(kind);
        }
        void BuildHome(UIPanelBuilder c) {
            c.AddTitle("Bauen",buildTrack.HasValue&&buildTrack.Value.IsValid?"Vorgemerkte Gleisposition · "+buildTrack.Value.segment.id:
                "Objekt wählen → Maus aufs Gleis → Linksklick");
            if(buildTrack.HasValue)c.AddButton("Position neu mit der Maus wählen",()=>{buildTrack=null;RebuildTool();});
            c.AddButtonLarge("Signalmast",()=>Act(()=>StartHomePlacement(MarkerRecord.Main)));
            c.AddButtonLarge("Vorsignal (einzeln)",()=>Act(()=>StartHomePlacement(MarkerRecord.Distant)));
            c.AddButtonLarge("Rangiersignal",()=>Act(()=>StartHomePlacement(MarkerRecord.Shunt)));
            c.AddButtonLarge("Geschwindigkeitsschild",()=>Act(()=>StartHomePlacement(MarkerRecord.Speed)));
            c.AddButtonLarge("Grenzzeichen",()=>Act(()=>StartHomePlacement(MarkerRecord.Clearance)));
            c.AddButtonLarge("Gleisfreimeldekasten A",()=>Act(()=>StartHomePlacement(MarkerRecord.CircuitA)));
            c.AddButtonLarge("Gleisfreimeldekasten B",()=>Act(()=>StartHomePlacement(MarkerRecord.CircuitB)));
            c.AddButtonLarge("Material und Bauaufträge",OpenMaterials);
            c.AddButtonLarge("Bestandssignale ungültig kennzeichnen",()=>Act(OpenStockSignals));
            c.AddLabel("Einen platzierten Mast anklicken, um seine Richtungen und Signalbegriffe einzurichten. Rangiersignale können auch einzeln stehen.");
            c.AddLabel("Rechtsklick oder Escape bricht die Platzierung ab.");
            var defaults=Defaults;
            c.AddLabel("Host-Vorgaben: "+SignUnits.Number(defaults.Lateral)+" m seitlich · "+SignUnits.Number(defaults.Height)+" m Höhe.");
            c.AddLabel("Vorgaben ändern: Spieloptionen → Mods → EnusDE Signaltechnik.");
        }
        void BuildObjects(UIPanelBuilder c) {
            c.AddTitle("Objekte","Oder ein Objekt direkt in der Spielwelt anklicken");
            if(records.Length==0){c.AddLabel("Noch keine eigenen Objekte platziert.");return;}
            var choices=records.OrderBy(r=>r.Name,StringComparer.OrdinalIgnoreCase).ToArray();
            int index=Array.FindIndex(choices,r=>r.Id==selectedId);if(index<0){index=0;selectedId=choices[0].Id;}
            c.AddField("Auswahl",c.AddDropdown(choices.Select(r=>(r.IsCircuitBox?CircuitIdentity.Caption(r):r.Name)+" · "+MarkerRecord.KindLabel(r.Kind)).ToList(),index,i=>{selectedId=choices[i].Id;selectedEditFor=null;}));
            c.AddButtonLarge("Ausgewähltes Objekt konfigurieren",()=>OpenObject(selectedId));
            c.AddLabel(records.Length+" eigene Objekte.");
        }
        void BuildConfiguration(UIPanelBuilder c) {
            var row=Selected;if(row==null){c.AddLabel("Objekt nicht mehr vorhanden.");return;}
            if(selectedEditFor!=row.Id)LoadDraft();
            c.AddTitle(row.IsCircuitBox?CircuitIdentity.Caption(row):row.Name,MarkerRecord.KindLabel(row.Kind));
            if(row.Kind==MarkerRecord.Main) {
                c.HStack(t=>{
                    t.AddButtonSelectable("Richtung A",directionTab==0,()=>{directionTab=0;c.SetNeedsRebuild();});
                    t.AddButtonSelectable("Richtung B",directionTab==1,()=>{directionTab=1;c.SetNeedsRebuild();});
                    t.AddButtonSelectable("Position",directionTab==2,()=>{directionTab=2;c.SetNeedsRebuild();});
                });
                if(directionTab==2)BuildPositionSettings(c);
                else {
                    c.AddField("Mastname",c.AddInputField(codeInput,v=>codeInput=v,"Name des Mastes",48));
                    BuildFace(c,directionTab==0?mastDraft.Front:mastDraft.Back);
                    BuildLabelScale(c);
                    c.AddButtonLarge("Mastkonfiguration speichern",()=>Act(SaveDetails));
                }
            }else {
                c.AddField(row.IsSignal?"Signalkennung":"Name",c.AddInputField(codeInput,v=>codeInput=v,"Name",48));
                if(row.IsSignal) {
                    c.AddField("Anzeigename",c.AddInputField(displayNameInput,v=>displayNameInput=v,"Leer: Signalkennung",48));
                    BuildLabelScale(c);
                }
                if(row.Kind==MarkerRecord.Shunt||row.Kind==MarkerRecord.Distant) {
                    c.AddFieldToggle("Gegenrichtung",()=>facingInput,v=>facingInput=v,true);
                    if(row.Kind==MarkerRecord.Shunt)c.AddLabel("Rangiersignal: Sh 0 / Sh 1.");
                    else BuildTarget(c,linkedInput,v=>linkedInput=v,null);
                }
                if(row.Kind==MarkerRecord.Speed)BuildSpeed(c);
                c.AddButton("Objekt speichern",()=>Act(SaveDetails));
                c.AddExpandable("Position und Abstände",positionExpanded,BuildPositionSettings,ExpandableStyle.SectionHeader);
            }
            if(row.IsSignal&&!row.Planned&&(row.Kind!=MarkerRecord.Main||directionTab<2)) {
                c.AddButton("Signal zuordnen",()=>Act(()=>{
                    var saved=Selected;
                    if(saved==null||saved.Id!=row.Id)throw new InvalidOperationException("Signal erneut auswählen.");
                    string id=saved.Id;
                    if(saved.Kind==MarkerRecord.Main) {
                        var mast=MastConfiguration.Effective(saved);
                        var face=directionTab==0?mast.Front:mast.Back;
                        if(!face.Enabled)throw new InvalidOperationException("Richtung zuerst aktivieren und speichern.");
                        id=face.Id;
                    }
                    Entry.AssignPanelSignal(id);
                    CloseTool();
                }));
            }
            if(row.IsSignal)BuildCircuitAttachment(c,row);
            if(CircuitIdentity.Enabled(row))
                c.AddButton("Als Grenze für Gleisfreimeldung vormerken",()=>Act(()=>{
                    if(Selected?.Id!=row.Id)throw new InvalidOperationException("Objekt erneut auswählen.");
                    Entry.RememberCircuitBoundary(row.Id);
                    status=CircuitIdentity.Caption(row)+" als Abschnittsgrenze vorgemerkt.";
                    RebuildTool();
                }));
            c.AddButton("Auf dem Gleis verschieben",()=>Act(EditSelected));
            c.AddButton("Dieses Objekt entfernen",()=>Act(RemoveSelected));
            c.AddExpandable("Technische Daten",technicalExpanded,t=>{
                t.AddField("Objekt-ID",row.Id);t.AddField("Gleisanker",AnchorText(row.Anchor));
                if(row.Kind==MarkerRecord.Main){t.AddField("ID Richtung A",mastDraft.Front.Id);t.AddField("ID Richtung B",mastDraft.Back.Id);}
            },ExpandableStyle.SectionHeader);
        }
        void BuildCircuitAttachment(UIPanelBuilder c,MarkerRecord row) {
            int variant=row.CircuitVariant;
            string offset=SignUnits.Number(row.CircuitOffset);
            c.AddSection("Gleisfreimeldung am Signal");
            c.AddField("Kasten",c.AddDropdown(new List<string>{"Kein Kasten","Variante A","Variante B"},variant,i=>variant=i));
            if(row.CircuitNumber>0)c.AddLabel("Entitätsnummer "+row.CircuitNumber.ToString("D3"));
            c.AddField("Kasten längs versetzen (m)",c.AddInputField(offset,v=>offset=v,"−2 bis 2",8));
            c.AddButton("GFM-Kasten übernehmen",()=>Act(()=>{
                RequireSelectedChange();
                if(Selected.Id!=row.Id)throw new InvalidOperationException("Signal erneut auswählen.");
                repository.ConfigureCircuit(row.Id,variant,variant==0?0:Dimension(offset,-2,2));
                Saved(variant==0?"Gleisfreimeldekasten entfernt.":"Gleisfreimeldekasten verknüpft.");
            }));
        }
        readonly UIState<bool> positionExpanded=new UIState<bool>(false);
        void BuildFace(UIPanelBuilder c,SignalFace face) {
            c.AddFieldToggle("Signalkopf in dieser Richtung",()=>face.Enabled,v=>{face.Enabled=v;c.SetNeedsRebuild();},true);
            if(!face.Enabled){c.AddLabel("In dieser Richtung ist kein Signal eingerichtet.");return;}
            c.AddField("Signalkennung",c.AddInputField(face.Code,v=>face.Code=v,"z. B. A1",48));
            c.AddField("Anzeigename",c.AddInputField(face.DisplayName??"",v=>face.DisplayName=v,"Leer: Mastname",48));
            c.AddLabel("Hp 0 · Halt ist immer verfügbar.");
            c.AddFieldToggle("Hp 1 · Fahrt",()=>face.Hp1,v=>face.Hp1=v,true);
            c.AddFieldToggle("Hp 2 · Langsamfahrt",()=>face.Hp2,v=>face.Hp2=v,true);
            c.AddFieldToggle("Sh 1 · Rangierfahrt am Mast",()=>face.Sh1,v=>face.Sh1=v,true);
            c.AddFieldToggle("Vorsignalkopf am Mast",()=>face.DistantHead,v=>{face.DistantHead=v;c.SetNeedsRebuild();},true);
            if(face.DistantHead)BuildTarget(c,face.DistantTarget,v=>face.DistantTarget=v,face.Id);
        }
        void BuildLabelScale(UIPanelBuilder c) {
            c.AddField("Tafelgröße (0,6–2)",c.AddInputField(labelScaleInput,v=>labelScaleInput=v,"1",6));
        }

        void BuildTarget(UIPanelBuilder c,string selected,Action<string> set,string ownId) {
            var choices=SignalCatalogue.Expand(records).Where(r=>r.Kind==MarkerRecord.Main&&r.Id!=ownId&&r.PhysicalId!=selectedId).OrderBy(r=>r.Name).ToArray();
            var labels=new List<string>{"Kein Hauptsignal zugeordnet"};labels.AddRange(choices.Select(r=>r.Name));
            c.AddField("Angekündigtes Hauptsignal",c.AddDropdown(labels,Array.FindIndex(choices,r=>r.Id==selected)+1,i=>set(i==0?null:choices[i-1].Id)));
        }
        void BuildSpeed(UIPanelBuilder c) {
            c.AddField("Eingabeeinheit",c.AddDropdown(new List<string>{"mph","km/h"},speedInKmh?1:0,i=>Act(()=>{
                SyncSpeedInput();speedInKmh=i==1;double value=page==Page.Placement?edit.SpeedMph:draftSpeed;
                speedInput=SignUnits.Number(value*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;c.SetNeedsRebuild();
            })));
            c.AddField("Geschwindigkeit",c.AddInputField(speedInput,v=>{speedInput=v;speedDirty=true;},"Wert",null));
            c.AddField("Beschriftung",()=>SignUnits.Caption(page==Page.Placement?edit.SpeedMph:draftSpeed,units.Mode),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel("Die Schildanzeige folgt der Übersetzungseinstellung und wird auf ganze Zahlen gerundet.");
        }
        void SyncSpeedInput() {
            if(!speedDirty)return;double mph=SignUnits.Input(speedInput,speedInKmh);
            if(page==Page.Placement)edit.SpeedMph=mph;else draftSpeed=mph;speedDirty=false;
        }
        void BuildPositionSettings(UIPanelBuilder c) {
            if(Selected!=null)c.AddLabel("Gespeichert: "+SignUnits.Number(Selected.Anchor.Lateral)+" m seitlich · "+SignUnits.Number(Selected.Anchor.Height)+" m Höhe.");
            c.AddFieldToggle("Host-Vorgaben benutzen",()=>useHostInput,v=>{useHostInput=v;c.SetNeedsRebuild();},true);
            if(useHostInput){var value=Defaults;c.AddLabel("Seitlich "+SignUnits.Number(value.Lateral)+" m · Höhe "+SignUnits.Number(value.Height)+" m · Größe "+SignUnits.Number(value.Scale)+"×");}
            else {
                c.AddField("Seitlicher Abstand (m)",c.AddInputField(lateralInput,v=>lateralInput=v,"-5 bis 5",null));
                c.AddField("Höhe zum Gleis (m)",c.AddInputField(heightInput,v=>heightInput=v,"-1 bis 2",null));
                c.AddField("Größe",c.AddInputField(scaleInput,v=>scaleInput=v,"0,5 bis 3",null));
            }
            c.AddButton("Abstände übernehmen",()=>Act(SavePosition));
        }
        static double Dimension(string text,double low,double high) {
            double value;if(!double.TryParse((text??"").Trim().Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value)||double.IsNaN(value)||double.IsInfinity(value)||value<low||value>high)throw new InvalidOperationException("Abstand oder Größe liegt außerhalb des angezeigten Bereichs.");return value;
        }
        void RequireSelectedChange() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            if(Selected==null)throw new InvalidOperationException("Kein Objekt ausgewählt.");
            if(!Entry.MastCanModify(Selected))throw new InvalidOperationException("Der Mast ist verschlossen. Fahrstraße zuerst sicher auflösen.");
        }
        void Saved(string text) {
            int tab=directionTab;RefreshRecords();selectedEditFor=null;LoadDraft();directionTab=tab;RefreshScene();status=text;Entry.RecordMarkerEvent("configured",Selected);RebuildTool();
        }
        void SaveDetails() {
            RequireSelectedChange();var row=Selected;
            double? labelScale=row.IsSignal?Dimension(labelScaleInput,.6,2):(double?)null;
            if(row.Kind==MarkerRecord.Main) {
                var mast=mastDraft.Clone();
                mast.Front.DisplayName=SignalLabel.Normalize(mast.Front.DisplayName);
                mast.Back.DisplayName=SignalLabel.Normalize(mast.Back.DisplayName);
                repository.ConfigureMast(row.Id,mast,codeInput,labelScale);
            } else {
                if(row.Kind==MarkerRecord.Speed)SyncSpeedInput();
                repository.ConfigureDetails(row.Id,codeInput,row.Kind==MarkerRecord.Speed?(double?)draftSpeed:null,facingInput,linkedInput,displayNameInput,labelScale);
            }
            Saved("Konfiguration gespeichert.");
        }
        void SavePosition() {
            RequireSelectedChange();var defaults=Defaults;
            repository.ConfigurePlacement(selectedId,useHostInput?defaults.Lateral:Dimension(lateralInput,-5,5),useHostInput?defaults.Height:Dimension(heightInput,-1,2),useHostInput?defaults.Scale:Dimension(scaleInput,.5,3),useHostInput);
            Saved("Abstände gespeichert.");
        }
        void BuildPlacement(UIPanelBuilder c) {
            c.AddTitle("Positionieren",MarkerRecord.KindLabel(edit.Kind));
            c.AddLabel("Blau ist die Vorschau. Bestätigen speichert, Abbrechen verwirft sie.");
            if(edit.Kind==MarkerRecord.Speed)BuildSpeed(c);
            c.HStack(t=>{t.AddButton("Bestätigen",()=>Act(Commit));t.AddButton("Abbrechen",()=>{Command("abbrechen");Navigate(Page.Build);});});
            c.AddSection("Fein verschieben");
            c.HStack(t=>{t.AddButtonCompact("−0,25 m am Gleis",()=>Act(()=>Adjust(-.25,0)));t.AddButtonCompact("+0,25 m am Gleis",()=>Act(()=>Adjust(.25,0)));});
            c.HStack(t=>{t.AddButtonCompact("−0,10 m seitlich",()=>Act(()=>Adjust(0,-.1)));t.AddButtonCompact("+0,10 m seitlich",()=>Act(()=>Adjust(0,.1)));});
            c.AddButton("Mit der Maus weiterbewegen",()=>Act(()=>{if(edit.Candidate==null)throw new InvalidOperationException("Keine gültige Vorschau.");SyncSpeedInput();previewOptions=new MarkerOptions{Lateral=edit.Candidate.Lateral,Height=edit.Candidate.Height,Scale=edit.Candidate.Scale};edit.Aim();placeAfter=Time.realtimeSinceStartup+.25;CloseTool();}));
            c.AddButton("An ausgewählte Fahrzeugfront",()=>Act(FromVehicle));
            c.AddField("Gleisposition",()=>AnchorText(edit.Candidate),UIPanelBuilder.Frequency.Periodic);
        }
        public void Shutdown() {
            Cleanup(DisposeCrosses);
            if(stopped)return;stopped=true;edit.Cancel();Cleanup(()=>modePicker?.Dispose());Cleanup(()=>existingSigns.Dispose());Cleanup(()=>scene?.Dispose());records=new MarkerRecord[0];repository=null;Cleanup(RemoveWindow);manager=null;definition=null;
        }
        void Cleanup(Action action){try{action();}catch(Exception e){log?.Warning("Baumenü-Bereinigung: "+e.Message);}}
        void RemoveWindow() {
            if(manager!=null&&definition!=null&&ReferenceEquals(manager.GetDefinition(WindowId),definition)) {
                try{manager.CloseWindow(WindowId);}catch{}
                var instance=manager.GetCustomWindow(WindowId);definition.BuilderAction=null;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                (typeof(WindowManager).GetField("_customDefinitions",flags)?.GetValue(manager) as IDictionary)?.Remove(WindowId);
                (typeof(WindowManager).GetField("_customWindows",flags)?.GetValue(manager) as IDictionary)?.Remove(WindowId);
                manager.UnregisterActiveWindow(WindowId);if(instance!=null)UnityEngine.Object.Destroy(instance.gameObject);
            }
        }
        void OnDestroy(){Shutdown();}
    }
}
