namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void OpenSetupForSignalField(string desk,int x,int y) {
            SelectDiagramSetupField(desk,x,y);WorkspacePage(15);
        }
    }
}
