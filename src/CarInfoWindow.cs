using System;
using Model;
using UI.Builder;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string CarInfoWindowId="enusde.signaltechnik.car-information";
        CustomWindowDefinition carInfoDefinition;
        Car informationCar;
        string informationWorld;
        long informationEpoch;
        void BuildCarInfoEntry(UIPanelBuilder builder,Car car) {
            if(car==null||!car.IsLocomotive||Automation==null)return;
            builder.AddButton(LocalText.Select("Signalling information…","Signaltechnik-Fahrinformationen …"),()=>OpenCarInfo(car));
        }
        void OpenCarInfo(Car car) {
            EnsureWindow();if(manager==null||car==null)return;
            informationCar=car;informationWorld=scope;informationEpoch++;
            if(carInfoDefinition==null||!ReferenceEquals(manager.GetDefinition(CarInfoWindowId),carInfoDefinition)) {
                carInfoDefinition=new CustomWindowDefinition{Identifier=CarInfoWindowId,
                    Title=WindowText("EnusDE · Signalling information","EnusDE · Signaltechnik-Fahrinformationen"),
                    BuilderAction=BuildCarInfoWindow,
                    Config=WindowConfig.Resizable(new Vector2Int(500,440),new Vector2Int(380,300),new Vector2Int(1000,900),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(carInfoDefinition);
            }
            var handle=CustomWindowHandle.Create(CarInfoWindowId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildCarInfoWindow(UIPanelBuilder builder) {
            var car=informationCar;long epoch=informationEpoch;
            builder.VScrollView(c=>{
                if(informationWorld!=scope||car==null||Automation==null){c.AddLabel(LocalText.Select("Vehicle or world no longer available. Reopen from the vehicle window.","Fahrzeug oder Welt nicht mehr verfügbar. Über das Fahrzeugfenster erneut öffnen."));return;}
                c.AddLabel(LocalText.Select("Vehicle: ","Fahrzeug: ")+car.id);
                c.AddField(LocalText.Select("Status","Status"),()=>workspaceActionError??status,UIPanelBuilder.Frequency.Periodic);
                BuildCarAutomation(c,car,()=>epoch==informationEpoch&&car==informationCar&&informationWorld==scope);
            });
        }
    }
}
