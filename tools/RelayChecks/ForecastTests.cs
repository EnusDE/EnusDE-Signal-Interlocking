using System;
using System.Linq;
using System.Reflection;
using Track.Signals;
using EnusDE.Signaltechnik;

public static class ForecastTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static AheadSignal S(string id,double distance,SignalAspect aspect)=>new AheadSignal{Id=id,Distance=distance,Aspect=aspect};
    public static void Run(){
        var target=AiForecast.Select(SignalAspect.Clear,new[]{S("clear",100,SignalAspect.Clear),S("stop",500,SignalAspect.Stop)});Check(target.Id=="stop"&&target.Distance==500&&target.Speed==0&&!target.Approach.HasValue,"own clear signal forecasts the following red signal before the first signal is passed");
        target=AiForecast.Select(SignalAspect.Clear,new[]{S("far",800,SignalAspect.Stop),S("near",200,SignalAspect.Restricting)});Check(target.Id=="near"&&target.Speed==15,"nearest restrictive signal is selected in actual route distance order");
        target=AiForecast.Select(SignalAspect.DivergingClear,new[]{S("slow",100,SignalAspect.DivergingClear),S("stop",400,SignalAspect.Stop)});Check(target.Speed==0&&target.Approach==15&&target.Id=="stop","forecasting stop after slow clear preserves the native slow approach restriction");
        target=AiForecast.Select(SignalAspect.Clear,new[]{S("slow",100,SignalAspect.DivergingClear)});Check(target.Speed==15&&target.Distance==100,"upcoming slow signal adds its speed target without reinterpreting it as unrestricted clear");
        Check(AiForecast.Select(SignalAspect.Stop,new[]{S("clear",50,SignalAspect.Clear)})==null&&AiForecast.Select(SignalAspect.Restricting,new[]{S("clear",50,SignalAspect.Clear)})==null,"forecast never relaxes a current red signal or standalone shunting restriction");
        Check(AiForecast.Select(SignalAspect.Clear,new[]{S("behind",-1,SignalAspect.Stop),S("self",0,SignalAspect.Stop),S("beyond",2001,SignalAspect.Stop)})==null,"signal forecast excludes passed/self markers and respects its bounded two-kilometer horizon");
        target=AiForecast.Select(SignalAspect.Clear,new[]{S("unknown",100,(SignalAspect)99)});Check(target.Speed==0,"unrecognized forecast signal aspect cannot become a permissive speed target");
        target=AiForecast.Select(SignalAspect.Restricting,new[]{S("shunt",100,SignalAspect.Restricting),S("stop",500,SignalAspect.Stop)});Check(target.Speed==0&&target.Approach==15&&target.Id=="stop","consecutive cleared shunt signals forecast the eventual stop while preserving their native shunting speed cap");
        var method=typeof(Model.AI.AutoEngineerPlanner).GetMethod("SetSignalSpeedRestriction",BindingFlags.Instance|BindingFlags.NonPublic);var types=method.GetParameters().Select(p=>p.ParameterType).ToArray();Check(method.ReturnType==typeof(void)&&types.SequenceEqual(new[]{typeof(int?),typeof(int?),typeof(float),typeof(CTCSignal),typeof(bool)}),"installed native speed-target method exactly matches the forecast prefix ref arguments");
    }
}
