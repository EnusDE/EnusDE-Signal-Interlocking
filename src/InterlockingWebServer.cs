using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    // Socket work stays on a worker. The game thread only consumes bounded, authenticated requests.
    internal sealed class InterlockingWebServer : IDisposable {
        const int BodyLimit=2048;
        readonly HttpListener listener=new HttpListener();
        readonly ConcurrentQueue<Request> pending=new ConcurrentQueue<Request>();
        readonly Thread worker;
        readonly byte[] page;
        readonly string secret;
        readonly bool lan;
        volatile bool stopped;
        public string Address {get;}
        public string Link=>Address+"#"+secret;
        public bool Running=>!stopped&&listener.IsListening;

        sealed class Request {
            internal string Path,Data,Layout,View;
            internal readonly TaskCompletionSource<object> Response=new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            int state;
            internal bool Start()=>Interlocked.CompareExchange(ref state,1,0)==0;
            internal bool Cancel()=>Interlocked.CompareExchange(ref state,2,0)==0;
        }

        internal InterlockingWebServer(IPAddress address,int port,byte[] page) {
            lan=!IPAddress.IsLoopback(address);
            if(lan&&!PrivateAddress(address))throw new ArgumentException(nameof(address));
            this.page=page;
            Address="http://"+address+":"+port+"/";
            var random=new byte[32];
            using(var generator=RandomNumberGenerator.Create())generator.GetBytes(random);
            secret=Convert.ToBase64String(random).TrimEnd('=').Replace('+','-').Replace('/','_');
            listener.Prefixes.Add(Address);
            try{listener.Start();}catch{listener.Close();throw;}
            worker=new Thread(Listen){IsBackground=true,Name="EnusDE interlocking web"};
            worker.Start();
        }

        internal static bool PrivateAddress(IPAddress address) {
            if(address==null||address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork)return false;
            var bytes=address.GetAddressBytes();
            return bytes[0]==10||bytes[0]==192&&bytes[1]==168||bytes[0]==172&&bytes[1]>=16&&bytes[1]<=31;
        }

        static bool SameSecret(string candidate,string expected) {
            if(candidate==null||candidate.Length!=expected.Length)return false;
            int difference=0;
            for(int i=0;i<expected.Length;i++)difference|=candidate[i]^expected[i];
            return difference==0;
        }

        void Listen() {
            try {
                while(!stopped) {
                    var context=listener.GetContext();
                    try{Handle(context);}
                    catch(Exception){try{context.Response.Abort();}catch{}}
                }
            }catch(HttpListenerException){}catch(ObjectDisposedException){}
            finally{stopped=true;listener.Close();}
        }

        void Handle(HttpListenerContext context) {
            var request=context.Request;
            var response=context.Response;
            response.KeepAlive=false;
            response.Headers["Cache-Control"]="no-store";
            response.Headers["Referrer-Policy"]="no-referrer";
            response.Headers["X-Content-Type-Options"]="nosniff";
            response.Headers["Content-Security-Policy"]="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            using(response) {
                var peer=request.RemoteEndPoint?.Address;
                if(request.Url?.Authority!=new Uri(Address).Authority||
                    !(lan?PrivateAddress(peer):peer!=null&&IPAddress.IsLoopback(peer))) {Write(response,403);return;}
                string path=request.Url.AbsolutePath;
                if(request.HttpMethod=="GET"&&path=="/"){Write(response,200,page,"text/html; charset=utf-8");return;}
                if(!SameSecret(request.Headers["X-EnusDE-Key"],secret)){Write(response,403);return;}
                bool snapshot=request.HttpMethod=="GET"&&path=="/state";
                bool command=request.HttpMethod=="POST"&&path=="/action";
                if(!snapshot&&!command){Write(response,404);return;}
                string origin=request.Headers["Origin"];
                if((command||origin!=null)&&origin!=Address.TrimEnd('/')){Write(response,403);return;}
                string data=request.QueryString["desk"]??"";
                if(command) {
                    string media=request.ContentType?.Split(';')[0].Trim();
                    if(!string.Equals(media,"application/json",StringComparison.OrdinalIgnoreCase)||
                        request.ContentLength64<=0||request.ContentLength64>BodyLimit){Write(response,400);return;}
                    var body=new byte[(int)request.ContentLength64];
                    int offset=0;
                    var deadline=DateTime.UtcNow.AddSeconds(2);
                    while(offset<body.Length) {
                        var reading=request.InputStream.ReadAsync(body,offset,body.Length-offset);
                        var remaining=deadline-DateTime.UtcNow;
                        if(remaining<=TimeSpan.Zero||!reading.Wait(remaining)||reading.Result==0){Write(response,408);return;}
                        offset+=reading.Result;
                    }
                    data=Encoding.UTF8.GetString(body);
                }else if(data.Length>128||(request.QueryString["layout"]?.Length??0)>64||(request.QueryString["view"]?.Length??0)>32){Write(response,400);return;}
                if(pending.Count>=4){Write(response,503);return;}
                var item=new Request{Path=path,Data=data,Layout=snapshot?request.QueryString["layout"]:null,View=snapshot?request.QueryString["view"]:null};
                pending.Enqueue(item);
                if(!item.Response.Task.Wait(2000)) {
                    if(item.Cancel()){Write(response,503);return;}
                    // A command already accepted by the game must not look like a retryable rejection.
                    if(!item.Response.Task.Wait(4000)){Write(response,202);return;}
                }
                var result=item.Response.Task.Result;
                if(result==null){Write(response,503);return;}
                Write(response,200,Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)),"application/json; charset=utf-8");
            }
        }

        static void Write(HttpListenerResponse response,int status,byte[] bytes=null,string type=null) {
            response.StatusCode=status;
            response.ContentLength64=bytes?.Length??0;
            if(bytes!=null){response.ContentType=type;response.OutputStream.Write(bytes,0,bytes.Length);}
        }

        internal void Pump(Func<string,string,string,string,object> handle) {
            for(int i=0;i<4&&pending.TryDequeue(out var request);i++) {
                if(!request.Start())continue;
                object result=null;
                if(!stopped) {
                    try{result=handle(request.Path,request.Data,request.Layout,request.View);}
                    catch(Exception error){result=new {error=LocalText.Get(error.Message)};}
                }
                request.Response.TrySetResult(result);
                break;
            }
        }

        public void Dispose() {
            stopped=true;
            listener.Close();
            while(pending.TryDequeue(out var request)){request.Cancel();request.Response.TrySetResult(null);}
        }
    }
}
