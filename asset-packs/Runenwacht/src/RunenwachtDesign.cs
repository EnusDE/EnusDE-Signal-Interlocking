using System;
using System.Collections.Generic;
using UnityEngine;
namespace EnusDE.Runenwacht {
    public sealed class RunenwachtHeadDesign {
        public RunenwachtGeometry Metal=new RunenwachtGeometry(),Reflector=new RunenwachtGeometry();
        public Dictionary<CrystalChannel,RunenwachtGeometry> Channels=new Dictionary<CrystalChannel,RunenwachtGeometry>();
        public RunenwachtGeometry Channel(CrystalChannel c){if(!Channels.TryGetValue(c,out var g))Channels[c]=g=new RunenwachtGeometry();return g;}
    }
    public static class RunenwachtDesign {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static RunenwachtGeometry Foundation(){var g=new RunenwachtGeometry();g.BevelBox(V(0,.17f,0),V(.85f,.34f,.85f),.045f);return g;}
        public static RunenwachtGeometry Mast(float height) {
            var g=new RunenwachtGeometry();g.BevelBox(V(0,.38f,.08f),V(.52f,.1f,.48f),.015f,1);
            for(int side=-1;side<=1;side+=2){g.BevelBox(V(side*.075f,height/2+.4f,.12f),V(.045f,height,.12f),.008f);
                for(float y=.58f;y<height+.3f;y+=.28f){g.Box(V(0,y,.16f),V(.13f,.025f,.045f),0,side*40);}
                var pipe=new[]{V(side*.21f,.42f,.13f),V(side*.21f,.58f,.13f),V(side*.24f,.66f,.13f),V(side*.24f,height+.12f,.13f),V(side*.2f,height+.2f,.13f),V(side*.2f,height+.24f,.13f)};g.Pipe(pipe,.022f,1,16);
                for(float y=.65f;y<height;y+=.5f){g.BevelBox(V(side*.24f,y,.13f),V(.072f,.04f,.06f),.008f,1);}
            }
            for(float y=.47f;y<height;y+=.55f){g.BevelBox(V(0,y,.11f),V(.23f,.1f,.2f),.012f,1);g.Ring(V(0,y,-.007f),.024f,0,.025f,0,8);}
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){g.Tube(V(x*.19f,.35f,z*.17f+.08f),V(x*.19f,.46f,z*.17f+.08f),.023f,0,6);}
            return g;
        }
        static void Bolt(RunenwachtGeometry g,float x,float y,float z,float radius=.018f){g.Ring(V(x,y,z+.008f),radius*1.35f,0,.012f,1,16);g.Ring(V(x,y,z-.004f),radius,0,.025f,0,6);g.Box(V(x,y,z-.02f),V(radius*.9f,.003f,.003f));}
        static void Insulators(RunenwachtGeometry g,float y,float scale=1) {
            foreach(int side in new[]{-1,1}){float x=side*.2f;g.Tube(V(x,y-.09f*scale,.13f),V(x,y+.06f,.13f),.045f,1,20);g.Tube(V(x,y-.44f*scale,.13f),V(x,y-.36f*scale,.13f),.05f,1,20);g.Tube(V(x,y-.42f*scale,.13f),V(x,y-.09f*scale,.13f),.045f,2,20);
                for(int i=0;i<5;i++)g.Tube(V(x,y+(-.4f+i*.055f)*scale,.13f),V(x,y+(-.37f+i*.055f)*scale,.13f),.069f,2,24);
                g.Pipe(new[]{V(x,y-.43f*scale,.13f),V(x,y-.5f*scale,.13f),V(side*.12f,y-.56f*scale,.13f),V(side*.12f,y-.68f*scale,.13f)},.023f,1,16);
            }
        }
        public static RunenwachtHeadDesign Head(string kind)=>Head(kind,false);
        public static RunenwachtHeadDesign Head(string kind,bool back) {
            var d=new RunenwachtHeadDesign();var g=d.Metal;
            if(kind=="main") {
                g.Ring(V(0,0,.135f),.565f,0,.16f,0,96);g.Ring(V(0,0,-.015f),.59f,.493f,.17f,0,96);g.Ring(V(0,0,-.112f),.583f,.539f,.045f,1,96);g.Ring(V(0,0,-.128f),.51f,.488f,.028f,1,96);
                d.Reflector.Ring(V(0,0,.041f),.49f,0,.018f,0,96);
                for(int i=0;i<36;i++){float a=i*Mathf.PI/18;Bolt(g,Mathf.Cos(a)*.556f,Mathf.Sin(a)*.556f,-.15f,.011f);}
                foreach(int angle in new[]{0,90,180,270}){float a=angle*Mathf.Deg2Rad;float x=Mathf.Cos(a)*.565f,y=Mathf.Sin(a)*.565f;g.BevelBox(V(x,y,-.14f),V(.13f,.13f,.085f),.018f,1);Bolt(g,x,y,-.19f,.025f);}
                // The red central crystal and two equal flanking cores form one centred cluster.
                foreach(var c in new[]{CrystalChannel.Red,CrystalChannel.Amber,CrystalChannel.Green}){float x=c==CrystalChannel.Red?0:c==CrystalChannel.Amber?-.21f:.21f;float h=c==CrystalChannel.Red?.78f:.62f;d.Channel(c).Gem(V(x,0,-.23f),.18f,h,.15f);g.BevelBox(V(x,-h/2-.035f,-.2f),V(.19f,.085f,.18f),.018f,1);Bolt(g,x,-h/2-.035f,-.3f,.012f);}
                for(int i=0;i<4;i++){float a=(45+i*90)*Mathf.Deg2Rad;float x=Mathf.Cos(a)*.415f,y=Mathf.Sin(a)*.415f;g.BevelBox(V(x,y,-.17f),V(.14f,.14f,.08f),.02f,1);var guides=d.Channel(CrystalChannel.Red);int first=guides.Vertices.Count;var centre=V(x,y,-.225f);guides.Gem(centre,.05f,.13f,.05f);guides.RotateZ(centre,first,45+i*90-90);}
                WhiteSocket(d,V(0,-.49f,-.34f),.11f,.14f,.06f);Insulators(g,-.57f);
            } else if(kind=="distant") {
                // Individual sealed spire backs replace the former rectangular box.
                float[] xs={-.29f,0,.29f},hs={.38f,.58f,.8f};
                for(int i=0;i<3;i++){float y=-.33f+hs[i]/2;g.SpireBack(V(xs[i],y,.035f),.255f,hs[i]+.1f,.13f);g.SpireBack(V(xs[i],y,-.04f),.225f,hs[i]+.065f,.018f,1);d.Reflector.SpireBack(V(xs[i],y,-.055f),.203f,hs[i]+.035f,.012f);d.Channel(i==2?CrystalChannel.Green:CrystalChannel.Amber).Gem(V(xs[i],y,-.155f),.17f,hs[i],.13f);g.BevelBox(V(xs[i],-.36f,-.11f),V(.225f,.1f,.19f),.018f,1);Bolt(g,xs[i],-.36f,-.21f,.014f);}
                g.Arc(V(0,.08f,-.16f),.65f,.555f,.115f,145,395,1,80);g.Arc(V(0,.08f,-.225f),.622f,.598f,.025f,145,395,0,80);
                for(int i=0;i<=20;i++){float a=(150+i*12)*Mathf.Deg2Rad;Bolt(g,Mathf.Cos(a)*.59f,.08f+Mathf.Sin(a)*.59f,-.245f,.009f);}
                foreach(int sign in new[]{-1,1}){float a=(sign<0?145:395)*Mathf.Deg2Rad;g.Ring(V(Mathf.Cos(a)*.6f,.08f+Mathf.Sin(a)*.6f,-.19f),.055f,0,.055f,1,24);}
                g.BevelBox(V(0,-.61f,-.1f),V(.2f,.15f,.26f),.02f,1);
                g.BevelBox(V(0,-.48f,-.21f),V(.86f,.16f,.2f),.025f,1);d.Channel(CrystalChannel.Red).Gem(V(0,-.48f,-.315f),.075f,.71f,.055f,true);Insulators(g,-.55f);
                WhiteSocket(d,V(-.46f,-.27f,-.27f),.09f,.12f,.045f);
            } else {
                g.BevelBox(V(0,0,.045f),V(.96f,.33f,.22f),.045f);g.BevelBox(V(0,0,-.08f),V(.95f,.31f,.065f),.025f,1);d.Reflector.BevelBox(V(0,0,-.12f),V(.76f,.23f,.018f),.02f);
                foreach(int sign in new[]{-1,1}){g.Ring(V(sign*.445f,0,-.045f),.18f,.105f,.26f,1,48);g.Ring(V(sign*.445f,0,-.19f),.17f,.13f,.04f,0,48);for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Bolt(g,sign*.445f+Mathf.Cos(a)*.145f,Mathf.Sin(a)*.145f,-.22f,.011f);}}
                d.Channel(CrystalChannel.Violet).Gem(V(0,.025f,-.225f),.14f,.67f,.12f,true);d.Channel(CrystalChannel.Red).Gem(V(0,-.1f,-.22f),.05f,.66f,.05f,true);WhiteSocket(d,V(.35f,.06f,-.29f),.075f,.1f,.045f);
                Insulators(g,-.19f,.68f);
            }
            g.BevelBox(V(0,-.15f,.27f),V(.14f,.3f,.42f),.025f,1);
            g.BevelBox(V(0,-.15f,back?.15f:.39f),V(.34f,.18f,.12f),.015f,0);
            return d;
        }
        static void WhiteSocket(RunenwachtHeadDesign design,Vector3 at,float width,float height,float depth) {
            design.Channel(CrystalChannel.White).Gem(at,width,height,depth);
            // A solid socket reaches the existing head frame and backs the enlarged white aperture.
            float front=at.z+depth/2+.005f,back=-.08f;
            design.Metal.BevelBox(V(at.x,at.y,(front+back)/2),V(width+.035f,height+.03f,back-front),.012f,0);
        }
        public static Vector3 FeedPort(string kind,int side)=>V(side*.12f,kind=="main"?-1.25f:kind=="distant"?-1.23f:-.19f-.68f*.68f,.13f);
        public static Vector3[] FeedRoute(string kind,bool back,int side) {
            var p=FeedPort(kind,side);float bus=back?.14f:.4f;
            return new[]{p,V(p.x,p.y-.045f,.13f),V(side*.17f,p.y-.075f,.16f),V(side*.24f,p.y-.075f,bus-.04f),V(side*.24f,p.y-.04f,bus),V(side*.24f,p.y+.12f,bus)};
        }
        public static RunenwachtGeometry FeedPipes(string kind,bool back) {
            var g=new RunenwachtGeometry();foreach(int side in new[]{-1,1}){var path=FeedRoute(kind,back,side);g.Pipe(path,.023f,1,16);var end=path[path.Length-1];g.BevelBox(end,V(.075f,.1f,.075f),.009f,1);g.Tube(path[0]-V(0,.015f,0),path[0]+V(0,.03f,0),.031f,1,16);}return g;
        }
        public static RunenwachtGeometry Plaque(float width,float height,float supportDepth=.54f) {
            var g=new RunenwachtGeometry();g.BevelBox(V(0,0,0),V(width,height,.035f),.009f,1);g.BevelBox(V(0,0,-.022f),V(width-.025f,height-.025f,.012f),.003f,2);
            g.BevelBox(V(0,0,.045f),V(.25f,.075f,.07f),.008f,1);
            foreach(int side in new[]{-1,1})g.BevelBox(V(side*.075f,0,(supportDepth+.055f)/2),V(.04f,.055f,supportDepth-.055f),.008f,0);
            g.BevelBox(V(0,0,supportDepth),V(.25f,.095f,.13f),.012f,1);
            foreach(int x in new[]{-1,1})Bolt(g,x*(width/2-.025f),0,-.038f,.008f);return g;
        }
    }
}
