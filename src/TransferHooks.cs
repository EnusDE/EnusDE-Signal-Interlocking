using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO.Compression;
using HarmonyLib;
using MessagePack;
using Network;
using Network.Steam;
using Steamworks;
using Railroader.Modding;
using UnityEngine;
using SteamClient=Network.Steam.SteamClient;

namespace EnusDE.Signaltechnik {
    // Loaded before joining a world: the initial snapshot arrives before OnWorldLoad.
    public sealed class TransferHooks : IDisposable {
        const string PatchId="enusde.signaltechnik.large-transfer";
        static readonly Dictionary<uint,TransferFrames.Outbox> outgoing=new Dictionary<uint,TransferFrames.Outbox>();
        static ConditionalWeakTable<SteamClient,TransferFrames.Receiver> incoming=new ConditionalWeakTable<SteamClient,TransferFrames.Receiver>();
        static WeakReference<SteamClient> receivingClient;
        static WeakReference<TransferFrames.Receiver> receivingBuffer;
        static bool releaseFragments=true;
        static IModLogger log;Harmony harmony;GameObject root;
        static readonly Type recipient=typeof(SendContext).GetNestedType("Recipient",BindingFlags.Public|BindingFlags.NonPublic);
        static readonly FieldInfo connectionField=recipient.GetField("Connection"),resultField=recipient.GetField("Result");
        static readonly FieldInfo clientConnection=typeof(SteamClient).GetField("_connection",BindingFlags.Instance|BindingFlags.NonPublic);
        public void Start(IModLogger logger) {
            Dispose();log=logger;harmony=new Harmony(PatchId);
            try {
                harmony.Patch(typeof(SendContext).GetMethod("SendToRecipients",BindingFlags.Static|BindingFlags.NonPublic),prefix:new HarmonyMethod(typeof(TransferHooks),nameof(Send)));
                harmony.Patch(typeof(SteamClient).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic),transpiler:new HarmonyMethod(typeof(TransferHooks),nameof(ClientDecoder)));
                harmony.Patch(typeof(SteamClient).GetMethod("Disconnect"),prefix:new HarmonyMethod(typeof(TransferHooks),nameof(Disconnect)));
                root=new GameObject("EnusDE Signaltechnik transfer");UnityEngine.Object.DontDestroyOnLoad(root);root.AddComponent<TransferPump>();
                log.Info("Signaltechnik MP: begrenzte Großnachrichtenübertragung vor Weltbeitritt bereit.");
            }catch{Dispose();throw;}
        }
        static bool Send(IList __0,SendMode __1,byte[] __2,int __3) {
            bool large=__3>Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend;
            bool ownHost=Game.State.StateManager.IsHost&&Entry.Shared!=null&&Entry.Shared.Ready;
            if((!large||!ownHost)&&!__0.Cast<object>().Any(r=>outgoing.ContainsKey(((HSteamNetConnection)connectionField.GetValue(r)).m_HSteamNetConnection)))return true;
            byte[] copy=null;
            for(int i=0;i<__0.Count;i++) {
                object row=__0[i];var connection=(HSteamNetConnection)connectionField.GetValue(row);uint key=connection.m_HSteamNetConnection;EResult result;
                TransferFrames.Outbox queue;
                if(__1==SendMode.Reliable&&(large&&ownHost||outgoing.TryGetValue(key,out queue))) {
                    try {
                        if(!outgoing.TryGetValue(key,out queue)){if(outgoing.Count>=8)throw new InvalidDataException("Zu viele gleichzeitige Großübertragungen.");outgoing.Add(key,queue=new TransferFrames.Outbox());}
                        if(__3<1||__3>TransferFrames.MaxBytes||outgoing.Values.Sum(q=>(long)q.Bytes)+__3>64L*1024*1024)throw new InvalidDataException("Signaltechnik-Übertragung überschreitet Speichergrenze.");
                        if(copy==null){copy=new byte[__3];Buffer.BlockCopy(__2,0,copy,0,__3);}queue.Add(copy,Time.realtimeSinceStartup,large);result=EResult.k_EResultOK;
                        if(large)log?.Info("Signaltechnik MP: "+__3+" Byte in "+((__3+TransferFrames.Payload-1)/TransferFrames.Payload)+" geordnete Teilnachrichten aufgeteilt.");
                    }catch(Exception e){outgoing.Remove(key);log?.Warning("Signaltechnik MP-Senden: "+e.Message);result=EResult.k_EResultLimitExceeded;}
                }else result=RawSend(connection,__2,__3,__1==SendMode.Reliable?8:0);
                resultField.SetValue(row,result);__0[i]=row;
            }
            return false;
        }
        static EResult RawSend(HSteamNetConnection connection,byte[] bytes,int count,int flags) {
            var pin=GCHandle.Alloc(bytes,GCHandleType.Pinned);try{long number;return SteamNetworkingSockets.SendMessageToConnection(connection,pin.AddrOfPinnedObject(),(uint)count,flags,out number);}catch{return EResult.k_EResultUnexpectedError;}finally{pin.Free();}
        }
        internal static void Pump() {
            TransferFrames.Receiver pending;if(receivingBuffer!=null&&receivingBuffer.TryGetTarget(out pending)&&pending.Expire(Time.realtimeSinceStartup)){log?.Warning("Signaltechnik MP: unvollständiger Empfang nach 120 Sekunden verworfen.");SteamClient client;if(receivingClient!=null&&receivingClient.TryGetTarget(out client)&&client!=null)client.Disconnect();}
            foreach(var pair in outgoing.ToArray()) {
                var connection=new HSteamNetConnection(pair.Key);
                var result=pair.Value.Pump(bytes=>{var code=RawSend(connection,bytes,bytes.Length,8);return code==EResult.k_EResultOK?TransferFrames.SendResult.Sent:code==EResult.k_EResultLimitExceeded?TransferFrames.SendResult.Retry:TransferFrames.SendResult.Failed;},Time.realtimeSinceStartup);
                if(result==TransferFrames.SendResult.Failed){outgoing.Remove(pair.Key);log?.Warning("Signaltechnik MP: Großübertragung abgebrochen; Verbindung getrennt.");try{SteamNetworkingSockets.CloseConnection(connection,4901,"Signaltechnik transfer failed",false);}catch{}}
                else if(pair.Value.Count==0)outgoing.Remove(pair.Key);
            }
        }
        static IEnumerable<CodeInstruction> ClientDecoder(IEnumerable<CodeInstruction> source) {
            var rows=source.ToList();var release=typeof(SteamNetworkingMessage_t).GetMethod("Release",new[]{typeof(IntPtr)});releaseFragments=!rows.Any(i=>i.Calls(release));
            var original=typeof(ReceiveContext).GetMethod("NetworkMessageFromPointer");var replacement=typeof(TransferHooks).GetMethod(nameof(Decode),BindingFlags.Static|BindingFlags.NonPublic);int found=0;
            foreach(var instruction in rows) {
                if(instruction.Calls(original)){var client=new CodeInstruction(OpCodes.Ldarg_0);client.labels.AddRange(instruction.labels);instruction.labels.Clear();client.blocks.AddRange(instruction.blocks);instruction.blocks.Clear();yield return client;instruction.opcode=OpCodes.Call;instruction.operand=replacement;found++;}
                yield return instruction;
            }
            if(found!=1)throw new InvalidOperationException("Unerwarteter nativer MP-Empfangsweg: "+found+" Decoderaufrufe.");
        }
        static INetworkMessage Decode(ReceiveContext context,IntPtr pointer,out HSteamNetConnection connection,SteamClient client) {
            var raw=Marshal.PtrToStructure<SteamNetworkingMessage_t>(pointer);connection=raw.m_conn;
            if(raw.m_cbSize<8)return context.NetworkMessageFromPointer(pointer,out connection);
            var prefix=new byte[8];Marshal.Copy(raw.m_pData,prefix,0,8);if(!TransferFrames.IsFrame(prefix))return context.NetworkMessageFromPointer(pointer,out connection);
            try {
                if(raw.m_conn!=(HSteamNetConnection)clientConnection.GetValue(client)||raw.m_cbSize>TransferFrames.Header+TransferFrames.Payload)throw new InvalidDataException("Ungültiger MP-Absender oder Rahmenumfang.");
                var data=new byte[raw.m_cbSize];Marshal.Copy(raw.m_pData,data,0,data.Length);var receiver=incoming.GetOrCreateValue(client);receivingClient=new WeakReference<SteamClient>(client);receivingBuffer=new WeakReference<TransferFrames.Receiver>(receiver);var complete=receiver.Accept(data,Time.realtimeSinceStartup);
                // Unhandled native envelope: no status/game message and no partial data reach the game.
                if(complete==null)return new Network.Messages.NetworkMessageEnvelope(0,0,new ArraySegment<byte>(new byte[0]));
                var result=DecodeMessage(complete);log?.Info("Signaltechnik MP: Großnachricht vollständig empfangen ("+complete.Length+" Byte).");return result;
            }catch(Exception e){incoming.Remove(client);log?.Warning("Signaltechnik MP-Empfang: "+e.Message);client.Disconnect();throw;}
            finally{if(releaseFragments)SteamNetworkingMessage_t.Release(pointer);}
        }
        public static INetworkMessage DecodeMessage(byte[] data) {
            var result=MessagePackSerializer.Deserialize<INetworkMessage>(data,Game.Messages.MessagepackSupport.UntrustedOptions);
            if(result is Network.Messages.NetworkMessageEnvelope envelope) {
                if(envelope.Flags0!=1||envelope.Flags1!=0)throw new InvalidDataException("Unbekannte native Nachrichtenhülle.");
                using(var input=new MemoryStream(envelope.Data.Array,envelope.Data.Offset,envelope.Data.Count))using(var gzip=new GZipStream(input,CompressionMode.Decompress))using(var output=new MemoryStream()) {
                    var buffer=new byte[8192];int n;while((n=gzip.Read(buffer,0,buffer.Length))>0){if(output.Length+n>64*1024*1024)throw new InvalidDataException("Entpackte Nachricht überschreitet Spielgrenze.");output.Write(buffer,0,n);}result=MessagePackSerializer.Deserialize<INetworkMessage>(output.ToArray(),Game.Messages.MessagepackSupport.UntrustedOptions);
                }
            }
            return result;
        }
        static void Disconnect(SteamClient __instance){incoming.Remove(__instance);}
        public void Dispose(){if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}if(root!=null)UnityEngine.Object.Destroy(root);root=null;outgoing.Clear();incoming=new ConditionalWeakTable<SteamClient,TransferFrames.Receiver>();receivingClient=null;receivingBuffer=null;log=null;}
    }
    public sealed class TransferPump : MonoBehaviour {void Update(){TransferHooks.Pump();}}
}
