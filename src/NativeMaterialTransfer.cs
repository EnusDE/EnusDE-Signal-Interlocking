using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        sealed class LegacyMaterialTransferReceipt {public int RemainingB,RemainingT;}
        public void TransferLegacyStock(string depotId,string warehouseId) {
            RequireReady();
            var source=installation.Economy.Depots.SingleOrDefault(d=>d.Id==depotId)??throw new InvalidOperationException(LocalText.Select("Legacy store is missing.","Altes Materiallager fehlt."));
            var target=MaterialWarehouses.SingleOrDefault(w=>w.Id==warehouseId)??throw new InvalidOperationException(LocalText.Select("Select an available MOW warehouse.","Ein verfügbares MOW-Lager auswählen."));
            string receiptKey="material-transfer."+SharedDocuments.Hash(Encoding.UTF8.GetBytes(WorldKey+":"+source.Id));
            string previousReceipt=Read(receiptKey);
            if(previousReceipt!=null) {
                var receipt=JsonConvert.DeserializeObject<LegacyMaterialTransferReceipt>(previousReceipt);
                if(receipt==null||receipt.RemainingB!=source.Stock.B||receipt.RemainingT!=source.Stock.T)throw new InvalidOperationException(LocalText.Select("Legacy stock differs from the recorded transfer. Restore a matching full game save before repeating a transfer.","Der alte Bestand passt nicht zur protokollierten Übernahme. Vor einer erneuten Übernahme einen passenden vollständigen Spielstand wiederherstellen."));
            }
            if(source.Stock.B==0&&source.Stock.T==0)return;
            float beforeB=target.Quantity(false),beforeT=target.Quantity(true);
            int transferB=NativeMaterialAmounts.FitTransfer(beforeB,source.Stock.B,target.Capacity(false)),transferT=NativeMaterialAmounts.FitTransfer(beforeT,source.Stock.T,target.Capacity(true));
            if(transferB==0&&transferT==0)throw new InvalidOperationException(LocalText.Select("The selected MOW warehouse is full. Remaining legacy stock is retained.","Das gewählte MOW-Lager ist voll. Der alte Restbestand bleibt erhalten."));
            float afterB=NativeMaterialAmounts.Transfer(beforeB,transferB,target.Capacity(false)),afterT=NativeMaterialAmounts.Transfer(beforeT,transferT,target.Capacity(true));
            var next=installation.Clone();var legacy=next.Economy.Depots.Single(d=>d.Id==depotId);legacy.Stock.B-=transferB;legacy.Stock.T-=transferT;
            Atomic(()=>{
                target.Set(false,afterB);target.Set(true,afterT);
                if(Math.Abs(target.Quantity(false)-afterB)>.001f||Math.Abs(target.Quantity(true)-afterT)>.001f)throw new InvalidOperationException(LocalText.Select("Native storage transfer was not confirmed.","Übernahme in das MOW-Lager wurde nicht bestätigt."));
                Write(receiptKey,JsonConvert.SerializeObject(new LegacyMaterialTransferReceipt{RemainingB=legacy.Stock.B,RemainingT=legacy.Stock.T}));CommitInstall(next);
            },()=>{target.Set(false,beforeB);target.Set(true,beforeT);Write(receiptKey,previousReceipt);});
        }
    }
}
