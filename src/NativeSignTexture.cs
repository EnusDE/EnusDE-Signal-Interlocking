using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace EnusDE.Signaltechnik {
    public static class NativeSignTexture {
        public const int MaximumPixels=4096*4096;
        public static Color32[] Read(Texture2D texture) {
            if(texture==null||texture.width<1||texture.height<1||(long)texture.width*texture.height>MaximumPixels)
                throw new InvalidOperationException("Schildtextur nicht verfügbar oder zu groß.");
            if(texture.isReadable)return texture.GetPixels32();
            // Runtime GPU readback also supports imported, non-readable textures. Never change import settings.
            var previous=RenderTexture.active;bool previousSrgb=GL.sRGBWrite;RenderTexture target=null;Texture2D copy=null;
            try {
                target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,texture.isDataSRGB?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
                GL.sRGBWrite=texture.isDataSRGB;Graphics.Blit(texture,target);RenderTexture.active=target;
                copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,!texture.isDataSRGB);
                copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0,false);copy.Apply(false,false);return copy.GetPixels32();
            } finally {GL.sRGBWrite=previousSrgb;RenderTexture.active=previous;if(target!=null)RenderTexture.ReleaseTemporary(target);if(copy!=null)UnityEngine.Object.Destroy(copy);}
        }
        public static SignDigitFont Font() {
            var font=TMP_Settings.defaultFontAsset;if(font==null)return SignDigitFont.BuiltIn();
            var digits=new SignDigitGlyph[10];var atlases=new Dictionary<int,Color32[]>();
            for(int i=0;i<10;i++) {
                if(!font.characterLookupTable.TryGetValue((uint)('0'+i),out var character))return SignDigitFont.BuiltIn();
                var glyph=character.glyph;var rect=glyph.glyphRect;var texture=font.atlasTextures[glyph.atlasIndex];
                if(!atlases.TryGetValue(glyph.atlasIndex,out var pixels)){pixels=Read(texture);atlases[glyph.atlasIndex]=pixels;}
                int l=rect.width,r=-1,b=rect.height,t=-1;
                for(int y=0;y<rect.height;y++)for(int x=0;x<rect.width;x++)if(pixels[(rect.y+y)*texture.width+rect.x+x].a>=128){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
                if(r<l||t<b)return SignDigitFont.BuiltIn();
                int width=r-l+1,height=t-b+1;var alpha=new byte[width*height];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
                    int a=pixels[(rect.y+b+y)*texture.width+rect.x+l+x].a;
                    alpha[y*width+x]=(byte)Math.Max(0,Math.Min(255,(a-112)*8));
                }
                digits[i]=new SignDigitGlyph(width,height,alpha);
            }
            return new SignDigitFont(digits);
        }
        static double Half(ushort bits) {
            int exponent=(bits>>10)&31,mantissa=bits&1023;double value=exponent==0?mantissa*Math.Pow(2,-24):
                exponent==31?double.NaN:(1+mantissa/1024.0)*Math.Pow(2,exponent-15);return (bits&32768)==0?value:-value;
        }
        static double Component(byte[] bytes,int at,VertexAttributeFormat format)=>format==VertexAttributeFormat.Float32?
            BitConverter.ToSingle(bytes,at):format==VertexAttributeFormat.Float16?Half(BitConverter.ToUInt16(bytes,at)):
            throw new InvalidOperationException("Unbekanntes Schild-Vertexformat.");
        public static SignTextureVertex[] Vertices(Mesh mesh,Vector2 scale,Vector2 offset,out int[] indices) {
            if(mesh.isReadable) {
                var positions=mesh.vertices;var uv=mesh.uv;indices=mesh.triangles;
                if(uv.Length!=positions.Length)throw new InvalidOperationException("Schild ohne eindeutige Texturkoordinaten.");
                var result=new SignTextureVertex[positions.Length];
                for(int i=0;i<result.Length;i++)result[i]=new SignTextureVertex(positions[i].x,positions[i].y,positions[i].z,uv[i].x*scale.x+offset.x,uv[i].y*scale.y+offset.y);
                return result;
            }
            // Imported meshes can discard their CPU arrays. Read their existing GPU buffers without changing the mesh.
            var buffers=new Dictionary<int,byte[]>();
            double Value(int vertex,VertexAttribute attribute,int component) {
                if(!mesh.HasVertexAttribute(attribute)||mesh.GetVertexAttributeDimension(attribute)<=component)throw new InvalidOperationException("Unvollständiges Schildmesh.");
                int stream=mesh.GetVertexAttributeStream(attribute),stride=mesh.GetVertexBufferStride(stream);
                var format=mesh.GetVertexAttributeFormat(attribute);int step=format==VertexAttributeFormat.Float32?4:format==VertexAttributeFormat.Float16?2:0;
                if(step==0)throw new InvalidOperationException("Unbekanntes Schild-Vertexformat.");
                if(!buffers.TryGetValue(stream,out var bytes)) {
                    using(var buffer=mesh.GetVertexBuffer(stream)){bytes=new byte[checked(buffer.count*buffer.stride)];if(bytes.Length>8*1024*1024)throw new InvalidOperationException("Schildmesh zu groß.");buffer.GetData(bytes);}
                    buffers[stream]=bytes;
                }
                return Component(bytes,vertex*stride+mesh.GetVertexAttributeOffset(attribute)+component*step,format);
            }
            var vertices=new SignTextureVertex[mesh.vertexCount];
            for(int i=0;i<vertices.Length;i++)vertices[i]=new SignTextureVertex(Value(i,VertexAttribute.Position,0),Value(i,VertexAttribute.Position,1),Value(i,VertexAttribute.Position,2),
                Value(i,VertexAttribute.TexCoord0,0)*scale.x+offset.x,Value(i,VertexAttribute.TexCoord0,1)*scale.y+offset.y);
            byte[] data;using(var buffer=mesh.GetIndexBuffer()){data=new byte[checked(buffer.count*buffer.stride)];if(data.Length>8*1024*1024)throw new InvalidOperationException("Schildmesh zu groß.");buffer.GetData(data);}
            int indexSize=mesh.indexFormat==IndexFormat.UInt16?2:4,start=(int)mesh.GetIndexStart(0),count=(int)mesh.GetIndexCount(0),basis=(int)mesh.GetBaseVertex(0);indices=new int[count];
            for(int i=0;i<count;i++){int at=(start+i)*indexSize;indices[i]=(indexSize==2?BitConverter.ToUInt16(data,at):checked((int)BitConverter.ToUInt32(data,at)))+basis;}
            return vertices;
        }
    }
}
