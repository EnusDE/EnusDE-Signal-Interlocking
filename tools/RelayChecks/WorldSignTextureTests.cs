using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;
using UnityEngine;

public static class WorldSignTextureTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Same(Color32 a,Color32 b)=>a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;
    static readonly Color32 Black=new Color32(12,15,10,255),White=new Color32(235,231,220,255),Yellow=new Color32(232,172,25,255);
    static Color32[] Plate(string number,Color32 background,Color32 ink,int size=256) {
        var result=Enumerable.Repeat(background,size*size).ToArray();if(number==null)return result;
        // Independent bitmap fixture for the native 25-mph source lettering.
        var patterns=new Dictionary<char,string[]>{
            ['2']=new[]{"01110","11011","00011","00110","01100","11000","11111"},
            ['5']=new[]{"11111","11000","11000","11110","00011","11011","01110"},
            ['4']=new[]{"00011","00111","01111","11011","11111","00011","00011"},
            ['0']=new[]{"01110","11011","11011","11011","11011","11011","01110"}};
        const int scale=8;int width=(number.Length*6-1)*scale,left=(size-width)/2,bottom=(size-7*scale)/2;
        for(int n=0;n<number.Length;n++)for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(patterns[number[n]][6-y][x]=='1')
            for(int dy=0;dy<scale;dy++)for(int dx=0;dx<scale;dx++)result[(bottom+y*scale+dy)*size+left+(n*6+x)*scale+dx]=ink;
        return result;
    }
    public static void Run() {
        var fonts=new[]{SignDigitFont.BuiltIn()};
        foreach(var colors in new[]{new[]{White,Black},new[]{Yellow,Black},new[]{Black,Yellow},new[]{new Color32(25,108,63,255),White}}) {
            var source=Plate("25",colors[0],colors[1]);var saved=(Color32[])source.Clone();
            Check(SignFaceConversion.TryRead(source,256,fonts,out var read)&&read.Mph==25&&Same(read.Background,colors[0])&&Same(read.Ink,colors[1]),
                "world-sign face reads 25 mph and its own foreground/background palette without assigning a sign meaning from color");
            var metric=SignFaceConversion.Convert(source,256,read,"metric",out var changed);
            bool recognized=SignFaceConversion.TryRead(metric,256,fonts,out var result,out var reason);
            Check(recognized&&result.Mph==40&&Same(result.Background,colors[0])&&Same(result.Ink,colors[1])&&
                source.Zip(saved,Same).All(v=>v)&&Enumerable.Range(0,source.Length).Where(i=>!changed[i]).All(i=>Same(source[i],metric[i])),
                "metric repaint changes 25 to 40 while preserving the source, face palette and every untouched paint pixel"+(recognized?"":" · "+reason));
            var original=SignFaceConversion.Convert(source,256,read,"original",out var untouched);
            Check(!untouched.Any(v=>v)&&original.Zip(source,Same).All(v=>v),"original unit mode keeps the complete native face byte-for-byte");
        }
        Check(!SignFaceConversion.TryRead(Plate(null,Yellow,Black),256,fonts,out _),"a blank rear face stays blank instead of receiving a copied speed restriction");
        var slash=Plate(null,White,Black);for(int y=80;y<176;y++)for(int shift=-2;shift<=2;shift++)slash[y*256+y+shift]=Black;
        Check(!SignFaceConversion.TryRead(slash,256,fonts,out _),"an unrecognized release mark is not guessed into a numeric restriction");
        var front=Plate("25",Yellow,Black);var rear=Plate("40",Black,White);var atlas=new Color32[512*256];
        for(int y=0;y<256;y++){Array.Copy(front,y*256,atlas,y*512,256);Array.Copy(rear,y*256,atlas,y*512+256,256);}
        const double r=.002794,z=.0197982,d=.00035875;
        var vertices=new[]{
            new SignTextureVertex(-r,-d,z-r,0,0),new SignTextureVertex(r,-d,z-r,.5,0),new SignTextureVertex(r,-d,z+r,.5,1),new SignTextureVertex(-r,-d,z+r,0,1),
            new SignTextureVertex(-r,d,z-r,1,0),new SignTextureVertex(r,d,z-r,.5,0),new SignTextureVertex(r,d,z+r,.5,1),new SignTextureVertex(-r,d,z+r,1,1)};
        var faces=NativeSignFace.Extract(vertices,new[]{0,1,2,0,2,3,4,6,5,4,7,6});
        var writes=new Dictionary<int,Color32>();var before=(Color32[])atlas.Clone();int count=0;
        foreach(var face in faces) {
            var sampled=face.Sample(atlas,512,256,256,false);
            Check(SignFaceConversion.TryRead(sampled,256,fonts,out var reading)&&reading.Mph==(face.Side<0?25:40),
                "opposite native mesh faces sample their separate UV regions in readable front-view orientation");
            var painted=SignFaceConversion.Convert(sampled,256,reading,"metric",out var changed);face.Paint(painted,changed,256,512,256,false,writes);count++;
        }
        foreach(var pair in writes)atlas[pair.Key]=pair.Value;
        Check(count==2&&writes.Count>0&&Enumerable.Range(0,atlas.Length).Where(i=>!writes.ContainsKey(i)).All(i=>Same(atlas[i],before[i])),
            "two-sided atlas patch changes numeral texels only and preserves every other atlas pixel");
        foreach(var face in faces) {
            var sampled=face.Sample(atlas,512,256,256,false);
            Check(SignFaceConversion.TryRead(sampled,256,fonts,out var reading)&&reading.Mph==(face.Side<0?40:64)&&
                Same(reading.Background,face.Side<0?Yellow:Black)&&Same(reading.Ink,face.Side<0?Black:White),
                "front 25 and rear 40 convert independently to 40 and 64 with their different native colors retained");
        }
    }
}
