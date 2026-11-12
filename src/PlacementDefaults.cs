using System;
using System.Reflection;
using Game.Mow;

namespace EnusDE.Signaltechnik {
    public static class PlacementDefaults {
        public static double GameSideOffset {
            get {
                var sourceField=typeof(SignController).GetField("LinesideOffset",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
                if(sourceField==null||!sourceField.IsLiteral)throw new MissingFieldException("SignController.LinesideOffset");
                return Math.Round(Convert.ToDouble(sourceField.GetRawConstantValue(),System.Globalization.CultureInfo.InvariantCulture),2);
            }
        }
        public static MarkerOptions Resolve(MarkerOptions value) {
            return new MarkerOptions{Visible=value.Visible,Lateral=value.UseGameSpacing?GameSideOffset:Math.Abs(value.Lateral),Height=value.Height,Scale=value.Scale,DefaultRightSide=value.DefaultRightSide,FollowView=value.FollowView,UseGameSpacing=value.UseGameSpacing}.Normalize();
        }
    }
}
