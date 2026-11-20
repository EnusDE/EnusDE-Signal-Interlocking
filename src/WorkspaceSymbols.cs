using System;
using System.Collections.Generic;
namespace EnusDE.Signaltechnik {
    public static class WorkspaceSymbols {
        public static List<float[]> For(string kind){var result=new List<float[]>();Action<float,float,float,float> line=(x,y,a,b)=>result.Add(new[]{x,y,a,b});
            if(kind=="DISPATCH"){line(4,7,28,7);line(12,7,23,16);line(23,16,28,16);line(8,7,8,13);line(4,13,12,13);line(12,13,12,28);line(12,28,4,28);line(4,28,4,13);line(7,18,9,18);line(7,23,9,23);}
            else if(kind=="BUILD"){line(4,4,28,4);line(4,8,28,8);line(8,3,8,9);line(16,3,16,9);line(24,3,24,9);line(9,13,22,25);line(6,16,19,28);line(6,16,9,13);line(19,28,25,28);line(25,28,28,25);line(28,25,25,22);line(25,22,22,25);}
            else if(kind=="MAPEDIT"){line(3,8,10,12);line(10,12,18,8);line(18,8,25,12);line(25,12,25,16);line(3,8,3,23);line(3,23,10,27);line(10,27,17,23);line(10,12,10,27);line(15,13,16,18);line(15,13,20,14);line(16,18,26,28);line(26,28,30,24);line(30,24,20,14);line(24,26,28,22);}
            else if(kind=="TRACK"){line(3,11,14,11);line(14,11,22,20);line(22,20,29,20);line(3,16,12,16);line(12,16,20,25);line(20,25,29,25);line(6,9,6,18);line(26,18,26,27);}
            else if(kind=="REGION"){line(4,10,4,4);line(4,4,10,4);line(22,4,28,4);line(28,4,28,10);line(4,22,4,28);line(4,28,10,28);line(22,28,28,28);line(28,28,28,22);line(10,16,22,16);line(16,10,16,22);}
            else if(kind=="ROTATE"){line(6,11,6,20);line(6,20,11,26);line(11,26,21,26);line(21,26,26,20);line(26,20,26,12);line(26,12,20,7);line(20,7,12,7);line(6,20,12,20);line(6,20,4,26);}
            else if(kind=="MIRROR"){line(16,4,16,28);line(4,7,12,7);line(12,7,12,25);line(12,25,4,7);line(28,7,20,7);line(20,7,20,25);line(20,25,28,7);}
            else if(kind=="ADD"){line(5,5,27,5);line(27,5,27,27);line(27,27,5,27);line(5,27,5,5);line(10,16,22,16);line(16,10,16,22);}
            else if(kind=="TEXT"){line(5,25,27,25);line(16,25,16,7);line(10,7,22,7);line(5,25,5,21);line(27,25,27,21);}
            else if(kind=="STW"){line(4,5,28,5);line(28,5,28,27);line(28,27,4,27);line(4,27,4,5);line(4,16,28,16);line(16,5,16,27);}
            else if(kind=="GO"){line(6,27,6,6);line(6,6,12,12);line(12,12,17,4);line(17,4,21,7);line(21,7,16,15);line(16,15,26,15);line(26,15,6,27);}
            else if(kind=="EDIT"){line(6,5,10,13);line(10,13,23,26);line(23,26,27,22);line(27,22,14,9);line(14,9,6,5);line(10,13,14,9);}
            else if(kind=="LOCK"){line(7,5,25,5);line(25,5,25,18);line(25,18,7,18);line(7,18,7,5);line(11,18,11,24);line(11,24,16,28);line(16,28,21,24);line(21,24,21,18);line(16,9,16,14);}
            else if(kind=="FS"){line(3,8,12,8);line(12,8,21,23);line(21,23,29,23);line(3,23,12,23);line(12,23,21,8);line(21,8,29,8);line(4,5,4,11);line(28,20,28,26);}
            else if(kind=="BAU"){line(8,5,21,22);line(5,8,18,25);line(16,27,24,19);line(18,29,27,20);line(24,19,27,20);line(16,27,18,29);}
            else if(kind=="ID"){line(3,6,29,6);line(29,6,29,26);line(29,26,3,26);line(3,26,3,6);line(9,10,9,22);line(16,10,16,22);line(16,22,22,19);line(22,19,22,13);line(22,13,16,10);}
            else{line(10,23,13,27);line(13,27,22,25);line(22,25,24,20);line(24,20,16,15);line(16,15,16,11);line(16,5,16,7);}return result;
        }
    }
}
