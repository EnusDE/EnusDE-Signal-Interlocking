using System;
using System.Linq;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        string pointBoxId,pointBoxChoice,pointBoxWorld,pointBoxAlong,pointBoxLateral,pointBoxHeight,pointBoxYaw;
        int pointBoxVariant;
        bool pointBoxFixed,pointBoxSupport,pointBoxDirection;
        internal void OpenPointBox(string id) {
            Act(()=>{
                if(!CanEdit(out string why))throw new InvalidOperationException(why);
                string shared=Entry.SharedPointSignal(id);
                if(shared!=null){quickCircuitActive=false;OpenObject(shared);status=LocalText.Select("Shared A/B boundary: edit the box on its signal. Point and signal references are retained.","Gemeinsame A/B-Grenze: Kasten am Signal bearbeiten. Weichen- und Signalverweise bleiben erhalten.");return;}
                var boundary=Entry.PointBoxDefault(id)??throw new InvalidOperationException("Weichenkasten nicht verfügbar.");
                edit.Cancel();scene.HidePreview();pointBoxId=id;pointBoxWorld=scope;
                LoadPointBox(boundary);page=Page.PointBox;ShowTool();
            });
        }
        void LoadPointBox(MarkerRecord boundary) {
            var value=Entry.PointBoxPosition(boundary.Id)??PointBoxPlacement.Default(boundary);
            pointBoxAlong=SignUnits.Number(value.Along);pointBoxLateral=SignUnits.Number(value.Lateral);pointBoxHeight=SignUnits.Number(value.Height);
            pointBoxYaw=SignUnits.Number(value.Yaw);pointBoxVariant=value.Variant;
            pointBoxFixed=value.FixedHeight;pointBoxSupport=value.GroundSupport;pointBoxDirection=value.DirectionFlip;
        }
        MarkerRecord ReadPointBox(out PointBoxPlacement value) {
            if(!CanEdit(out string why))throw new InvalidOperationException(why);
            if(scope!=pointBoxWorld)throw new InvalidOperationException("Spielstand gewechselt. Weichenkasten erneut auswählen.");
            var boundary=Entry.PointBoxDefault(pointBoxId)??throw new InvalidOperationException("Weichengrenze nicht mehr verfügbar.");
            value=new PointBoxPlacement{Along=Dimension(pointBoxAlong,-10,10),Lateral=Dimension(pointBoxLateral,-5,5),
                Height=Dimension(pointBoxHeight,-1,2),Yaw=Dimension(pointBoxYaw,-180,180),Variant=pointBoxVariant,FixedHeight=pointBoxFixed,GroundSupport=pointBoxSupport,DirectionFlip=pointBoxDirection};
            var row=value.Apply(boundary);
            if(!geometry.Resolve(row.Anchor,out _,out _,out string error))throw new InvalidOperationException(error);
            return row;
        }
        bool GroundPointBox(MarkerRecord row) {
            if(row.Anchor.FixedHeight)return true;
            if(!scene.TryGroundHeight(row.Anchor,out double level))return false;
            row.Anchor.Height=level+row.Anchor.Height;
            if(row.Anchor.Height< -1||row.Anchor.Height>2)return false;
            row.Anchor.FixedHeight=true;return true;
        }
        bool DrawPointBox(MarkerRecord row) {
            if(!GroundPointBox(row)){scene.Hide(row.Id);return false;}
            return scene.Show(row,out _,units.Mode);
        }
        void BuildPointBoxPicker(UIPanelBuilder c) {
            var boxes=Entry.AutomaticPointBoxes();if(boxes.Length==0)return;
            if(!boxes.Any(b=>b.Id==pointBoxChoice))pointBoxChoice=boxes[0].Id;
            c.AddSearchChoice(LocalText.Get("Automatische Weichenkästen"),boxes.Select(b=>b.Name).ToList(),Array.FindIndex(boxes,b=>b.Id==pointBoxChoice),i=>pointBoxChoice=boxes[i].Id);
            c.AddButton(LocalText.Get("Weichenkasten bearbeiten"),()=>OpenPointBox(pointBoxChoice));
        }
        void BuildPointBoxMenu(UIPanelBuilder c) {
            var boundary=Entry.PointBoxDefault(pointBoxId);
            if(boundary==null||pointBoxWorld!=scope){c.AddLabel(LocalText.Get("Weichenkasten erneut auswählen."));BuildPointBoxPicker(c);return;}
            c.AddTitle(boundary.Name,"Modellposition des automatischen Weichenkastens");
            c.AddLabel(LocalText.Get("Die GFM-Grenze bleibt am bisherigen Gleispunkt. Nur Kasten und Kabelverlauf werden angepasst."));
            c.AddField(LocalText.Get("Bauart"),c.AddDropdown(new[]{"B","C"}.ToList(),pointBoxVariant==3?1:0,i=>{pointBoxVariant=i==1?3:2;scene.HidePreview();}));
            c.AddFieldToggle(LocalText.Select("Reverse C monitoring direction","C-Überwachungsrichtung umkehren"),()=>pointBoxDirection,v=>pointBoxDirection=v,true);
            c.AddLabel(LocalText.Get("Drehung zum Gleis (Grad)"));c.AddInputField(pointBoxYaw,v=>{pointBoxYaw=v;scene.HidePreview();},"−180 bis 180",12);
            c.AddButton(LocalText.Get("Kasten um 90° drehen"),()=>Act(()=>{double yaw=Dimension(pointBoxYaw,-180,180)+90;pointBoxYaw=SignUnits.Number(yaw>180?yaw-360:yaw);scene.HidePreview();RebuildTool();}));
            c.AddLabel(LocalText.Get("Längsversatz auf dem Grenzsegment (m)"));c.AddInputField(pointBoxAlong,v=>{pointBoxAlong=v;scene.HidePreview();},"−10 bis 10",24);
            c.AddLabel(LocalText.Get("Seitlicher Abstand (m), Vorzeichen wählt die Gleisseite"));c.AddInputField(pointBoxLateral,v=>{pointBoxLateral=v;scene.HidePreview();},"−5 bis 5",24);
            c.AddButton(LocalText.Get("Gleisseite wechseln"),()=>Act(()=>{pointBoxLateral=SignUnits.Number(-Dimension(pointBoxLateral,-5,5));scene.HidePreview();RebuildTool();}));
            c.AddLabel(pointBoxFixed?"Höhe zum Gleisbett (m)":"Höhenkorrektur über Boden (m)");
            c.AddInputField(pointBoxHeight,v=>{pointBoxHeight=v;scene.HidePreview();},"−1 bis 2",24);
            c.AddFieldToggle(LocalText.Get("Feste Höhe"),()=>pointBoxFixed,v=>{pointBoxFixed=v;scene.HidePreview();RebuildTool();},true);
            c.AddFieldToggle(LocalText.Get("Unterbau"),()=>pointBoxSupport,v=>pointBoxSupport=v,true);
            c.AddLabel(LocalText.Get("Blau zeigt die neue Position. Das vorhandene Modell bleibt bis zum Speichern sichtbar."));
            c.AddButton(LocalText.Get("Vorschau anzeigen"),()=>Act(()=>{
                var row=ReadPointBox(out _);if(!GroundPointBox(row))throw new InvalidOperationException("Boden nicht vollständig erkannt. Feste Höhe wählen oder andere Position prüfen.");
                scene.ShowPreview(row.Anchor,row.Kind,circuitYaw:row.Kind==MarkerRecord.CircuitC?0:row.CircuitYaw,reverse:row.Kind==MarkerRecord.CircuitC?CircuitIdentity.Reverse(row):row.FacingReverse);status=LocalText.Get("Vorschau aktiv; Änderungen übernehmen speichert die Modellposition.");
            }));
            c.AddButton(LocalText.Get("Änderungen übernehmen"),()=>Act(()=>{
                var row=ReadPointBox(out var value);
                Entry.PutPointBoxPosition(pointBoxId,value);scene.HidePreview();
                DrawPointBox(row);status=boundary.Name+": Modellposition in der gemeinsamen Anlage gespeichert.";
            }));
            c.AddButton(LocalText.Get("Automatische Position wiederherstellen"),()=>Act(()=>{
                if(!CanEdit(out string why)||scope!=pointBoxWorld)throw new InvalidOperationException(why??"Spielstand gewechselt.");
                Entry.PutPointBoxPosition(pointBoxId,null);scene.HidePreview();
                var original=Entry.PointBoxDefault(pointBoxId);LoadPointBox(original);DrawPointBox(original);
                status=LocalText.Get("Automatische Modellposition wiederhergestellt.");RebuildTool();
            }));
            c.AddButton(LocalText.Get("Als Abschnittsgrenze vormerken"),()=>Act(()=>{ReadPointBox(out _);Entry.RememberCircuitBoundary(pointBoxId);status=boundary.Name+" als Abschnittsgrenze vorgemerkt.";}));
            c.AddButton(LocalText.Get("Zur Objektliste"),()=>Navigate(Page.Objects));
        }
    }
}
