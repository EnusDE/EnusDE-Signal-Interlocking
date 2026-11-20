using System;
using EnusDE.Signaltechnik;

static class SelectionInputTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run() {
        var input=new WorldSelectionInput();Check(!input.Consumes(10,false),"ordinary world interactions remain available before a selection completes");
        input.Complete();Check(input.Consumes(10,true)&&input.Consumes(11,true),"the click that completes a selection cannot also open properties or throw a point");
        Check(input.Consumes(12,false)&&input.Consumes(12,false),"all release callbacks in the same frame remain owned by the completed selection");
        Check(!input.Consumes(13,false),"a fresh world click becomes available after a complete released frame");
        input.Complete();input.Consumes(20,false);Check(input.Consumes(21,true)&&input.Consumes(22,false)&&!input.Consumes(23,false),"a still-held second mouse button delays rearming rather than leaking a second activation");
        input.Complete();input.Reset();Check(!input.Consumes(30,true),"starting another deliberate selection resets the preceding click guard");
    }
}
