using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO.Compression;
using EnusDE.Signaltechnik;
using MessagePack;
using HarmonyLib;

public static class TransferTests {
    static void Check(bool value,string message){if(!value)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}}
    public static void Run() {
        var bytes=new byte[1400000];new Random(42).NextBytes(bytes);var hash=TransferFrames.Digest(bytes);var id=Guid.NewGuid();var parts=Enumerable.Range(0,(bytes.Length+TransferFrames.Payload-1)/TransferFrames.Payload).Select(i=>TransferFrames.Frame(bytes,id,hash,i*TransferFrames.Payload)).ToArray();
        Check(parts.All(p=>p.Length<Steamworks.Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend),"large snapshot fragments remain below the installed Steam per-message limit");
        var receiver=new TransferFrames.Receiver();byte[] result=null;bool partial=true;for(int i=0;i<parts.Length;i++){result=receiver.Accept(parts[i],i*.01);if(i<parts.Length-1)partial&=result==null;}
        Check(partial&&result.SequenceEqual(bytes)&&receiver.Buffered==0,"1.4 MB reliable snapshot is exposed exactly once after full hash-verified assembly and releases its buffer");
        Check(Reject(()=>receiver.Accept(parts[1],1))&&receiver.Buffered==0,"missing first frame cannot allocate or expose a partial snapshot");receiver.Accept(parts[0],1);Check(Reject(()=>receiver.Accept(parts[0],2))&&receiver.Buffered==0,"duplicate frame rejects and clears incomplete assembly");receiver.Accept(parts[0],1);Check(Reject(()=>receiver.Accept(parts[2],2)),"out-of-order reliable fragments fail closed");
        receiver.Accept(parts[0],1);Check(Reject(()=>receiver.Accept(parts[1],122))&&receiver.Buffered==0,"expired partial transfer releases all retained bytes");
        receiver.Accept(parts[0],1);Check(!receiver.Expire(120)&&receiver.Expire(122)&&receiver.Buffered==0,"idle receive timeout frees an incomplete buffer even without another incoming frame");
        var wrong=(byte[])parts[0].Clone();Buffer.BlockCopy(BitConverter.GetBytes(TransferFrames.MaxBytes+1),0,wrong,24,4);Check(Reject(()=>receiver.Accept(wrong,1))&&receiver.Buffered==0,"oversized declarations reject before receive-buffer allocation");
        receiver.Accept(parts[0],1);var foreign=TransferFrames.Frame(bytes,Guid.NewGuid(),hash,TransferFrames.Payload);Check(Reject(()=>receiver.Accept(foreign,2)),"interleaved transfer identities cannot mix snapshots");
        parts[parts.Length-1][TransferFrames.Header]^=1;Check(Reject(()=>{foreach(var part in parts)receiver.Accept(part,1);})&&receiver.Buffered==0,"corrupted final payload cannot reach native deserialization");parts[parts.Length-1][TransferFrames.Header]^=1;
        var queue=new TransferFrames.Outbox();queue.Add(bytes,1,true);var after=new byte[]{1,2,3};queue.Add(after,1,false);int attempts=0;var delivered=new List<byte[]>();
        for(int i=0;queue.Count>0&&i<100;i++)queue.Pump(p=>{if(++attempts%3==0)return TransferFrames.SendResult.Retry;delivered.Add(p);return TransferFrames.SendResult.Sent;},1+i*.01,2);
        Check(queue.Count==0&&queue.Bytes==0&&delivered.Last().SequenceEqual(after)&&delivered.Count==parts.Length+1,"Steam send-buffer backpressure retries without dropping fragments or overtaking the snapshot with a later reliable message");
        queue.Add(bytes,1,true);Check(queue.Pump(p=>TransferFrames.SendResult.Retry,122)==TransferFrames.SendResult.Failed&&queue.Bytes==0,"stalled send queue expires and releases retained snapshots");queue.Add(new byte[TransferFrames.MaxBytes],1,true);Check(Reject(()=>queue.Add(new byte[1],1,false)),"per-connection send storage has a hard 16 MB bound");queue.Clear();
        Game.Messages.MessagepackSupport.Setup();var time=new Network.Messages.TimeSync();var native=MessagePackSerializer.Serialize<Network.INetworkMessage>(time);Check(TransferHooks.DecodeMessage(native) is Network.Messages.TimeSync,"reassembled bytes use the installed native MessagePack network schema");
        var snapshot=Game.Messages.Snapshot.Empty();snapshot.Properties[SharedRuntime.ObjectId]=new Dictionary<string,Game.Messages.IPropertyValue>{{"desks.0.0",new Game.Messages.StringPropertyValue(Convert.ToBase64String(bytes))}};
        var snapshotBytes=MessagePackSerializer.Serialize<Network.INetworkMessage>(new Network.Messages.SnapshotEnvelope(snapshot));byte[] compressed;using(var output=new MemoryStream()){using(var zip=new GZipStream(output,CompressionLevel.Optimal,true))zip.Write(snapshotBytes);compressed=output.ToArray();}
        var wire=MessagePackSerializer.Serialize<Network.INetworkMessage>(new Network.Messages.NetworkMessageEnvelope(1,0,new ArraySegment<byte>(compressed)));hash=TransferFrames.Digest(wire);id=Guid.NewGuid();result=null;for(int offset=0;offset<wire.Length;offset+=TransferFrames.Payload)result=receiver.Accept(TransferFrames.Frame(wire,id,hash,offset),1);
        var recovered=(Network.Messages.SnapshotEnvelope)TransferHooks.DecodeMessage(result);Check(wire.Length>Steamworks.Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend&&MessagePackSerializer.Serialize(snapshot).SequenceEqual(MessagePackSerializer.Serialize(recovered.Snapshot)),"oversized compressed native SnapshotEnvelope roundtrips through production framing, hash verification and native untrusted deserialization without changing any snapshot fields");
        var source=typeof(Network.Steam.SteamClient).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);var il=Instructions(source);
        var decoder=typeof(Network.Steam.ReceiveContext).GetMethod("NetworkMessageFromPointer");var release=typeof(Steamworks.SteamNetworkingMessage_t).GetMethod("Release",new[]{typeof(IntPtr)});
        Check(il.Count(i=>i.Calls(decoder))==1&&!il.Any(i=>i.Calls(release)),"installed native client has exactly one decoder call and leaves input-pointer ownership to the extension");
        var transform=typeof(TransferHooks).GetMethod("ClientDecoder",BindingFlags.Static|BindingFlags.NonPublic);var patched=((IEnumerable<CodeInstruction>)transform.Invoke(null,new object[]{il})).ToList();Check(!patched.Any(i=>i.Calls(decoder))&&patched.Count(i=>i.operand is MethodInfo m&&m.Name=="Decode")==1,"actual native client IL receives one wrapper before world load while preserving the original dispatch path");
        var send=typeof(Network.Steam.SendContext).GetMethod("SendToRecipients",BindingFlags.Static|BindingFlags.NonPublic);Check(send!=null&&send.GetParameters().Length==4&&typeof(System.Collections.IList).IsAssignableFrom(send.GetParameters()[0].ParameterType)&&send.GetParameters()[2].ParameterType==typeof(byte[])&&send.GetParameters()[3].ParameterType==typeof(int),"installed native sender exposes the exact validated encoded-message hook");
        Check(Network.Multiplayer.SendModeForMessage(new Network.Messages.SnapshotEnvelope(Game.Messages.Snapshot.Empty()))==Network.SendMode.Reliable,"native initial snapshot uses ordered reliable transport");
        Check(Network.Multiplayer.SendModeForMessage((Game.Messages.IGameMessage)new Game.Messages.PropertyChange(),false)==Network.SendMode.Reliable,"native shared-property changes remain reliable and follow a queued snapshot in order");
    }
    static List<CodeInstruction> Instructions(MethodInfo method) {
        var table=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);var data=method.GetMethodBody().GetILAsByteArray();var result=new List<CodeInstruction>();
        for(int i=0;i<data.Length;){ushort value=data[i++];if(value==0xfe)value=(ushort)(0xfe00|data[i++]);var op=table[value];int size=op.OperandType switch{OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(data,i),_=>4};object operand=size==0?null:BitConverter.ToString(data,i,size);if(op.OperandType==OperandType.InlineMethod)operand=method.Module.ResolveMethod(BitConverter.ToInt32(data,i));result.Add(new CodeInstruction(op,operand));i+=size;}return result;
    }
}
