using System;
using System.Linq;
using System.Reflection;
using EnusDE.Signaltechnik;

static class Game063Tests {
    public static class NewPaths {public static string RailroaderDirectory=>@"D:\Railroader user";}
    public static class OldPaths {public static int Reads;public static string RailroaderDirectory {get{Reads++;return @"C:\Legacy user";}}}
    public static class EmptyPaths {public static string RailroaderDirectory=>" ";}
    public static class WrongPaths {public static int RailroaderDirectory=>42;}
    public static class ThrowingPaths {public static string RailroaderDirectory=>throw new InvalidOperationException("native getter failed");}
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run() {
        OldPaths.Reads=0;
        Type Both(string name)=>name=="Game.HomeDirectory.UserPaths"?typeof(NewPaths):name=="Game.UserPaths"?typeof(OldPaths):null;
        Check(RailroaderPaths.Directory(Both)==@"D:\Railroader user"&&OldPaths.Reads==0,"simulated 0.6.3 namespaced user-path API takes precedence over the legacy path without reading legacy settings");
        Check(RailroaderPaths.Directory(name=>name=="Game.HomeDirectory+UserPaths"?typeof(NewPaths):null)==@"D:\Railroader user","path adapter also supports the nested HomeDirectory/UserPaths type spelling");
        Check(RailroaderPaths.Directory(name=>name=="Game.UserPaths"?typeof(OldPaths):null)==@"C:\Legacy user"&&OldPaths.Reads==1,"legacy-library fallback retains the original user path exactly without rewriting or moving user data");
        Check(Reject(()=>RailroaderPaths.Directory(_=>null)),"a missing user-path API is reported explicitly instead of guessing a directory");
        Check(Reject(()=>RailroaderPaths.Directory(name=>name=="Game.HomeDirectory.UserPaths"?typeof(WrongPaths):null))&&Reject(()=>RailroaderPaths.Directory(name=>name=="Game.HomeDirectory.UserPaths"?typeof(EmptyPaths):null)),"wrong path signatures and empty paths cannot silently resolve to a different settings location");
        Check(Reject(()=>RailroaderPaths.Directory(name=>name=="Game.HomeDirectory.UserPaths"?typeof(ThrowingPaths):name=="Game.UserPaths"?typeof(OldPaths):null))&&OldPaths.Reads==1,"a failing new native path getter does not fall back to an unrelated legacy directory");
        var actual=RailroaderPaths.ApiType();var getter=actual.GetProperty("RailroaderDirectory",BindingFlags.Public|BindingFlags.Static);
        Check(getter?.PropertyType==typeof(string)&&getter.GetGetMethod()!=null,"installed game-library user-path metadata matches the adapter contract · "+actual.FullName);
        Check(Entry.Version=="0.7.68","new compatibility package reports mod version 0.7.68");
    }
}
