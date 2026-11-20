using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class SignDigitGlyph {
        public readonly int Width,Height;
        public readonly byte[] Alpha;
        public SignDigitGlyph(int width,int height,byte[] alpha){Width=width;Height=height;Alpha=alpha;}
    }
    public sealed class SignDigitFont {
        public readonly SignDigitGlyph[] Digits;
        public SignDigitFont(SignDigitGlyph[] digits){Digits=digits.Select(g=>{
            int l=g.Width,r=-1,b=g.Height,t=-1;for(int y=0;y<g.Height;y++)for(int x=0;x<g.Width;x++)if(g.Alpha[y*g.Width+x]>=128){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
            if(r<l||t<b)return g;int width=r-l+1,height=t-b+1;var alpha=new byte[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)alpha[y*width+x]=g.Alpha[(b+y)*g.Width+l+x];return new SignDigitGlyph(width,height,alpha);
        }).ToArray();}
        public static SignDigitFont BuiltIn() {
            string[] patterns={"01110/11011/11011/11011/11011/11011/01110","00100/01100/00100/00100/00100/00100/01110",
                "01110/11011/00011/00110/01100/11000/11111","11110/00011/00011/01110/00011/00011/11110",
                "00011/00111/01111/11011/11111/00011/00011","11111/11000/11000/11110/00011/11011/01110",
                "00110/01100/11000/11110/11011/11011/01110","11111/00011/00110/00110/01100/01100/01100",
                "01110/11011/11011/01110/11011/11011/01110","01110/11011/11011/01111/00011/00110/01100"};
            return new SignDigitFont(patterns.Select(p=>{var rows=p.Split('/');var a=new byte[35];for(int y=0;y<7;y++)for(int x=0;x<5;x++)a[y*5+x]=(byte)(rows[6-y][x]=='1'?255:0);return new SignDigitGlyph(5,7,a);}).ToArray());
        }
        public int Height=>Digits.Max(d=>d.Height);
        public int Gap=>Math.Max(1,Height/12);
        public double Width(string text)=>text.Sum(c=>(double)Digits[c-'0'].Width)+Gap*(text.Length-1);
        public double Sample(string text,double x,double y) {
            double at=0;foreach(char c in text){var glyph=Digits[c-'0'];if(x>=at&&x<at+glyph.Width){
                double gx=x-at-.5,gy=y*glyph.Height/Height-.5;int ix=(int)Math.Floor(gx),iy=(int)Math.Floor(gy);double tx=gx-ix,ty=gy-iy;
                double Pixel(int px,int py)=>px<0||py<0||px>=glyph.Width||py>=glyph.Height?0:glyph.Alpha[py*glyph.Width+px]/255.0;
                return (Pixel(ix,iy)*(1-tx)+Pixel(ix+1,iy)*tx)*(1-ty)+(Pixel(ix,iy+1)*(1-tx)+Pixel(ix+1,iy+1)*tx)*ty;
            }at+=glyph.Width+Gap;}return 0;
        }
    }
    public sealed class SignFaceReading {
        public int Mph,Left,Bottom,Width,Height;
        public Color32 Background,Ink;
        public SignDigitFont Font;
    }
    public static class SignFaceConversion {
        const int CompareWidth=40,CompareHeight=32;
        sealed class Template {public int Number;public double Aspect;public bool[] Mask;public SignDigitFont Font;}
        static int Distance(Color32 a,Color32 b){int r=a.r-b.r,g=a.g-b.g,z=a.b-b.b;return r*r+g*g+z*z;}
        static Color32 Median(IEnumerable<Color32> colors) {
            var list=colors.ToArray();if(list.Length==0)return new Color32(0,0,0,255);
            byte Pick(Func<Color32,byte> f){var a=list.Select(f).OrderBy(v=>v).ToArray();return a[a.Length/2];}
            return new Color32(Pick(c=>c.r),Pick(c=>c.g),Pick(c=>c.b),255);
        }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SignDigitFont,Template[]> templateCache=new System.Runtime.CompilerServices.ConditionalWeakTable<SignDigitFont,Template[]>();
        static Template[] BuildTemplates(SignDigitFont font) {
            var result=new List<Template>();
            for(int n=1;n<=200;n++) {
                string text=n.ToString(System.Globalization.CultureInfo.InvariantCulture);double width=font.Width(text);var mask=new bool[CompareWidth*CompareHeight];
                for(int y=0;y<CompareHeight;y++)for(int x=0;x<CompareWidth;x++)mask[y*CompareWidth+x]=font.Sample(text,(x+.5)*width/CompareWidth,(y+.5)*font.Height/CompareHeight)>.4;
                result.Add(new Template{Number=n,Aspect=width/font.Height,Mask=mask,Font=font});
            }
            return result.ToArray();
        }
        static IEnumerable<Template> Templates(IEnumerable<SignDigitFont> fonts) {
            foreach(var font in fonts)foreach(var template in templateCache.GetValue(font,BuildTemplates))yield return template;
        }
        public static bool TryRead(Color32[] pixels,int size,IEnumerable<SignDigitFont> fonts,out SignFaceReading reading)=>TryRead(pixels,size,fonts,out reading,out _);
        public static bool TryRead(Color32[] pixels,int size,IEnumerable<SignDigitFont> fonts,out SignFaceReading reading,out string reason) {
            reason="Keine eindeutige Zahl im Schildfeld.";reading=null;if(size<32||size>512||pixels==null||pixels.Length!=size*size)return false;
            var edge=new List<Color32>();
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){double nx=2*(x+.5)/size-1,ny=2*(y+.5)/size-1,r=nx*nx+ny*ny;
                if(r>.36&&r<.62&&pixels[y*size+x].a>240)edge.Add(pixels[y*size+x]);}
            if(edge.Count<size)return false;var background=Median(edge);var contrasts=new List<int>();
            for(int y=size/4;y<size*3/4;y++)for(int x=size/8;x<size*7/8;x++)if(pixels[y*size+x].a>240)contrasts.Add(Distance(pixels[y*size+x],background));
            if(contrasts.Count==0)return false;contrasts.Sort();int strong=contrasts[(int)(contrasts.Count*.98)];if(strong<3600)return false;
            int threshold=Math.Max(2500,(int)(strong*.40));var mask=new bool[size*size];
            for(int y=size/4;y<size*3/4;y++)for(int x=size/8;x<size*7/8;x++)mask[y*size+x]=pixels[y*size+x].a>240&&Distance(pixels[y*size+x],background)>threshold;
            // Remove isolated wear and speckles; keep complete digit components.
            var visited=new bool[mask.Length];var clean=new bool[mask.Length];var queue=new Queue<int>();
            for(int i=0;i<mask.Length;i++)if(mask[i]&&!visited[i]) {
                var part=new List<int>();queue.Enqueue(i);visited[i]=true;
                while(queue.Count>0){int at=queue.Dequeue();part.Add(at);int x=at%size,y=at/size;
                    foreach(int n in new[]{x>0?at-1:-1,x+1<size?at+1:-1,y>0?at-size:-1,y+1<size?at+size:-1})
                        if(n>=0&&mask[n]&&!visited[n]){visited[n]=true;queue.Enqueue(n);}}
                if(part.Count>=Math.Max(5,size*size/1500))foreach(int at in part)clean[at]=true;
            }
            var inkIndices=Enumerable.Range(0,clean.Length).Where(i=>clean[i]).ToArray();if(inkIndices.Length<size*size/200)return false;
            int left=inkIndices.Min(i=>i%size),right=inkIndices.Max(i=>i%size),bottom=inkIndices.Min(i=>i/size),top=inkIndices.Max(i=>i/size);
            int width=right-left+1,height=top-bottom+1;if(height<size/10||height>size*.48||width>size*.76)return false;
            var normalized=new bool[CompareWidth*CompareHeight];
            for(int y=0;y<CompareHeight;y++)for(int x=0;x<CompareWidth;x++)normalized[y*CompareWidth+x]=clean[(bottom+Math.Min(height-1,(int)((y+.5)*height/CompareHeight)))*size+left+Math.Min(width-1,(int)((x+.5)*width/CompareWidth))];
            double aspect=(double)width/height;
            var scores=Templates(fonts).Select(t=>{
                int union=0,intersection=0;for(int i=0;i<normalized.Length;i++){if(normalized[i]||t.Mask[i])union++;if(normalized[i]&&t.Mask[i])intersection++;}
                double score=union==0?0:(double)intersection/union-.14*Math.Abs(Math.Log(aspect/t.Aspect));return new{Template=t,Score=score};
            }).GroupBy(s=>s.Template.Number).Select(g=>g.OrderByDescending(s=>s.Score).First()).OrderByDescending(s=>s.Score).Take(2).ToArray();
            if(scores.Length<2)return false;
            reason="Ziffernvergleich: "+scores[0].Template.Number+" ("+scores[0].Score.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+") / "+scores[1].Template.Number+" ("+scores[1].Score.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+").";
            string best=scores[0].Template.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),next=scores[1].Template.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            double margin=scores[0].Score-scores[1].Score;
            // A shared prefix must not dilute the evidence for the one differing digit.
            if(best.Length==next.Length){int differing=Enumerable.Range(0,best.Length).Count(i=>best[i]!=next[i]);margin*=best.Length/(double)differing;}
            if(scores[0].Score<.60||margin<.08)return false;
            reading=new SignFaceReading{Mph=scores[0].Template.Number,Left=left,Bottom=bottom,Width=width,Height=height,
                Background=background,Ink=Median(inkIndices.Select(i=>pixels[i])),Font=scores[0].Template.Font};return true;
        }
        public static Color32[] Convert(Color32[] original,int size,SignFaceReading reading,string mode,out bool[] changed) {
            var result=(Color32[])original.Clone();changed=new bool[original.Length];if(mode=="original")return result;
            string text=SignUnits.Caption(reading.Mph,mode);double width=reading.Font.Width(text),height=reading.Font.Height;
            int l=Math.Max(0,reading.Left-2),r=Math.Min(size-1,reading.Left+reading.Width+1),b=Math.Max(0,reading.Bottom-2),t=Math.Min(size-1,reading.Bottom+reading.Height+1);
            for(int y=b;y<=t;y++)for(int x=l;x<=r;x++) {
                int i=y*size+x;var color=original[i];double alpha=reading.Font.Sample(text,(x-reading.Left+.5)*width/reading.Width,(y-reading.Bottom+.5)*height/reading.Height);
                alpha=Math.Max(0,Math.Min(1,(alpha-.25)*2));
                // Inpaint old ink only; untouched paint, border and alpha remain exact.
                bool old=Distance(color,reading.Background)>1600;
                if(!old&&alpha<=.01)continue;
                var bg=old?reading.Background:color;
                byte Blend(byte a,byte z)=>(byte)Math.Max(0,Math.Min(255,Math.Round(a*(1-alpha)+z*alpha)));
                result[i]=new Color32(Blend(bg.r,reading.Ink.r),Blend(bg.g,reading.Ink.g),Blend(bg.b,reading.Ink.b),color.a);changed[i]=true;
            }
            return result;
        }
    }
}
