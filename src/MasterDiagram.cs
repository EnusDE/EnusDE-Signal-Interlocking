using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace EnusDE.Signaltechnik {
    public sealed class DeskModule {
        public string Desk,Master;
        public int X,Y;
        public DeskModule Clone()=>(DeskModule)MemberwiseClone();
    }
    // Modules refer to the local diagram. They never create another physical installation or authority.
    public static class MasterDiagram {
        public static string Owner(DeskLibrary library,DeskModule module)=>string.IsNullOrEmpty(module.Master)?library.Desks.Single(d=>d.Global).Id:module.Master;
        public static DeskModule At(DeskLibrary library,string master,int x,int y)=>library.Modules.FirstOrDefault(m=>Owner(library,m)==master&&
            new PanelRegion{X=m.X,Y=m.Y,Width=library.Desks.Single(d=>d.Id==m.Desk).Panel.Columns,Height=library.Desks.Single(d=>d.Id==m.Desk).Panel.Rows}.Contains(x,y));
        public static void Move(DeskLibrary library,string master,string local,int x,int y) {
            var proposed=library.Clone();var item=proposed.Modules.Single(m=>Owner(proposed,m)==master&&m.Desk==local);
            item.X=x;item.Y=y;Validate(proposed);
            var original=library.Modules.Single(m=>Owner(library,m)==master&&m.Desk==local);original.X=x;original.Y=y;
        }
        public static void Validate(DeskLibrary library) {
            if(library.Modules==null||library.Modules.Count>256||library.Modules.Any(m=>m==null)||
                library.Modules.Select(m=>Owner(library,m)+":"+m.Desk).Distinct().Count()!=library.Modules.Count)
                throw new InvalidDataException(LocalText.Select("Invalid or duplicate master modules.","Ungültige oder doppelte ESTW-Module."));
            foreach(var module in library.Modules)if(!library.Desks.Any(d=>d.IsMaster&&d.Id==Owner(library,module))||!library.Desks.Any(d=>!d.IsMaster&&d.Id==module.Desk))
                throw new InvalidDataException(LocalText.Select("Master module owner or local interlocking is missing.","Übergeordnetes oder örtliches Stellwerk des Moduls fehlt."));
            foreach(var master in library.Desks.Where(d=>d.IsMaster)) {
                var modules=library.Modules.Where(m=>Owner(library,m)==master.Id).ToArray();
                if(master.Panel.Tiles.Count+modules.Sum(m=>library.Desks.First(d=>d.Id==m.Desk).ShowInDispatcher?library.Desks.First(d=>d.Id==m.Desk).Panel.Tiles.Count:0)>65536||master.Panel.Areas.Count+modules.Length>128)
                    throw new InvalidDataException(LocalText.Select("Master diagram exceeds the field or area limit.","ESTW-Tafel überschreitet die Feld- oder Bereichsgrenze."));
                var boxes=new List<PanelRegion>();
                foreach(var module in modules) {
                    var local=library.Desks.Single(d=>!d.IsMaster&&d.Id==module.Desk);
                    var area=new PanelRegion{X=module.X,Y=module.Y,Width=local.Panel.Columns,Height=local.Panel.Rows};
                    if(module.X<0||module.Y<0||module.X+area.Width>PanelDocument.MaxColumns||module.Y+area.Height>PanelDocument.MaxRows||master.Panel.Tiles.Any(t=>area.Contains(t.X,t.Y))||boxes.Any(b=>b.X<area.X+area.Width&&area.X<b.X+b.Width&&b.Y<area.Y+area.Height&&area.Y<b.Y+b.Height))
                        throw new InvalidDataException(LocalText.Select("Master modules overlap or lie outside the diagram.","ESTW-Module überlappen oder liegen außerhalb der Tafel."));
                    boxes.Add(area);
                }
            }
        }
        public static PanelDocument Project(DeskLibrary library,string masterId=null,bool includeHidden=false) {
            Validate(library);var master=masterId==null?library.Desks.Single(d=>d.Global):library.Desks.Single(d=>d.IsMaster&&d.Id==masterId);var result=master.Panel.Clone();
            foreach(var module in library.Modules.Where(m=>Owner(library,m)==master.Id)) {
                var local=library.Desks.Single(d=>d.Id==module.Desk);
                if(!local.ShowInDispatcher&&!includeHidden)continue;
                result.Columns=Math.Max(result.Columns,module.X+local.Panel.Columns);result.Rows=Math.Max(result.Rows,module.Y+local.Panel.Rows);
                foreach(var tile in local.Panel.Tiles){var copy=tile.Clone();copy.X+=module.X;copy.Y+=module.Y;copy.NumberId=null;result.Tiles.Add(copy);}
                result.Areas.Add(new PanelArea{Id=local.Id,Code=local.Code,Name=local.Name,Kind="Stellwerk",Desk=local.Id,X=module.X,Y=module.Y,Width=local.Panel.Columns,Height=local.Panel.Rows});
            }
            return result;
        }
        public static DeskModule Suggest(DeskLibrary library,string id,string masterId=null) {
            var local=library.Desks.Single(d=>d.Id==id&&!d.IsMaster);
            var copy=library.Clone();var module=new DeskModule{Desk=id,Master=masterId};copy.Modules.Add(module);
            for(int y=0;y+local.Panel.Rows<=PanelDocument.MaxRows;y+=2)for(int x=0;x+local.Panel.Columns<=PanelDocument.MaxColumns;x+=2) {
                module.X=x;module.Y=y;try{Validate(copy);return module;}catch(InvalidDataException){}
            }
            throw new InvalidOperationException("Kein freier Platz für dieses Ortsstellwerk auf der ESTW-Tafel.");
        }
    }
}
