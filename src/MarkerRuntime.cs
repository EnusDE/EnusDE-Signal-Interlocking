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
        readonly BuildModeSession buildMode=new BuildModeSession();
        public bool BuildModeActive=>buildMode.Active;
        internal string SelectedBuildSignal=>selectedId;
        bool buildWindowShown;
        MarkerScene scene;
        MarkerRepository repository;
        MarkerRecord[] records=new MarkerRecord[0];
        MarkerOptions options=new MarkerOptions(),previewOptions=new MarkerOptions();
        readonly ExistingSignDisplay existingSigns=new ExistingSignDisplay();
        readonly HashSet<string> shown=new HashSet<string>();
        SignUnitPreference units;
        string speedInput="20";
        bool speedInKmh;
        bool speedDirty,useHostInput,fixedHeightInput,groundSupportInput;
        double draftSpeed;
        string lateralInput,heightInput,scaleInput;
        MastConfiguration mastDraft;
        enum Page { Build,Objects,Configure,Placement,Materials,ReadOnly,Stock,PointBox,PointCircuits,QuickCircuits }
        Page page;
        int directionTab;
        string codeInput="",selectedEditFor,linkedInput,displayNameInput,labelScaleInput;
        bool facingInput;
        double placeAfter;
        ModePicker modePicker;
        Track.Location? buildTrack;
        public MarkerRecord[] Objects=>records.Select(r=>r.Clone()).ToArray();
        public string WorldKey=>scope;
        public bool IsPlacing=>edit.Aiming||edit.Repeating;
        internal bool GermanInterface=>modePicker?.German==true;
        MarkerOptions Defaults=>CurrentDefaults();
        WindowManager manager;
        CustomWindowDefinition definition;
        IModLogger log;
        string directory,selectedId,status=LocalText.Get("Spielstand wird vorbereitet."),scope,repositoryError;
        string keyPath,keyMap,cachedKey;
        PlacementWorkLoop workLoop;
        string signWarning;
        int unresolved;
        bool stopped;
        public void Initialize(IModLogger logger,string path) {
            log=logger;
            directory=Path.Combine(path,"Markers");
            scene=new MarkerScene(geometry,transform);
            modePicker=new ModePicker(PickerAccess,OpenFromPicker,Entry.OpenSettings);
            units=new SignUnitPreference(Path.Combine(RailroaderPaths.Directory(),"ModSettings","enusde-translation.json"));
            units.Refresh();
            ApplySettings(Entry.SettingsSnapshot());
            workLoop=new PlacementWorkLoop(PreparePlacement,ReadPlacementInput,UpdateAim,RefreshScene,RefreshSignDisplay,ReportPlacementFailure);
        }
        ModePickerAccess PickerAccess() {
            bool active=!stopped&&!StateManager.IsUnloading;
            return new ModePickerAccess(StateManager.IsHost,
                active&&Entry.WorkspaceAvailable,
                active&&options.Visible&&repository!=null&&Entry.Shared?.WorldKey==scope,
                active&&Entry.WorkspaceEditable,
                Entry.Shared?.Ready==true&&!Entry.Shared.ConstructionUnlocked);
        }
        void OpenFromPicker(WorkspaceMode mode) {
            if(mode==WorkspaceMode.Build) {
                Entry.CloseWorkspaceMode();
                buildTrack=null;
                Command("open");
                return;
            }
            if(IsPlacing){edit.Cancel();scene.HidePreview();}
            EndBuildMode();
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
            if(!shared.Ready){status=shared.RecoveryNotice??shared.Error??"Anlagenbestand wird geladen / vom Host übertragen.";return;}
            string key=shared.WorldKey;
            if(scope!=null&&scope!=key){EndBuildMode();buildTrack=null;DisposeCrosses();edit.Cancel();scene.Dispose();scene=new MarkerScene(geometry,transform);repository=null;records=new MarkerRecord[0];repositoryError=null;selectedId=null;sharedVersion=-1;}
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
            if(!Entry.Shared.ConstructionUnlocked){reason=LocalText.Select("Unlock signalling in Milestones first. Check the map prerequisites and purchase price.","Signaltechnik zuerst unter Meilensteine freischalten. Kartenvoraussetzungen und Kaufpreis beachten.");return false;}
            if(!options.Visible){reason="Objektanzeige unter Auswahlfenster > Einstellungen zuerst einschalten.";return false;}
            return true;
        }
        void Update() {
            if(stopped||StateManager.IsUnloading)return;
            if(Entry.LampTestOwner!=null&&(!buildMode.Active||Entry.LampTestOwner!=selectedId))Entry.EndLampTest();
            if(buildMode.Active&&buildWindowShown&&CustomWindowHandle.Create(WindowId)?.IsOpen()!=true)EndBuildMode();
            if(quickCircuitActive&&(scope!=quickCircuitWorld||Keyboard.current?.escapeKey.wasPressedThisFrame==true)){quickCircuitActive=false;status=LocalText.Select("Detection tool stopped.","GFM-Werkzeug beendet.");RebuildTool();}
            workLoop?.Tick(Time.realtimeSinceStartup,edit.Aiming);
            try{RefreshCrosses();}catch(Exception e){crossError=e.Message;log?.Warning("Ungültigkeitskreuz: "+e.Message);nextCross=Time.realtimeSinceStartup+30;}
        }
        void PreparePlacement() {
            try{EnsureRepository();EnsureWindow();}
            finally{modePicker.Update();}
        }
        void ReadPlacementInput() {
            if(!IsPlacing)return;
            if(edit.Repeating&&!edit.Aiming&&CustomWindowHandle.Create(WindowId)?.IsOpen()!=true){StopPlacement();return;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){StopPlacement();return;}
            if(!edit.Aiming||Time.realtimeSinceStartup<placeAfter)return;
            if(mouse!=null&&mouse.leftButton.wasPressedThisFrame&&!WorldPointerInput.OverUi())Act(Commit);
        }
        void StopPlacement() {
            setupPlacementAccepted=null;
            bool repeated=edit.Repeating;edit.Cancel();scene.HidePreview();RefreshScene();page=Page.Build;
            status=repeated?LocalText.Select("Signal placement finished. Click a signal to edit it.","Signalplatzierung beendet. Zum Bearbeiten ein Signal anklicken."):LocalText.Get("Platzierung abgebrochen.");RebuildTool();
        }
        void RefreshSignDisplay() {
            units.Refresh();existingSigns.Refresh(options.AdaptExistingSpeedSigns,units.Mode);
            if(edit.Candidate!=null)RenderPreview();
            if(existingSigns.Error!=null)throw new InvalidOperationException(existingSigns.Error);
        }
        void ReportPlacementFailure(PlacementWork work,string error) {
            if(work==PlacementWork.Signs){if(signWarning==error)return;signWarning=error;}
            else if(error!=null)status=LocalText.Get("Signalbau: ")+error;
            if(error!=null)log?.Warning((work==PlacementWork.Signs?"Optionale Schildanzeige: ":"Signalbau "+work+": ")+error);
        }
        void UpdateAim() {
            MarkerAnchor anchor;string why;
            if(!geometry.Pointer(previewOptions,out anchor,out why)){edit.Offer(null);scene.HidePreview();status=why;return;}
            AlignPlacement(anchor,previewOptions);
            edit.Offer(anchor);RenderPreview();status=edit.Repeating?
                LocalText.Select("Signal placement active. Each left-click places another signal; Escape finishes. Right mouse remains available for the camera.","Signalplatzierung aktiv. Jeder Linksklick setzt ein weiteres Signal; Escape beendet. Rechte Maustaste bleibt für die Kamera verfügbar."):
                LocalText.Select("Preview follows the mouse. Left-click places; Escape cancels. Right mouse remains available for the camera.","Vorschau folgt der Maus. Linksklick setzt; Escape bricht ab. Rechte Maustaste bleibt für die Kamera verfügbar.");
        }
        void RenderPreview() {
            var row=records.FirstOrDefault(r=>r.Id==edit.EditingId);
            var anchor=edit.Candidate;
            if(anchor!=null&&(MarkerRecord.IsCircuitKind(edit.Kind))) {
                anchor=anchor.Clone();anchor.GroundSupport=row?.CircuitSupport??true;
            }
            scene.ShowPreview(anchor,edit.Kind,edit.SpeedMph,units.Mode,edit.FacingReverse^(row?.CircuitReverse==true&&edit.Kind==MarkerRecord.CircuitC),edit.Kind==MarkerRecord.CircuitC?0:row?.CircuitYaw??0,row?.HlSignal??edit.HlSignal,row?.PresentationPack??edit.PresentationPack);
        }
        void PrepareSpeedInput(){speedInKmh=units.Mode!="original";speedInput=SignUnits.Number(edit.SpeedMph*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;}
        public void FormatSignTooltip(Game.Mow.PlayerSign sign,ref TooltipInfo tooltip){if(options.AdaptExistingSpeedSigns&&units.Mode!="original"&&sign.Kind==Game.Mow.PlayerSign.KindSpeedRestriction&&sign.Mph.HasValue&&SignUnits.ValidSpeed(sign.Mph.Value))tooltip.Title="Geschwindigkeitsbeschränkung: "+SignUnits.Description(sign.Mph.Value,units.Mode);}
        public void FromTrack(Track.Location location,string kind) {
            setupPlacementAccepted=null;
            if(kind=="build") {
                if(IsPlacing){edit.Cancel();scene.HidePreview();RefreshScene();}
                buildTrack=location;
                page=Page.Build;
                status=LocalText.Get("Gleisposition vorgemerkt. Bauobjekt auswählen.");
                ShowTool();
                return;
            }
            if(kind=="edit"){if(IsPlacing)StopPlacement();page=Page.Objects;ShowTool();return;}
            if(kind=="interlocking"){Entry.OpenInterlocking();return;}
            if(kind=="identifiers"){Entry.InspectSignalTrack(location);return;}
            if(kind=="assign-point"){Entry.AssignSignalPoint(location);return;}
            if(kind=="edit-point"){Act(()=>OpenPointCircuit(Entry.SelectTrackPoint(location)));return;}
            if(kind=="point-circuit-enable"){Act(()=>{Entry.EnableTrackPointCircuit(location);RefreshScene();});return;}
            if(kind=="automation-contact"){Entry.PlaceAutomationContact(location);return;}
            Act(()=>{
                string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
                if(kind!=MarkerRecord.Clearance&&kind!=MarkerRecord.Speed&&kind!=MarkerRecord.Main&&kind!=MarkerRecord.Distant&&kind!=MarkerRecord.Shunt&&!MarkerRecord.IsCircuitKind(kind))throw new InvalidOperationException("Unbekannter Objekttyp.");
                if(records.Length>=MarkerRepository.Maximum)throw new InvalidOperationException("P0-Grenze von 128 Objekten erreicht.");
                if(!location.IsValid)throw new InvalidOperationException("Angeklicktes Gleis ist nicht mehr gültig.");
                var at=location.WithEnd(Track.TrackSegment.End.A);
                var defaults=Defaults;var anchor=new MarkerAnchor{Segment=at.segment.id,Meters=at.distance,Lateral=defaults.Lateral,Height=defaults.Height,Scale=defaults.Scale};
                Vector3 p;Quaternion q;if(!geometry.Resolve(anchor,out p,out q,out why))throw new InvalidOperationException(why);
                previewOptions=defaults;edit.Start(defaults,kind,true);AlignPlacement(anchor,defaults);edit.Offer(anchor);edit.Freeze();PrepareSpeedInput();RenderPreview();
                status=LocalText.Get("Angeklickte Gleisposition übernommen. Vorschau prüfen und bestätigen.");
            });
            page=edit.Candidate==null?Page.Build:Page.Placement;ShowTool();
        }
        string lastGlowStatus;
        int automaticPointCursor;
        void RefreshScene() {
            var automatic=Entry.AutomaticPointBoxes();
            var current=new HashSet<string>(records.Select(r=>r.Id).Concat(automatic.Select(r=>r.Id)));foreach(string id in shown.Where(id=>!current.Contains(id)).ToArray()){scene.Remove(id);shown.Remove(id);}foreach(string id in current)shown.Add(id);
            if(!options.Visible){scene.SetVisible(false);return;}
            if(page==Page.PointBox&&CustomWindowHandle.Create(WindowId)?.IsOpen()!=true)scene.HidePreview();
            var glow=options.Glow;var camera=glow.Enabled&&glow.Halo?Camera.main:null;scene.GlowView(camera==null?(Vector3?)null:camera.transform.position);
            unresolved=0;foreach(var row in records){if(row.Id==edit.EditingId){scene.Hide(row.Id);continue;}var visual=row.Clone();visual.SharedPointBoundary=row.IsSignal&&Entry.IsSharedSignalCircuit(row.Id);string why;if(!scene.Show(visual,out why,units.Mode))unresolved++;}
            // Keep terrain raycasts and first-time model creation bounded for large networks.
            for(int i=0;i<Math.Min(32,automatic.Length);i++) {
                if(automaticPointCursor>=automatic.Length)automaticPointCursor=0;
                var row=automatic[automaticPointCursor++].Clone();
                if(!DrawPointBox(row))unresolved++;
            }
            if(scene.GlowStatus!=null&&scene.GlowStatus!=lastGlowStatus){lastGlowStatus=scene.GlowStatus;log?.Info(lastGlowStatus);Entry.RecordSignalEvent("signal_glow_renderer",new{renderer=lastGlowStatus,scope="local_visual_only",weather_compatibility_verified=false});}
        }
        void RefreshRecords() {
            records=repository.Records.ToArray();if(!records.Any(x=>x.Id==selectedId))selectedId=records.FirstOrDefault()?.Id;
        }
        MarkerRecord Selected=>records.FirstOrDefault(x=>x.Id==selectedId);
        public string ObjectCaption(string id) {
            var row=records.FirstOrDefault(x=>x.Id==id)??Entry.AutomaticPointBoxes().FirstOrDefault(x=>x.Id==id);
            return row==null?"Signaltechnik":row.IsCircuitBox?CircuitIdentity.Caption(row):row.Name;
        }
        public void OpenObject(string id,int direction=0) {
            if(edit.Repeating)return;
            if(StateManager.IsHost&&records.Any(r=>r.Id==id&&r.Kind==MarkerRecord.Main&&r.Name.StartsWith("RS-",StringComparison.OrdinalIgnoreCase)))Act(()=>{repository.NormalizeSignalCode(id);RefreshRecords();});
            if(QuickCircuitClick(id))return;
            if(!records.Any(r=>r.Id==id))return;
            if(!StateManager.IsHost||!buildMode.Active){selectedId=id;directionTab=direction==1?1:0;page=Page.ReadOnly;ShowTool();return;}
            edit.Cancel();scene.HidePreview();selectedId=id;selectedEditFor=null;LoadDraft();directionTab=direction==1?1:0;page=Page.Configure;ShowTool();
        }
        internal void OpenObjectContext(string id,int direction) {
            if(edit.Repeating)return;
            var row=records.FirstOrDefault(r=>r.Id==id);if(row?.IsSignal!=true){OpenObject(id,direction);return;}
            var menu=UI.ContextMenu.ContextMenu.Shared;menu.Clear();
            menu.AddButton(UI.ContextMenu.ContextMenuQuadrant.General,LocalText.Select("Edit","Bearbeiten"),()=>Act(()=>OpenObject(id,direction)));menu.Show(row.DisplayTitle);
        }
        void LoadDraft() {
            var row=Selected;if(row==null)return;
            selectedEditFor=row.Id;codeInput=row.Name;displayNameInput=row.DisplayName??"";labelScaleInput=SignUnits.Number(row.LabelScale??1);facingInput=row.FacingReverse;linkedInput=row.LinkedSignal;mastDraft=row.Kind==MarkerRecord.Main?MastConfiguration.Effective(row):null;
            fixedHeightInput=row.Anchor.FixedHeight;groundSupportInput=row.Anchor.GroundSupport;
            useHostInput=row.UseHostPlacement;lateralInput=SignUnits.Number(row.Anchor.Lateral);heightInput=SignUnits.Number(row.Anchor.Height);scaleInput=SignUnits.Number(row.Anchor.Scale);
            draftSpeed=row.SpeedMph;speedInKmh=units.Mode!="original";speedInput=SignUnits.Number(draftSpeed*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;directionTab=0;
        }
        void ShowTool() {
            if(!buildMode.Active&&page!=Page.Materials&&page!=Page.ReadOnly) {
                string reason;if(!CanEdit(out reason)){status=reason;return;}
                buildMode.Begin(true);scene.Building=true;RefreshScene();
            }
            EnsureWindow();
            if(manager==null||StateManager.IsUnloading)return;
            bool cached=manager.GetCustomWindow(WindowId)!=null;
            CustomWindowHandle.Create(WindowId)?.Show();
            if(buildMode.Active)buildWindowShown=true;
            if(cached)RebuildTool();
        }
        public void ApplyHostPlacement(){string why;if(!CanEdit(out why))throw new InvalidOperationException(why);repository.ApplyHostPlacement(Defaults,Entry.MastCanModify);RefreshRecords();selectedEditFor=null;RefreshScene();status=LocalText.Get("Host-Vorgaben auf bestehende Masten mit Host-Bindung angewendet.");RebuildTool();}
        Action<MarkerRecord> setupPlacementAccepted;
        public void BeginSetupPlacement(string kind,Action<MarkerRecord> accepted) {
            if(kind!=MarkerRecord.Main&&kind!=MarkerRecord.Shunt&&kind!=MarkerRecord.Distant)throw new InvalidOperationException(LocalText.Select("Choose a supported signal type.","Einen unterstützten Signaltyp wählen."));
            StartPreview(kind,false);setupPlacementAccepted=accepted;
        }
        void StartPreview()=>StartPreview(MarkerRecord.Clearance);
        void StartPreview(string kind,bool repeating=true) {
            setupPlacementAccepted=null;
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            if(records.Length>=MarkerRepository.Maximum)throw new InvalidOperationException("P0-Grenze von 128 Zeichen erreicht.");
            previewOptions=Defaults;
            edit.Start(previewOptions,kind,repeating);placeAfter=Time.realtimeSinceStartup+.25;PrepareSpeedInput();UpdateAim();page=Page.Build;ShowTool();
        }
        void Commit() {
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            var original=records.FirstOrDefault(r=>r.Id==edit.EditingId);if(original!=null&&!Entry.MastCanModify(original))throw new InvalidOperationException("Mast ist inzwischen durch eine Fahrstraße verschlossen. Vorschau abbrechen.");
            if(edit.Kind==MarkerRecord.Speed&&speedDirty){edit.SpeedMph=SignUnits.Input(speedInput,speedInKmh);speedDirty=false;}
            if(edit.Aiming)UpdateAim();
            var setupAccepted=original==null&&!edit.Repeating?setupPlacementAccepted:null;setupPlacementAccepted=null;
            var row=edit.Commit(repository,a=>{Vector3 p;Quaternion q;string error;return geometry.Resolve(a,out p,out q,out error);});
            scene.HidePreview();RefreshRecords();selectedId=row.Id;string reason;
            bool drawn=scene.Show(row,out reason,units.Mode);status=row.Name+" gespeichert."+(drawn?"":" Darstellung: "+reason);
            Entry.RecordMarkerEvent("placed_or_moved",row);selectedEditFor=null;
            if(edit.Repeating){placeAfter=Time.realtimeSinceStartup+.25;page=Page.Build;RebuildTool();return;}
            page=Page.Configure;LoadDraft();RebuildTool();
            setupAccepted?.Invoke(row.Clone());
        }
        void FromVehicle() {
            setupPlacementAccepted=null;
            string why;if(!CanEdit(out why))throw new InvalidOperationException(why);
            if(records.Length>=MarkerRepository.Maximum&&edit.EditingId==null)throw new InvalidOperationException("P0-Grenze von 128 Zeichen erreicht.");
            var car=Game.Scripting.Modules.Railroader.World.getSelectedCar()??Game.Scripting.Modules.Railroader.World.getInspectorCar();
            if(car==null)throw new InvalidOperationException("Zuerst eine Lok oder einen Wagen auswählen.");
            var location=car.LocationF;if(!location.IsValid)throw new InvalidOperationException("Fahrzeug hat keine gültige Gleisposition.");
            location=location.WithEnd(Track.TrackSegment.End.A);
            var defaults=Defaults;var anchor=new MarkerAnchor{Segment=location.segment.id,Meters=location.distance,Lateral=defaults.Lateral,Height=defaults.Height,Scale=defaults.Scale};
            Vector3 p;Quaternion q;if(!geometry.Resolve(anchor,out p,out q,out why))throw new InvalidOperationException(why);
            string kind=edit.Kind,existing=edit.EditingId;double speed=edit.SpeedMph;bool reverse=edit.FacingReverse,useHost=edit.UseHostPlacement,hl=edit.HlSignal,repeating=edit.Repeating;
            if(existing!=null){anchor.FixedHeight=edit.Candidate?.FixedHeight??false;anchor.GroundSupport=edit.Candidate?.GroundSupport??false;}
            if(existing!=null)edit.Edit(new MarkerRecord{Id=existing,Kind=kind,SpeedMph=speed,FacingReverse=reverse,UseHostPlacement=useHost,Anchor=anchor});
            else{previewOptions=defaults;edit.Start(defaults,kind,repeating);AlignPlacement(anchor,defaults);edit.Offer(anchor);edit.Freeze();edit.SpeedMph=speed;edit.HlSignal=hl;}
            page=Page.Placement;PrepareSpeedInput();RenderPreview();status=LocalText.Get("Fahrzeugfront übernommen. Noch nicht gespeichert.");RebuildTool();
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
        void Adjust(double along,double lateral,double height=0) {
            if(edit.Candidate==null)throw new InvalidOperationException("Zuerst eine Vorschau wählen.");
            edit.Freeze();var next=edit.Candidate.Clone();next.Meters+=along;next.Lateral+=lateral;next.Height=Math.Round(next.Height+height,3);next.Validate();Vector3 p;Quaternion q;string why;
            if(!geometry.Resolve(next,out p,out q,out why))throw new InvalidOperationException(why);
            edit.Edit(new MarkerRecord{Id=edit.EditingId,Anchor=next,Kind=edit.Kind,SpeedMph=edit.SpeedMph,FacingReverse=edit.FacingReverse,UseHostPlacement=edit.UseHostPlacement&&lateral==0&&height==0});RenderPreview();status=LocalText.Get("Vorschau geändert; zum Übernehmen bestätigen.");
        }
        MarkerAnchor FitGround(MarkerAnchor anchor) {
            if(!scene.TryGroundHeight(anchor,out double height))throw new InvalidOperationException("Boden unter der Aufstellfläche nicht vollständig erkannt. Manuelle Höhe verwenden.");
            var next=anchor.Clone();next.Height=height;next.FixedHeight=true;next.GroundSupport=true;
            next.Validate();return next;
        }
        void FitPreviewGround() {
            if(edit.Candidate==null)throw new InvalidOperationException("Zuerst eine Vorschau wählen.");
            var next=FitGround(edit.Candidate);
            edit.Edit(new MarkerRecord{Id=edit.EditingId,Anchor=next,Kind=edit.Kind,SpeedMph=edit.SpeedMph,FacingReverse=edit.FacingReverse,UseHostPlacement=false});
            RenderPreview();RebuildTool();
        }
        void FitSelectedGround() {
            RequireSelectedChange();var next=FitGround(Selected.Anchor);
            repository.ConfigurePlacement(selectedId,next.Lateral,next.Height,next.Scale,false,true,true);
            Saved("Aufstellhöhe am erkannten Boden ausgerichtet und fest gespeichert.");
        }
        void PreviewGround(bool? fixedHeight=null,bool? support=null) {
            if(edit.Candidate==null)throw new InvalidOperationException("Zuerst eine Vorschau wählen.");
            edit.Freeze();var next=edit.Candidate.Clone();
            if(fixedHeight.HasValue)next.FixedHeight=fixedHeight.Value;
            if(support.HasValue)next.GroundSupport=support.Value;
            edit.Edit(new MarkerRecord{Id=edit.EditingId,Anchor=next,Kind=edit.Kind,SpeedMph=edit.SpeedMph,FacingReverse=edit.FacingReverse,UseHostPlacement=edit.UseHostPlacement});
            RenderPreview();RebuildTool();
        }
        public string Command(string command) {
            try {
                switch((command??"open").Trim().ToLowerInvariant()) {
                    case "open":case "öffnen":case "oeffnen":EnsureRepository();if(edit.Repeating)StopPlacement();else if(edit.Aiming){if(edit.Candidate!=null)edit.Freeze();else edit.Cancel();}page=edit.Candidate==null?Page.Build:Page.Placement;ShowTool();return status;
                    case "vorschau":StartPreview();break;
                    case "fahrzeug":FromVehicle();break;
                    case "setzen":Commit();break;
                    case "abbrechen":setupPlacementAccepted=null;edit.Cancel();scene.HidePreview();RefreshScene();status=LocalText.Get("Vorschau abgebrochen.");break;
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
            definition=new CustomWindowDefinition{Identifier=WindowId,Title="EnusDE Signal & Interlocking",BuilderAction=Build,
                Config=WindowConfig.Resizable(new Vector2Int(410,680),new Vector2Int(360,350),new Vector2Int(540,1200),Window.Position.CenterRight)};
            manager.RegisterCustomWindow(definition);
        }
        void HideTool(){if(page==Page.PointBox)scene.HidePreview();if(manager!=null&&!StateManager.IsUnloading)manager.CloseWindow(WindowId);}
        void CloseTool(){if(buildMode.Active)RebuildTool();else HideTool();}
        public void EndBuildMode() {
            Entry.EndLampTest();
            if(!buildMode.End())return;
            buildWindowShown=false;setupPlacementAccepted=null;quickCircuitActive=false;
            edit.Cancel();scene.HidePreview();scene.Building=false;RefreshScene();
            buildTrack=null;page=Page.Build;HideTool();
            status=LocalText.Select("Build mode ended. Confirmed work orders remain.","Baumodus beendet. Bestätigte Bauaufträge bleiben erhalten.");
        }
        internal void CloseForWorldSelection(){edit.Cancel();scene.HidePreview();EndBuildMode();HideTool();}
        void RebuildTool(){if(manager!=null&&!StateManager.IsUnloading)CustomWindowHandle.Create(WindowId)?.SetNeedsRebuild();}
        string AnchorText(MarkerAnchor anchor)=>anchor==null?"Keine gültige Vorschau.":anchor.Segment+" · "+anchor.Meters.ToString("0.00")+" m ab Ende A · seitlich "+anchor.Lateral.ToString("0.00")+" m";
        readonly UIState<bool> technicalExpanded=new UIState<bool>(false);
        void Navigate(Page next) {
            if(page==Page.PointBox&&next!=Page.PointBox)scene.HidePreview();
            if(page==Page.Placement&&next!=Page.Placement){edit.Cancel();scene.HidePreview();RefreshScene();}
            page=next;RebuildTool();
        }
        void Build(UIPanelBuilder b) {
            b.VScrollView(c=>{
                if(buildMode.Active) {
                    c.AddTitle(LocalText.Select("Build mode active","Baumodus aktiv"),"");
                    c.AddButton(LocalText.Select("End build mode","Baumodus beenden"),EndBuildMode);
                }
                c.HStack(row=>{
                    if(buildMode.Active){row.AddButtonSelectable(LocalText.Get("Bauen"),page==Page.Build,()=>Navigate(Page.Build));row.AddButtonSelectable(LocalText.Get("Objekte"),page==Page.Objects||page==Page.Configure,()=>Navigate(Page.Objects));}
                    row.AddButton(LocalText.Get("Material"),OpenMaterials);
                });
                c.AddLabel(()=>status,UIPanelBuilder.Frequency.Periodic);
                switch(page) {
                    case Page.Materials:BuildMaterials(c);break;
                    case Page.ReadOnly:BuildReadOnly(c);break;
                    case Page.Stock:BuildStock(c);break;
                    case Page.Objects:BuildObjects(c);break;
                    case Page.PointBox:BuildPointBoxMenu(c);break;
                    case Page.PointCircuits:BuildPointCircuitMenu(c);break;
                    case Page.QuickCircuits:BuildQuickCircuitTool(c);break;
                    case Page.Configure:BuildConfiguration(c);break;
                    case Page.Placement:BuildPlacement(c);break;
                    default:BuildHome(c);break;
                }
                if(!buildMode.Active)c.AddButtonCompact(LocalText.Get("Schließen"),CloseTool);
            });
        }
        void StartHomePlacement(string kind,bool hl=false,string presentationPack=null) {
            if(presentationPack!=null&&Entry.Shared?.CanPlacePresentation(presentationPack)!=true)throw new InvalidOperationException(LocalText.Select("Activate the experiment and world test license first.","Zuerst Experiment und Welt-Testlizenz freischalten."));
            if(hl)HlTestFeature.Require(options.EnableHlSignalsTest);
            if(buildTrack.HasValue) {
                var at=buildTrack.Value;
                buildTrack=null;
                FromTrack(at,kind);
            } else StartPreview(kind);
            edit.HlSignal=hl;edit.PresentationPack=presentationPack;RenderPreview();
        }
        void BuildHome(UIPanelBuilder c) {
            foreach(var pack in SignalPresentationPacks.Installed)if(Entry.Shared?.CanPlacePresentation(pack.Id)==true) {
                foreach(var kind in new[]{MarkerRecord.Main,MarkerRecord.Distant,MarkerRecord.Shunt}) {
                    var id=pack.Id;var signalKind=kind;
                    c.AddButton(pack.Name+" · "+MarkerRecord.KindLabel(kind),()=>Act(()=>StartHomePlacement(signalKind,false,id)));
                }
            }
            c.AddButton(LocalText.Select("Detection boundary quick tool","GFM-Schnellwerkzeug"),()=>Navigate(Page.QuickCircuits));
            c.AddButton(LocalText.Get("Weichen-Gleisfreimeldung"),()=>Navigate(Page.PointCircuits));
            c.AddTitle(LocalText.Get("Bauen"),buildTrack.HasValue&&buildTrack.Value.IsValid?"Vorgemerkte Gleisposition · "+buildTrack.Value.segment.id:
                "Objekt wählen → Maus aufs Gleis → Linksklick");
            if(buildTrack.HasValue)c.AddButton(LocalText.Get("Position neu mit der Maus wählen"),()=>{buildTrack=null;RebuildTool();});
            BuildPalette(c);
            if(options.EnableHlSignalsTest) {
                c.AddButtonLarge(LocalText.Select("Hl main signal · test","Hl-Hauptsignal · Test"),()=>Act(()=>StartHomePlacement(MarkerRecord.Main,true)));
                c.AddButtonLarge(LocalText.Select("Hl distant signal · test","Hl-Vorsignal · Test"),()=>Act(()=>StartHomePlacement(MarkerRecord.Distant,true)));
            }
            c.AddButton(LocalText.Get("Material und Bauaufträge"),OpenMaterials);
            c.AddButton(LocalText.Get("Bestandssignale ungültig kennzeichnen"),()=>Act(OpenStockSignals));
            c.AddLabel(LocalText.Get("Einen platzierten Mast anklicken, um seine Richtungen und Signalbegriffe einzurichten. Rangiersignale können auch einzeln stehen."));
            c.AddLabel(LocalText.Select("Escape cancels placement. Right mouse can still control the camera.","Escape bricht die Platzierung ab. Die rechte Maustaste bleibt für die Kamera verfügbar."));
            c.AddLabel(LocalText.Select("Signals use continuous placement: keep placing with left-click, finish with Escape, then click individual signals to edit them.","Signale fortlaufend setzen: mit Linksklick weiterbauen, mit Escape beenden, danach einzelne Signale zum Bearbeiten anklicken."));
            var defaults=Defaults;
            c.AddLabel(LocalText.Get("Host-Vorgaben: ")+SignUnits.Number(defaults.Lateral)+" m seitlich · "+SignUnits.Number(defaults.Height)+" m Höhe.");
            c.AddLabel(LocalText.Get("Vorgaben ändern: Auswahlfenster → Einstellungen."));
        }
        void BuildObjects(UIPanelBuilder c) {
            c.AddButton(LocalText.Get("Weichen-Gleisfreimeldung"),()=>Navigate(Page.PointCircuits));
            BuildPointBoxPicker(c);
            c.AddTitle(LocalText.Get("Objekte"),"Oder ein Objekt direkt in der Spielwelt anklicken");
            if(records.Length==0){c.AddLabel(LocalText.Get("Noch keine eigenen Objekte platziert."));return;}
            var choices=records.OrderBy(r=>r.Name,StringComparer.OrdinalIgnoreCase).ToArray();
            int index=Array.FindIndex(choices,r=>r.Id==selectedId);if(index<0){index=0;selectedId=choices[0].Id;}
            c.AddSearchChoice(LocalText.Get("Auswahl"),choices.Select(r=>(r.IsCircuitBox?CircuitIdentity.Caption(r):r.Name)+" · "+MarkerRecord.KindLabel(r.Kind)).ToList(),index,i=>{selectedId=choices[i].Id;selectedEditFor=null;});
            c.AddButtonLarge(LocalText.Get("Ausgewähltes Objekt konfigurieren"),()=>OpenObject(selectedId));
            c.AddLabel(records.Length+" eigene Objekte.");
        }
        void BuildConfiguration(UIPanelBuilder c) {
            var row=Selected;if(row==null){c.AddLabel(LocalText.Get("Objekt nicht mehr vorhanden."));return;}
            if(selectedEditFor!=row.Id)LoadDraft();
            c.AddTitle(row.IsCircuitBox?CircuitIdentity.Caption(row):row.Name,MarkerRecord.KindLabel(row.Kind));
            if(row.IsSignal) {
                Entry.BuildSignalLampTests(c,row,directionTab,action=>Act(()=>{action();RefreshScene();RebuildTool();}));
                c.AddLabel(LocalText.Select("Presentation: ","Darstellung: ")+(row.PresentationPack??LocalText.Select("Built-in","Eingebaut")));
                if(row.PresentationPack!=null) {
                    if(SignalPresentationPacks.Find(row.PresentationPack)==null)c.AddLabel(LocalText.Select("Pack unavailable: built-in fallback is displayed. Signal IDs and routes remain intact.","Paket fehlt: eingebaute Ersatzdarstellung. Signal-IDs und Fahrstraßen bleiben erhalten."));
                    c.AddButton(LocalText.Select("Use built-in presentation","Eingebaute Darstellung verwenden"),()=>Act(()=>{RequireSelectedChange();repository.ConfigurePresentation(row.Id,null);Saved(LocalText.Select("Presentation saved.","Darstellung gespeichert."));}));
                }
                foreach(var pack in SignalPresentationPacks.Installed)if(Entry.Shared?.CanPlacePresentation(pack.Id)==true&&row.PresentationPack!=pack.Id) {
                    var id=pack.Id;c.AddButton(pack.Name,()=>Act(()=>{RequireSelectedChange();repository.ConfigurePresentation(row.Id,id);Saved(LocalText.Select("Presentation saved; signal IDs retained.","Darstellung gespeichert; Signal-IDs erhalten."));}));
                }
            }
            if(row.PresentationPack==null&&(row.Kind==MarkerRecord.Main||row.Kind==MarkerRecord.Distant))BuildHlModelSelection(c,row);
            if(row.PreviousCode!=null)c.AddField(LocalText.Select("Previous code (inactive)","Frühere Kennung (inaktiv)"),()=>row.PreviousCode,UIPanelBuilder.Frequency.Periodic);
            if(row.Kind==MarkerRecord.Main) {
                if(!row.HlSignal)c.AddLabel(LocalText.Select(row.LowMain?"Low main signal · Hp 0 / Hp 1 / Hp 2 / Sh 1":"Main signal on mast",row.LowMain?"Niedriges Hauptsignal · Hp 0 / Hp 1 / Hp 2 / Sh 1":"Hauptsignal am Mast"));
                if(!row.HlSignal&&row.PresentationPack==null)c.AddButton(LocalText.Select(row.LowMain?"Convert to high mast":"Use low main signal model",row.LowMain?"Auf hohen Mast umrüsten":"Als niedriges Hauptsignal ausführen"),()=>Act(()=>{
                    RequireSelectedChange();if(Selected.Id!=row.Id)throw new InvalidOperationException("Signal erneut auswählen.");
                    repository.ConfigureLowMain(row.Id,!row.LowMain);Saved("Bauart geändert. Fahrstraßen und Signal-ID bleiben erhalten.");
                }));
                c.HStack(t=>{
                    t.AddButtonSelectable(LocalText.Get("Richtung A"),directionTab==0,()=>{directionTab=0;c.SetNeedsRebuild();});
                    t.AddButtonSelectable(LocalText.Get("Richtung B"),directionTab==1,()=>{directionTab=1;c.SetNeedsRebuild();});
                    t.AddButtonSelectable(LocalText.Get("Position"),directionTab==2,()=>{directionTab=2;c.SetNeedsRebuild();});
                });
                if(directionTab==2)BuildPositionSettings(c);
                else {
                    c.AddField(LocalText.Select("Automatic mast code","Automatische Mastkennung"),()=>row.Name,UIPanelBuilder.Frequency.Periodic);
                    BuildFace(c,directionTab==0?mastDraft.Front:mastDraft.Back);
                    BuildLabelScale(c);
                    c.AddButtonLarge(LocalText.Get("Mastkonfiguration speichern"),()=>Act(SaveDetails));
                }
            }else {
                if(row.IsSignal)c.AddField(LocalText.Select("Automatic signal code","Automatische Signalkennung"),()=>row.Name,UIPanelBuilder.Frequency.Periodic);
                else c.AddField(LocalText.Get("Name"),c.AddInputField(codeInput,v=>codeInput=v,"Name",48));
                if(row.IsSignal) {
                    c.AddField(LocalText.Get("Anzeigename"),c.AddInputField(displayNameInput,v=>displayNameInput=v,"Leer: Signalkennung",48));
                    BuildLabelScale(c);
                }
                if(row.Kind==MarkerRecord.Shunt||row.Kind==MarkerRecord.Distant) {
                    c.AddFieldToggle(LocalText.Get("Gegenrichtung"),()=>facingInput,v=>facingInput=v,true);
                    if(row.Kind==MarkerRecord.Shunt){c.AddLabel(LocalText.Get("Rangiersignal: Sh 0 / Sh 1."));
                        if(row.PresentationPack==null)c.AddButton(LocalText.Get("Zum niedrigen Hauptsignal aufwerten"),()=>Act(()=>{
                            RequireSelectedChange();if(Selected.Id!=row.Id)throw new InvalidOperationException("Signal erneut auswählen.");
                            repository.PromoteShunt(row.Id);Saved(LocalText.Select("Low main signal: Hp 0 / Hp 1 / Hp 2 / Sh 1. Code prefix updated; internal ID and position retained.","Niedriges Hauptsignal: Hp 0 / Hp 1 / Hp 2 / Sh 1. Kennungspräfix angepasst; interne ID und Position bleiben erhalten."));
                        }));}
                    else BuildTarget(c,linkedInput,v=>linkedInput=v,null);
                }
                if(row.Kind==MarkerRecord.Speed)BuildSpeed(c);
                c.AddButton(LocalText.Get("Objekt speichern"),()=>Act(SaveDetails));
                c.AddExpandable(LocalText.Get("Position und Abstände"),positionExpanded,BuildPositionSettings,ExpandableStyle.SectionHeader);
            }
            if(row.IsSignal&&!row.Planned&&(row.Kind!=MarkerRecord.Main||directionTab<2)) {
                c.AddButton(LocalText.Get("Signal zuordnen"),()=>Act(()=>{
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
            if(row.IsSignal||row.IsCircuitBox)BuildCircuitAttachment(c,row);
            if(row.IsSignal&&row.Kind!=MarkerRecord.Main)Entry.BuildSignalOperatingEditor(c,row.Id,()=>{if(Selected?.Id!=row.Id)throw new InvalidOperationException(LocalText.Select("Reopen the signal.","Signal erneut öffnen."));});
            if(CircuitIdentity.Enabled(row))
                c.AddButton(LocalText.Get("Als Grenze für Gleisfreimeldung vormerken"),()=>Act(()=>{
                    if(Selected?.Id!=row.Id)throw new InvalidOperationException("Objekt erneut auswählen.");
                    Entry.RememberCircuitBoundary(row.Id);
                    status=CircuitIdentity.Caption(row)+" als Abschnittsgrenze vorgemerkt.";
                    RebuildTool();
                }));
            c.AddButton(LocalText.Get("Auf dem Gleis verschieben"),()=>Act(EditSelected));
            c.AddButton(LocalText.Get("Dieses Objekt entfernen"),()=>Act(RemoveSelected));
            c.AddExpandable(LocalText.Get("Technische Daten"),technicalExpanded,t=>{
                t.AddField(LocalText.Get("Objekt-ID"),row.Id);t.AddField(LocalText.Get("Gleisanker"),AnchorText(row.Anchor));
                if(row.Kind==MarkerRecord.Main){t.AddField(LocalText.Get("ID Richtung A"),mastDraft.Front.Id);t.AddField(LocalText.Get("ID Richtung B"),mastDraft.Back.Id);}
            },ExpandableStyle.SectionHeader);
        }
        void BuildCircuitAttachment(UIPanelBuilder c,MarkerRecord row) {
            int variant=CircuitIdentity.Variant(row);
            string offset=SignUnits.Number(row.CircuitOffset),yaw=SignUnits.Number(row.CircuitYaw);
            bool support=row.CircuitSupport,reverse=row.CircuitReverse;
            c.AddSection(row.IsSignal?"Gleisfreimeldung am Signal":"Gleisfreimeldekasten");
            if(row.IsSignal&&Entry.IsSharedSignalCircuit(row.Id)) {
                c.AddLabel(LocalText.Select("A/B · shared signal and point boundary","A/B · gemeinsame Signal- und Weichengrenze"));
                c.AddLabel(LocalText.Select("Used by points: ","Mitbenutzt von Weichen: ")+string.Join(", ",Entry.SharedSignalPoints(row.Id)));
            }
            if(row.IsCircuitBox)c.AddField(LocalText.Get("Bauart"),c.AddDropdown(new List<string>{LocalText.Select("B · two-sided boundary","B · beidseitige Grenze"),LocalText.Select("C · one-sided boundary","C · einseitige Grenze")},variant==3?1:0,i=>variant=i==1?3:2));
            else c.AddField(LocalText.Get("Kasten"),c.AddDropdown(new List<string>{LocalText.Get("Kein Kasten"),LocalText.Select("A · attached signal boundary","A · Grenze am Signal"),LocalText.Select("C · one-sided boundary","C · einseitige Grenze")},variant==3?2:variant>0?1:0,i=>variant=i==2?3:i));
            c.AddFieldToggle(LocalText.Select("Reverse C monitoring direction","C-Überwachungsrichtung umkehren"),()=>reverse,v=>reverse=v,true);
            c.AddLabel(LocalText.Select("A: signal boundary. B: free-standing or point boundary. C: monitored only on the arrow side; its back cannot close another detection section. Cosmetic rotation does not change monitoring direction.","A: Signalgrenze. B: freie oder Weichengrenze. C: nur auf der Pfeilseite überwacht; die Rückseite schließt keinen weiteren Freimeldeabschnitt. Die optische Drehkorrektur ändert die Überwachungsrichtung nicht."));
            c.AddLabel(()=>scene.CircuitWarning(row.Id)??"",UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(row.IsSignal?"0° richtet die beschriftete Front wie das Signal aus. Die Drehkorrektur betrifft nur den Kasten.":"Drehkorrektur zum Gleis; die logische GFM-Grenze bleibt unverändert.");
            c.AddField(LocalText.Get("Drehkorrektur (Grad)"),c.AddInputField(yaw,v=>yaw=v,"−180 bis 180",12));
            c.AddFieldToggle(LocalText.Get("Unterbau für Kasten"),()=>support,v=>support=v,true);
            if(row.CircuitNumber>0)c.AddLabel(LocalText.Get("Entitätsnummer ")+row.CircuitNumber.ToString("D3"));
            if(row.IsSignal)c.AddField(LocalText.Get("Kasten längs versetzen (m)"),c.AddInputField(offset,v=>offset=v,"−2 bis 2",8));
            c.AddButton(LocalText.Get("GFM-Kasten übernehmen"),()=>Act(()=>{
                RequireSelectedChange();
                if(Selected.Id!=row.Id)throw new InvalidOperationException("Signal erneut auswählen.");
                repository.ConfigureCircuit(row.Id,variant,variant==0?0:Dimension(offset,-2,2),Dimension(yaw,-180,180),support,reverse);
                Saved(variant==0?"Gleisfreimeldekasten entfernt.":"Gleisfreimeldekasten verknüpft.");
            }));
        }
        readonly UIState<bool> positionExpanded=new UIState<bool>(false);
        void BuildFace(UIPanelBuilder c,SignalFace face) {
            c.AddFieldToggle(LocalText.Get("Signalkopf in dieser Richtung"),()=>face.Enabled,v=>{face.Enabled=v;c.SetNeedsRebuild();},true);
            if(!face.Enabled){c.AddLabel(LocalText.Get("In dieser Richtung ist kein Signal eingerichtet."));return;}
            c.AddField(LocalText.Select("Automatic signal code","Automatische Signalkennung"),()=>face.Code,UIPanelBuilder.Frequency.Periodic);
            c.AddField(LocalText.Get("Anzeigename"),c.AddInputField(face.DisplayName??"",v=>face.DisplayName=v,"Leer: Mastname",48));
            c.AddLabel(LocalText.Get("Hp 0 · Halt ist immer verfügbar."));
            if(Selected?.HlSignal==true) {
                c.AddFieldToggle(LocalText.Select("Hl · unrestricted train route","Hl · Zugfahrt ohne Signalbegrenzung"),()=>face.Hp1,v=>face.Hp1=v,true);
                c.AddFieldToggle(LocalText.Select("Hl · reduced route (40 km/h display)","Hl · Langsamfahrt (Anzeige 40 km/h)"),()=>face.Hp2,v=>face.Hp2=v,true);
                c.AddFieldToggle(LocalText.Select("Ra 12 · shunting permission","Ra 12 · Rangierfahrt"),()=>face.Sh1,v=>face.Sh1=v,true);
                c.AddFieldToggle(LocalText.Select("Combined distant function in Hl head","Vorsignalfunktion im Hl-Schirm"),()=>face.DistantHead,v=>{face.DistantHead=v;c.SetNeedsRebuild();},true);
                c.AddField(LocalText.Select("Current Hl aspect","Aktuelles Hl-Signalbild"),()=>HlLampPlan.Runtime(MarkerRecord.Main,Entry.SignalAspect(face.Id),Entry.SignalAspect(face.DistantTarget),face.DistantHead).Code,UIPanelBuilder.Frequency.Periodic);
            } else {
                c.AddFieldToggle(LocalText.Get("Hp 1 · Fahrt"),()=>face.Hp1,v=>face.Hp1=v,true);
                c.AddFieldToggle(LocalText.Get("Hp 2 · Langsamfahrt"),()=>face.Hp2,v=>face.Hp2=v,true);
                c.AddFieldToggle(LocalText.Get("Sh 1 · Rangierfahrt am Mast"),()=>face.Sh1,v=>face.Sh1=v,true);
                if(Selected?.LowMain!=true)c.AddFieldToggle(LocalText.Get("Vorsignalkopf am Mast"),()=>face.DistantHead,v=>{face.DistantHead=v;c.SetNeedsRebuild();},true);
            }
            if(face.DistantHead)BuildTarget(c,face.DistantTarget,v=>face.DistantTarget=v,face.Id);
            string roleMast=Selected?.Id,roleWorld=scope;int roleDirection=directionTab;
            Entry.BuildSignalRoleEditor(c,face.Id,()=>{if(Selected?.Id!=roleMast||scope!=roleWorld||directionTab!=roleDirection)throw new InvalidOperationException(LocalText.Select("Signal selection changed. Reopen its properties.","Signalauswahl geändert. Eigenschaften erneut öffnen."));});
            Entry.BuildSignalContactStatus(c,face.Id);
        }
        void BuildLabelScale(UIPanelBuilder c) {
            c.AddField(LocalText.Get("Tafelgröße (0,6–2)"),c.AddInputField(labelScaleInput,v=>labelScaleInput=v,"1",6));
        }

        void BuildTarget(UIPanelBuilder c,string selected,Action<string> set,string ownId) {
            var choices=SignalCatalogue.Expand(records).Where(r=>r.Kind==MarkerRecord.Main&&r.Id!=ownId&&r.PhysicalId!=selectedId).OrderBy(r=>r.Name).ToArray();
            var labels=new List<string>{"Kein Hauptsignal zugeordnet"};labels.AddRange(choices.Select(r=>r.Name));
            c.AddSearchChoice(LocalText.Get("Angekündigtes Hauptsignal"),labels,Array.FindIndex(choices,r=>r.Id==selected)+1,i=>set(i==0?null:choices[i-1].Id));
        }
        void BuildSpeed(UIPanelBuilder c) {
            c.AddField(LocalText.Get("Eingabeeinheit"),c.AddDropdown(new List<string>{LocalText.Get("mph"),LocalText.Get("km/h")},speedInKmh?1:0,i=>Act(()=>{
                SyncSpeedInput();speedInKmh=i==1;double value=page==Page.Placement?edit.SpeedMph:draftSpeed;
                speedInput=SignUnits.Number(value*(speedInKmh?SignUnits.MphToKmh:1));speedDirty=false;c.SetNeedsRebuild();
            })));
            c.AddField(LocalText.Get("Geschwindigkeit"),c.AddInputField(speedInput,v=>{speedInput=v;speedDirty=true;},"Wert",null));
            c.AddField(LocalText.Get("Beschriftung"),()=>SignUnits.Caption(page==Page.Placement?edit.SpeedMph:draftSpeed,units.Mode),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(LocalText.Get("Die Schildanzeige folgt der Übersetzungseinstellung und wird auf ganze Zahlen gerundet."));
        }
        void SyncSpeedInput() {
            if(!speedDirty)return;double mph=SignUnits.Input(speedInput,speedInKmh);
            if(page==Page.Placement)edit.SpeedMph=mph;else draftSpeed=mph;speedDirty=false;
        }
        void BuildPositionSettings(UIPanelBuilder c) {
            if(Selected!=null)c.AddLabel(LocalText.Get("Gespeichert: ")+SignUnits.Number(Selected.Anchor.Lateral)+" m seitlich · "+SignUnits.Number(Selected.Anchor.Height)+" m Höhe.");
            if(Selected?.IsSignal==true||Selected?.IsCircuitBox==true) {
                c.AddButton(LocalText.Get("Auf Bodenhöhe ausrichten"),()=>Act(FitSelectedGround));
                c.AddLabel(LocalText.Get("Aufstellhöhe fest ans Gleisbett binden"));
                c.AddFieldToggle(LocalText.Get("Feste Höhe"),()=>fixedHeightInput,v=>{fixedHeightInput=v;c.SetNeedsRebuild();},true);
                c.AddLabel(fixedHeightInput?"Höhe wird vom Gleisbett aus gemessen. Die Bodenerkennung versetzt das Objekt nicht.":"Automatisch auf Gelände setzen; Höhe ist die Korrektur über dem erkannten Boden.");
                c.AddLabel(LocalText.Get("Unterbau bis zum Gelände ergänzen"));
                c.AddFieldToggle(LocalText.Get("Unterbau"),()=>groundSupportInput,v=>groundSupportInput=v,true);
                c.AddLabel(LocalText.Get("Der Unterbau füllt einen Spalt unter dem Sockel. Er verändert oder gräbt das Landschaftsgelände nicht ab."));
            }
            c.AddFieldToggle(LocalText.Get("Host-Vorgaben benutzen"),()=>useHostInput,v=>{useHostInput=v;c.SetNeedsRebuild();},true);
            if(useHostInput){var value=Defaults;c.AddLabel(LocalText.Get("Seitlich ")+SignUnits.Number(value.Lateral)+" m · Höhe "+SignUnits.Number(value.Height)+" m · Größe "+SignUnits.Number(value.Scale)+"×");}
            else {
                c.AddField(LocalText.Get("Seitlicher Abstand (m)"),c.AddInputField(lateralInput,v=>lateralInput=v,"-5 bis 5",null));
                c.AddLabel(fixedHeightInput?"Höhe zum Gleisbett (m)":"Höhenkorrektur über Gelände (m)");
                c.AddInputField(heightInput,v=>heightInput=v,"-1 bis 2",null);
                c.AddField(LocalText.Get("Größe"),c.AddInputField(scaleInput,v=>scaleInput=v,"0,5 bis 3",null));
            }
            c.AddButton(LocalText.Get("Abstände übernehmen"),()=>Act(SavePosition));
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
            Entry.EndLampTest();
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
            repository.ConfigurePlacement(selectedId,useHostInput?defaults.Lateral:Dimension(lateralInput,-5,5),useHostInput?defaults.Height:Dimension(heightInput,-1,2),useHostInput?defaults.Scale:Dimension(scaleInput,.5,3),useHostInput,fixedHeightInput,groundSupportInput);
            Saved("Abstände gespeichert.");
        }
        void BuildPlacement(UIPanelBuilder c) {
            c.AddTitle(LocalText.Get("Positionieren"),MarkerRecord.KindLabel(edit.Kind));
            c.AddLabel(LocalText.Get("Blau ist die Vorschau. Bestätigen speichert, Abbrechen verwirft sie."));
            if(edit.Repeating)c.AddLabel(LocalText.Select("After confirming, place more signals of this type in the world. Escape finishes the series.","Nach dem Bestätigen weitere Signale dieses Typs in der Welt setzen. Escape beendet die Serie."));
            if(edit.Kind==MarkerRecord.Speed)BuildSpeed(c);
            c.HStack(t=>{t.AddButton(LocalText.Get("Bestätigen"),()=>Act(Commit));t.AddButton(LocalText.Get("Abbrechen"),()=>{Command("abbrechen");Navigate(Page.Build);});});
            c.AddSection(LocalText.Get("Fein verschieben"));
            c.HStack(t=>{t.AddButtonCompact(LocalText.Get("−0,25 m am Gleis"),()=>Act(()=>Adjust(-.25,0)));t.AddButtonCompact(LocalText.Get("+0,25 m am Gleis"),()=>Act(()=>Adjust(.25,0)));});
            c.HStack(t=>{t.AddButtonCompact(LocalText.Get("−0,10 m seitlich"),()=>Act(()=>Adjust(0,-.1)));t.AddButtonCompact(LocalText.Get("+0,10 m seitlich"),()=>Act(()=>Adjust(0,.1)));});
            c.HStack(t=>{t.AddButtonCompact(LocalText.Get("−0,10 m tiefer"),()=>Act(()=>Adjust(0,0,-.1)));t.AddButtonCompact(LocalText.Get("+0,10 m höher"),()=>Act(()=>Adjust(0,0,.1)));});
            if(edit.Kind==MarkerRecord.Main||edit.Kind==MarkerRecord.Distant||edit.Kind==MarkerRecord.Shunt||MarkerRecord.IsCircuitKind(edit.Kind)) {
                c.AddButton(LocalText.Get("Auf Bodenhöhe ausrichten"),()=>Act(FitPreviewGround));
                c.AddLabel(LocalText.Get("Aufstellhöhe fest ans Gleisbett binden"));
                c.AddFieldToggle(LocalText.Get("Feste Höhe"),()=>edit.Candidate?.FixedHeight==true,v=>Act(()=>PreviewGround(fixedHeight:v)),true);
                c.AddLabel(LocalText.Get("Unterbau bis zum Gelände ergänzen"));
                c.AddFieldToggle(LocalText.Get("Unterbau"),()=>edit.Candidate?.GroundSupport==true,v=>Act(()=>PreviewGround(support:v)),true);
                c.AddLabel(LocalText.Get("Unterbau ergänzt den Sockel; das Landschaftsgelände bleibt unverändert."));
            }
            c.AddButton(LocalText.Get("Mit der Maus weiterbewegen"),()=>Act(()=>{if(edit.Candidate==null)throw new InvalidOperationException("Keine gültige Vorschau.");SyncSpeedInput();previewOptions=new MarkerOptions{Lateral=edit.Candidate.Lateral,Height=edit.Candidate.Height,Scale=edit.Candidate.Scale};edit.Aim();placeAfter=Time.realtimeSinceStartup+.25;RebuildTool();}));
            c.AddButton(LocalText.Get("An ausgewählte Fahrzeugfront"),()=>Act(FromVehicle));
            c.AddField(LocalText.Get("Gleisposition"),()=>AnchorText(edit.Candidate),UIPanelBuilder.Frequency.Periodic);
        }
        public void Shutdown() {
            Cleanup(DisposeCrosses);
            if(stopped)return;stopped=true;buildMode.End();edit.Cancel();Cleanup(DisposeBuildIcons);Cleanup(()=>modePicker?.Dispose());Cleanup(()=>existingSigns.Dispose());Cleanup(()=>scene?.Dispose());records=new MarkerRecord[0];repository=null;Cleanup(RemoveWindow);manager=null;definition=null;
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
