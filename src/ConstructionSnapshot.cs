using System;
using System.Text;
using Newtonsoft.Json;
namespace EnusDE.Signaltechnik {
    // Train-number assignment and passage are live data, not construction edits.
    public static class ConstructionSnapshot {
        public static string Signature(InstallationDocument value)=>value==null?null:SharedDocuments.Hash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new{value.Protocol,value.WorldKey,value.Map,value.Objects,value.Economy,value.Placement,value.Retired})));
    }
}
