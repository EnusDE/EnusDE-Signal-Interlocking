namespace EnusDE.Signaltechnik {
    public enum LocalSignalAspect { Stop,Clear,Slow,Shunt,Dark,Invalid,Kennlicht }
    public static class NativeSignalAuthority {
        public static Track.Signals.SignalAspect For(LocalSignalAspect aspect,RouteRun via,string id) {
            if(aspect==LocalSignalAspect.Shunt&&via!=null&&!via.Route.Shunting&&via.PermitsVia(id))return Track.Signals.SignalAspect.Clear;
            switch(aspect){case LocalSignalAspect.Clear:return Track.Signals.SignalAspect.Clear;case LocalSignalAspect.Slow:return Track.Signals.SignalAspect.DivergingClear;case LocalSignalAspect.Shunt:return Track.Signals.SignalAspect.Restricting;default:return Track.Signals.SignalAspect.Stop;}
        }
    }
    public struct SignalLampPlan {
        public LocalSignalAspect Main,Shunt,Distant;
        public static SignalLampPlan For(LocalSignalAspect authority,LocalSignalAspect announced) {
            if(authority==LocalSignalAspect.Invalid)return new SignalLampPlan{Main=authority,Shunt=LocalSignalAspect.Dark,Distant=authority};
            if(authority==LocalSignalAspect.Kennlicht)return new SignalLampPlan{Main=authority,Shunt=LocalSignalAspect.Dark,Distant=LocalSignalAspect.Dark};
            return new SignalLampPlan{Main=authority==LocalSignalAspect.Shunt?LocalSignalAspect.Stop:authority,
                Shunt=authority==LocalSignalAspect.Shunt?LocalSignalAspect.Shunt:authority==LocalSignalAspect.Stop?LocalSignalAspect.Stop:LocalSignalAspect.Dark,
                Distant=authority==LocalSignalAspect.Stop||authority==LocalSignalAspect.Shunt?LocalSignalAspect.Dark:announced};
        }
    }
}
