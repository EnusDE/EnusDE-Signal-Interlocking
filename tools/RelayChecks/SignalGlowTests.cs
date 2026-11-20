using System;
using System.Linq;
using EnusDE.Signaltechnik;
static class SignalGlowTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static SignalLensColor[] L(string kind,LocalSignalAspect aspect)=>SignalLensPlan.For(kind,aspect);
    public static void Run() {
        var hv=HvSignalGeometry.Lamps(MarkerRecord.Main);
        Check(hv.Length==3&&hv[0][0]>hv[2][0]&&hv[1][0]>hv[2][0]&&hv[0][1]>hv[2][1]&&hv[2][1]>hv[1][1]&&
            L(MarkerRecord.Main,LocalSignalAspect.Stop)[2]==SignalLensColor.Red&&
            L(MarkerRecord.Main,LocalSignalAspect.Slow)[0]==SignalLensColor.Green&&L(MarkerRecord.Main,LocalSignalAspect.Slow)[1]==SignalLensColor.Yellow,
            "H/V main places green upper right, red on the left and yellow lower right without changing Hp0/Hp1/Hp2 optical indices");
        var sh=HvSignalGeometry.Lamps(MarkerRecord.Shunt);
        Check(sh[0][1]==sh[1][1]&&sh[2][0]<sh[3][0]&&sh[2][1]<sh[3][1]&&HvSignalGeometry.LensScale(MarkerRecord.Shunt)<1,
            "H/V shunting head keeps horizontal red stop lights and rising diagonal white Sh1 lights with separated smaller lenses");
        var lattice=MarkerMeshData.CreateMast();
        Check(lattice.Vertices.Count<1000&&lattice.Vertices.Any(v=>v[0]>.09f)&&lattice.Vertices.Any(v=>v[0]<-.09f)&&
            lattice.Triangles.All(t=>t.Count%3==0&&t.All(i=>i>=0&&i<lattice.Vertices.Count)),
            "the original H/V lattice mast has bounded bracing geometry and valid triangle indices");
        var mastSign=MarkerMeshData.CreateMastSign();
        Check(mastSign.Triangles[0].Count==72&&mastSign.Triangles[1].Count==36&&mastSign.Vertices.All(v=>v[1]>=3&&v[1]<=3.8f),
            "main mast sign carries distinct white top and bottom bands and one red center band");
        var plate=MarkerMeshData.CreateSignalPlate();
        Check(plate.Vertices.Count==8&&plate.Triangles[0].Count==36&&
            plate.Vertices.All(v=>Math.Abs(v[0])<=.39f&&Math.Abs(v[1])<=.105f)&&
            plate.Vertices.Min(v=>v[2])<0&&plate.Vertices.Max(v=>v[2])>0,
            "the signal nameplate is a bounded opaque box with a solid reverse side");
        Check(L(MarkerRecord.Main,LocalSignalAspect.Stop).SequenceEqual(new[]{SignalLensColor.Off,SignalLensColor.Off,SignalLensColor.Red}),"Hp0 emits red only at the actual stop lens");
        Check(L(MarkerRecord.Main,LocalSignalAspect.Clear).SequenceEqual(new[]{SignalLensColor.Green,SignalLensColor.Off,SignalLensColor.Off}),"Hp1 emits green without lighting the stop or slow lens");
        Check(L(MarkerRecord.Main,LocalSignalAspect.Slow).SequenceEqual(new[]{SignalLensColor.Green,SignalLensColor.Yellow,SignalLensColor.Off}),"Hp2 illuminates the separate green and yellow lenses");
        var yard=SignalLampPlan.For(LocalSignalAspect.Shunt,LocalSignalAspect.Clear);
        Check(L(MarkerRecord.Main,yard.Main).Count(c=>c==SignalLensColor.Red)==1&&L(MarkerRecord.Shunt,yard.Shunt).SequenceEqual(new[]{SignalLensColor.Off,SignalLensColor.Off,SignalLensColor.White,SignalLensColor.White})&&L(MarkerRecord.Distant,yard.Distant).All(c=>c==SignalLensColor.Off),"mast Sh1 retains red main, two white shunt lights and a fully dark distant head");
        Check(L(MarkerRecord.Shunt,LocalSignalAspect.Stop).SequenceEqual(new[]{SignalLensColor.Red,SignalLensColor.Red,SignalLensColor.Off,SignalLensColor.Off}),"standalone shunt stop keeps both red lenses and no white halo");
        Check(L(MarkerRecord.Distant,LocalSignalAspect.Stop).SequenceEqual(new[]{SignalLensColor.Yellow,SignalLensColor.Yellow,SignalLensColor.Off,SignalLensColor.Off}),"Vr0 lights the two yellow lenses only");
        Check(L(MarkerRecord.Distant,LocalSignalAspect.Clear).SequenceEqual(new[]{SignalLensColor.Off,SignalLensColor.Off,SignalLensColor.Green,SignalLensColor.Green}),"Vr1 lights the two green lenses only");
        Check(L(MarkerRecord.Distant,LocalSignalAspect.Slow).SequenceEqual(new[]{SignalLensColor.Yellow,SignalLensColor.Off,SignalLensColor.Off,SignalLensColor.Green}),"Vr2 uses the existing yellow and green diagonal instead of a false clear aspect");
        Check(new[]{MarkerRecord.Main,MarkerRecord.Shunt,MarkerRecord.Distant}.All(kind=>L(kind,LocalSignalAspect.Dark).All(c=>c==SignalLensColor.Off)),"dark heads have no active lens or halo color");
        Check(L(MarkerRecord.Main,(LocalSignalAspect)99).Last()==SignalLensColor.Red&&L(MarkerRecord.Distant,(LocalSignalAspect)99).Count(c=>c==SignalLensColor.Yellow)==2,"unknown presentation values fall back to stop or expect-stop rather than green");
        var options=new SignalGlowOptions{Radiance=double.NaN,HaloStrength=double.PositiveInfinity}.Normalize();Check(options.Radiance==6&&options.HaloStrength==.75,"nonfinite glow settings use bounded defaults");options=new SignalGlowOptions{Radiance=100,HaloStrength=-1}.Normalize();Check(options.Radiance==24&&options.HaloStrength==.1,"extreme glow preferences cannot escape fixed intensity limits");
        var legacy=Newtonsoft.Json.JsonConvert.DeserializeObject<DiagnosticSettings>("{\"Markers\":{\"Visible\":true}}").Normalize();var copy=legacy.Clone();copy.Markers.Glow.Radiance=12;Check(legacy.Markers.Glow.Enabled&&legacy.Markers.Glow.Radiance==6&&copy.Markers.Glow.Radiance==12,"old local settings receive glow defaults and cloned preferences remain isolated");
        Check(!SignalGlowOptions.ShowHalo(true,true,true,true,0)&&!SignalGlowOptions.ShowHalo(false,true,false,true,0)&&!SignalGlowOptions.ShowHalo(true,false,false,true,0)&&!SignalGlowOptions.ShowHalo(true,true,false,false,0)&&!SignalGlowOptions.ShowHalo(true,true,false,true,100,-1),"preview, disabled glow, disabled halo, missing camera and rear view suppress halo rendering");
        Check(SignalGlowOptions.ShowHalo(true,true,false,true,249*249)&&!SignalGlowOptions.ShowHalo(true,true,false,true,251*251)&&!SignalGlowOptions.ShowHalo(true,true,false,true,double.NaN),"halo work stays inside a finite 250-meter camera range");
        Check(SignalGlowOptions.HaloAlpha(0,0)==.45&&SignalGlowOptions.HaloAlpha(1,0)==0&&SignalGlowOptions.HaloAlpha(1,1)==0&&SignalGlowOptions.HaloAlpha(.2,0)>SignalGlowOptions.HaloAlpha(.8,0),"generated halo mask fades smoothly to transparent edges instead of an opaque square");
    }
}
