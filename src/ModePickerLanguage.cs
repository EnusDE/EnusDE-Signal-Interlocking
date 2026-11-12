using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Read the active EnusDE Translation preference. English is the local fallback.
    sealed class ModePickerLanguage {
        const string BehaviourName="EnusDE.GermanUi.GermanUiBehaviour";
        MonoBehaviour source;
        FieldInfo settingsField, stoppedField, languageField, interfaceField;
        Type behaviourType, settingsType;
        bool resolved;
        double nextDiscovery=-1;

        public bool ReadGerman() {
            try {
                if(!resolved) {
                    resolved=true;
                    behaviourType=AccessTools.TypeByName(BehaviourName);
                    if(behaviourType==null||!typeof(MonoBehaviour).IsAssignableFrom(behaviourType))return false;
                    settingsField=AccessTools.Field(behaviourType,"settings");
                    stoppedField=AccessTools.Field(behaviourType,"stopped");
                }
                if(settingsField==null||stoppedField?.FieldType!=typeof(bool))return false;
                if(!Active(source)) {
                    source=null;
                    double now=Time.realtimeSinceStartup;
                    if(now<nextDiscovery)return false;
                    nextDiscovery=now+5;
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType(behaviourType,FindObjectsSortMode.None)) {
                        var behaviour=candidate as MonoBehaviour;
                        if(!Active(behaviour))continue;
                        if(source!=null){source=null;return false;}
                        source=behaviour;
                    }
                }
                if(source==null)return false;
                var settings=settingsField.GetValue(source);
                if(settings==null)return false;
                if(settings.GetType()!=settingsType) {
                    settingsType=settings.GetType();
                    languageField=AccessTools.Field(settingsType,"language");
                    interfaceField=AccessTools.Field(settingsType,"translateInterface");
                }
                return languageField?.FieldType==typeof(string)&&interfaceField?.FieldType==typeof(bool)
                    &&ModePickerText.UsesGerman((string)languageField.GetValue(settings),(bool)interfaceField.GetValue(settings));
            }catch(Exception) {
                source=null;
                return false;
            }
        }

        bool Active(MonoBehaviour value)=>value!=null&&value.isActiveAndEnabled&&!(bool)stoppedField.GetValue(value);
    }
}
