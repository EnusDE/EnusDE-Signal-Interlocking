using System;
using System.Linq;
using System.Reflection;

namespace EnusDE.Signaltechnik {
    // Resolve the path API once at startup. 0.6.3 moved UserPaths into HomeDirectory;
    // avoiding a static reference also allows the earlier library set to check the adapter.
    public static class RailroaderPaths {
        public static readonly string[] ApiNames={"Game.HomeDirectory.UserPaths","Game.HomeDirectory+UserPaths","Game.UserPaths"};
        static Type Find(string name)=>typeof(Game.State.StateManager).Assembly.GetType(name,false)??AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).FirstOrDefault(t=>t!=null);
        public static Type ApiType(Func<string,Type> find=null) {
            var resolve=find??Find;
            foreach(string name in ApiNames){var type=resolve(name);if(type!=null)return type;}
            throw new InvalidOperationException(LocalText.Select("Railroader's user-directory API is unavailable.","Die Benutzerdatenpfad-API von Railroader ist nicht verfügbar."));
        }
        public static string Directory(Func<string,Type> find=null) {
            var type=ApiType(find);var property=type.GetProperty("RailroaderDirectory",BindingFlags.Public|BindingFlags.Static);
            if(property?.PropertyType!=typeof(string)||property.GetGetMethod()==null||property.GetIndexParameters().Length!=0)
                throw new InvalidOperationException(LocalText.Select("Railroader's user-directory API has an unsupported signature: ","Die Benutzerdatenpfad-API von Railroader hat eine nicht unterstützte Signatur: ")+type.FullName);
            string path=(string)property.GetValue(null,null);
            if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException(LocalText.Select("Railroader returned an empty user-directory path.","Railroader hat einen leeren Benutzerdatenpfad zurückgegeben."));
            return path;
        }
    }
}
