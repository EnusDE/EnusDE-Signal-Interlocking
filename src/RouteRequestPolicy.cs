using System;
using System.Security.Cryptography;
using System.Text;

namespace EnusDE.Signaltechnik {
    public static class RouteRequestPolicy {
        // Identity describes the complete calculated protection, not merely the endpoints.
        public static RouteDefinition ForRequest(RouteDefinition definition) {
            if(definition?.Path==null||definition.Path.Count==0)throw new InvalidOperationException(LocalText.Select("A calculated route needs a complete directed path.","Ein berechneter Fahrweg benötigt einen vollständigen gerichteten Verlauf."));
            var copy=definition.Clone();
            byte[] bytes;using(var hash=SHA256.Create())bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(RouteIdentity.DefinitionKey(copy)));
            copy.Id=BitConverter.ToString(bytes,0,16).Replace("-","").ToLowerInvariant();
            copy.Code="AUTO-"+copy.Id.Substring(0,16);
            copy.Validate();return copy;
        }
        public static void RequireModeChange(bool active,bool blockHeld,bool lineHeld,bool pending) {
            if(active||blockHeld||lineHeld||pending)throw new InvalidOperationException(LocalText.Select(
                "Release routes, direction locks and pending contact requests before changing route mode.",
                "Vor dem Wechsel des Fahrwegmodus Fahrstraßen, Richtungsbindungen und wartende Kontaktanforderungen vollständig auflösen."));
        }
    }
}
