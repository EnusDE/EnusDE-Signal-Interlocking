using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Read the active EnusDE Translation preference. English is the local fallback.
    sealed class ModePickerLanguage {
        const string BehaviourName="EnusDE.GermanUi.GermanUiBehaviour";
        MonoBehaviour source;
        FieldInfo settingsField, stoppedField, languageField, interfaceField,translatorField;
        MethodInfo translate;
        Type behaviourType, settingsType;
        bool resolved;
        double nextDiscovery=-1;

        public bool ReadGerman() {
            LocalText.AutomaticLanguage=ReadTranslationLanguage();
            return LocalText.German;
        }
        string ReadTranslationLanguage() {
            try {
                if(!resolved) {
                    resolved=true;
                    behaviourType=AccessTools.TypeByName(BehaviourName);
                    if(behaviourType==null||!typeof(MonoBehaviour).IsAssignableFrom(behaviourType))return "en";
                    settingsField=AccessTools.Field(behaviourType,"settings");
                    stoppedField=AccessTools.Field(behaviourType,"stopped");
                    translatorField=AccessTools.Field(behaviourType,"translator");
                    LocalText.ExternalTranslate=TranslateExternal;
                }
                if(settingsField==null||stoppedField?.FieldType!=typeof(bool))return "en";
                if(!Active(source)) {
                    source=null;
                    double now=Time.realtimeSinceStartup;
                    if(now<nextDiscovery)return "en";
                    nextDiscovery=now+5;
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType(behaviourType,FindObjectsSortMode.None)) {
                        var behaviour=candidate as MonoBehaviour;
                        if(!Active(behaviour))continue;
                        if(source!=null){source=null;return "en";}
                        source=behaviour;
                    }
                }
                if(source==null)return "en";
                var settings=settingsField.GetValue(source);
                if(settings==null)return "en";
                if(settings.GetType()!=settingsType) {
                    settingsType=settings.GetType();
                    languageField=AccessTools.Field(settingsType,"language");
                    interfaceField=AccessTools.Field(settingsType,"translateInterface");
                }
                if(languageField?.FieldType!=typeof(string)||interfaceField?.FieldType!=typeof(bool)||(bool)interfaceField.GetValue(settings)==false)return "en";
                return LocalText.NormalizeLanguage((string)languageField.GetValue(settings));
            }catch(Exception) {
                source=null;
                return "en";
            }
        }

        string TranslateExternal(string english,string language) {
            try {
                if(!Active(source)||LocalText.NormalizeLanguage((string)languageField.GetValue(settingsField.GetValue(source)))!=language)return null;
                var engine=translatorField?.GetValue(source);if(engine==null)return null;
                translate=AccessTools.Method(engine.GetType(),"Translate",new[]{typeof(string)});
                return translate?.Invoke(engine,new object[]{english}) as string;
            }catch{return null;}
        }
        bool Active(MonoBehaviour value)=>value!=null&&value.isActiveAndEnabled&&!(bool)stoppedField.GetValue(value);
    }
}
