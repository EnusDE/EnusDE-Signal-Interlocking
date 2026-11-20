using System;
using System.Collections.Generic;
namespace EnusDE.Signaltechnik {
    public static class BuildSymbols {
        public static List<float[]> For(string kind) {
            var lines=new List<float[]>();Action<float,float,float,float> line=(x,y,a,b)=>lines.Add(new[]{x,y,a,b});
            Action<float,float,float,float> box=(x,y,a,b)=>{line(x,y,a,y);line(a,y,a,b);line(a,b,x,b);line(x,b,x,y);};Action<float,float> lens=(x,y)=>box(x-2,y-2,x+2,y+2);
            if(kind=="BUILD_MAIN"){box(10,11,22,29);lens(16,25);lens(16,19);line(16,11,16,3);line(9,3,23,3);}
            else if(kind=="BUILD_DISTANT"){line(7,16,20,29);line(20,29,28,21);line(28,21,15,8);line(15,8,7,16);lens(14,17);lens(21,24);line(17,10,17,3);line(10,3,24,3);}
            else if(kind=="BUILD_SHUNT"){box(5,9,27,24);lens(11,15);lens(21,20);line(16,9,16,3);line(9,3,23,3);}
            else if(kind=="BUILD_SPEED"){box(6,13,26,29);line(16,13,16,3);line(10,3,22,3);line(10,25,14,25);line(14,25,14,17);line(19,17,19,25);line(19,25,23,17);}
            else if(kind=="BUILD_CLEARANCE"){line(3,7,16,25);line(16,25,29,7);line(29,7,3,7);line(10,8,19,20);line(16,8,23,16);}
            else{box(5,7,27,23);line(5,23,9,28);line(9,28,23,28);line(23,28,27,23);box(9,11,23,19);line(4,3,28,3);line(16,7,16,3);if(kind=="BUILD_GFM_C")line(23,15,29,15);else line(3,15,9,15);}
            return lines;
        }
    }
}
