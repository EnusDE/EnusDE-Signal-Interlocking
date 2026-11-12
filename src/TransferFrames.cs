using System;
using System.IO;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Extension framing below the game's unchanged MessagePack/GZip encoding.
    // One ordered reliable stream, one in-flight assembly; never exposes a partial snapshot.
    public static class TransferFrames {
        static readonly byte[] Magic={0x45,0x44,0x53,0x54,0x57,0,1,0};
        public const int Header=80,Payload=60*1024,MaxBytes=16*1024*1024;
        public static bool IsFrame(byte[] data){if(data==null||data.Length<Magic.Length)return false;for(int i=0;i<Magic.Length;i++)if(data[i]!=Magic[i])return false;return true;}
        public static byte[] Frame(byte[] message,Guid id,byte[] hash,int offset) {
            if(message==null||message.Length<1||message.Length>MaxBytes||offset<0||offset>=message.Length||offset%Payload!=0||hash==null||hash.Length!=32)throw new InvalidDataException("Ungültige Übertragungsgröße.");
            int length=Math.Min(Payload,message.Length-offset);var result=new byte[Header+length];Buffer.BlockCopy(Magic,0,result,0,8);Buffer.BlockCopy(id.ToByteArray(),0,result,8,16);
            Buffer.BlockCopy(BitConverter.GetBytes(message.Length),0,result,24,4);Buffer.BlockCopy(BitConverter.GetBytes(offset),0,result,28,4);Buffer.BlockCopy(hash,0,result,32,32);Buffer.BlockCopy(message,offset,result,Header,length);return result;
        }
        public static byte[] Digest(byte[] data){using(var sha=System.Security.Cryptography.SHA256.Create())return sha.ComputeHash(data);}
        public sealed class Receiver {
            byte[] buffer,hash;Guid id;int offset;double started;
            public int Buffered=>buffer?.Length??0;
            public void Clear(){buffer=null;hash=null;offset=0;}
            public bool Expire(double now){if(buffer==null||now>=started&&now-started<=120)return false;Clear();return true;}
            public byte[] Accept(byte[] part,double now) {
                try {
                    if(!IsFrame(part)||part.Length<=Header||part.Length>Header+Payload||double.IsNaN(now))throw new InvalidDataException("Ungültiger Signaltechnik-Übertragungsrahmen.");
                    for(int i=64;i<Header;i++)if(part[i]!=0)throw new InvalidDataException("Unbekannte Übertragungsoptionen.");
                    int total=BitConverter.ToInt32(part,24),at=BitConverter.ToInt32(part,28),length=part.Length-Header;var token=new byte[16];Buffer.BlockCopy(part,8,token,0,16);var next=new Guid(token);
                    if(total<1||total>MaxBytes||at<0||at%Payload!=0||at>total-length||length!=Math.Min(Payload,total-at))throw new InvalidDataException("Unzulässiger Signaltechnik-Übertragungsumfang.");
                    if(buffer==null){if(at!=0)throw new InvalidDataException("Übertragungsanfang fehlt.");buffer=new byte[total];hash=new byte[32];Buffer.BlockCopy(part,32,hash,0,32);id=next;started=now;}
                    if(now<started||now-started>120||next!=id||total!=buffer.Length||at!=offset)throw new InvalidDataException("Unterbrochene oder abgelaufene Signaltechnik-Übertragung.");
                    for(int i=0;i<32;i++)if(hash[i]!=part[32+i])throw new InvalidDataException("Uneinheitliche Übertragungs-Prüfsumme.");
                    Buffer.BlockCopy(part,Header,buffer,at,length);offset+=length;if(offset<total)return null;
                    var complete=buffer;var actual=Digest(complete);for(int i=0;i<32;i++)if(actual[i]!=hash[i])throw new InvalidDataException("Signaltechnik-Übertragungs-Prüfsumme stimmt nicht.");Clear();return complete;
                }catch{Clear();throw;}
            }
        }
        public enum SendResult {Sent,Retry,Failed}
        public sealed class Outbox {
            sealed class Item {public byte[] Data,Hash,Part;public Guid Id=Guid.NewGuid();public int Offset;public double Started;public bool Split;}
            readonly Queue<Item> queue=new Queue<Item>();
            public int Bytes {get;private set;}
            public int Count=>queue.Count;
            public void Clear(){queue.Clear();Bytes=0;}
            public void Add(byte[] data,double now,bool split) {if(data==null||data.Length<1||data.Length>MaxBytes||Bytes>MaxBytes-data.Length||queue.Count>=1024)throw new InvalidDataException("Signaltechnik-Sendewarteschlange voll.");queue.Enqueue(new Item{Data=data,Hash=split?Digest(data):null,Started=now,Split=split});Bytes+=data.Length;}
            public SendResult Pump(Func<byte[],SendResult> send,double now,int budget=4) {
                while(budget-->0&&queue.Count>0){var item=queue.Peek();if(now<item.Started||now-item.Started>120){Clear();return SendResult.Failed;}var part=item.Part??(item.Part=item.Split?Frame(item.Data,item.Id,item.Hash,item.Offset):item.Data);var result=send(part);if(result==SendResult.Retry)return result;if(result==SendResult.Failed){Clear();return result;}item.Part=null;item.Offset+=item.Split?part.Length-Header:part.Length;if(item.Offset==item.Data.Length){queue.Dequeue();Bytes-=item.Data.Length;}}
                return SendResult.Sent;
            }
        }
    }
}
