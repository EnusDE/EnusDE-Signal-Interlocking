using System;

namespace EnusDE.Signaltechnik {
    public static class ShaderPolicy {
        public static T FindLit<T>(Func<string,T> find) where T:class {
            foreach(string name in new[]{"HDRP/Lit","Universal Render Pipeline/Lit","Standard"}){var value=find(name);if(value!=null)return value;}return null;
        }
        static readonly string[] names={"Universal Render Pipeline/Unlit","HDRP/Unlit","Unlit/Color","Universal Render Pipeline/Lit","HDRP/Lit","Standard"};
        public static T Find<T>(Func<string,T> find) where T:class {
            foreach(string name in names){var value=find(name);if(value!=null)return value;}return null;
        }
    }
}
