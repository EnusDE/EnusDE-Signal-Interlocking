using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed class SignalSearchEntry {
        public readonly string Id, Code, Name;
        public SignalSearchEntry(string id,string code,string name){Id=id;Code=code??"";Name=name??"";}
        public string Caption=>Name==""||string.Equals(Code,Name,StringComparison.OrdinalIgnoreCase)?Code:Code+"  ·  "+Name;
    }

    public static class SignalSearch {
        public static SignalSearchEntry[] Find(IEnumerable<SignalSearchEntry> entries,string query,int limit=12) {
            string trimmed=(query??"").Trim();
            var terms=trimmed.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
            return entries.Where(entry=>terms.All(term=>Has(entry.Code,term)||Has(entry.Name,term)||Has(entry.Id??"",term)))
                .OrderByDescending(entry=>string.Equals(entry.Code,trimmed,StringComparison.OrdinalIgnoreCase))
                .ThenBy(entry=>entry.Code,StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry=>entry.Id,StringComparer.Ordinal)
                .Take(limit).ToArray();
        }
        static bool Has(string value,string term)=>value.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0;
    }

    public static class SearchChoiceFields {
        public static void AddSearchChoice(this UIPanelBuilder c,string label,List<string> labels,int index,Action<int> selected) {
            c.AddSection(label);
            c.AddLabel(()=>index>=0&&index<labels.Count?labels[index]:"Keine Auswahl",UIPanelBuilder.Frequency.Periodic);
            var input=c.AddInputField("",_=>{},"Kennung oder Bezeichnung suchen",128).GetComponent<TMP_InputField>();
            var frame=c.VStack(_=>{});
            var picker=frame.gameObject.AddComponent<SignalPickerList>();
            var toggle=c.VStack(_=>{});
            var size=toggle.GetComponent<LayoutElement>()??toggle.gameObject.AddComponent<LayoutElement>();
            size.minHeight=size.preferredHeight=32;size.flexibleHeight=0;size.layoutPriority=10;
            var fitter=toggle.GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
            var button=EditorSurface.Button(toggle,"Liste öffnen / schließen",picker.Toggle);
            (button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;
            WorkspaceSurface.Fill(button.GetComponent<RectTransform>());
            var entries=labels.Select((text,i)=>new SignalSearchEntry(i.ToString(System.Globalization.CultureInfo.InvariantCulture),text,"")).ToArray();
            picker.Initialize(input,button.transform,entries,"Keine Treffer.",entry=>{
                index=int.Parse(entry.Id,System.Globalization.CultureInfo.InvariantCulture);
                selected(index);
            },Key.Backspace);
        }
    }

    sealed class SignalPickerList:MonoBehaviour {
        const int Height=192,RowHeight=64,RowCount=4;
        readonly RectTransform[] rows=new RectTransform[RowCount];
        readonly TMP_Text[] captions=new TMP_Text[RowCount];
        readonly List<RaycastResult> hits=new List<RaycastResult>();
        SignalSearchEntry[] candidates,matches=Array.Empty<SignalSearchEntry>();
        Action<SignalSearchEntry> choose;
        RectTransform frame,content;
        TMP_InputField input;
        Transform opener;
        ScrollRect scroll;
        TMP_Text empty;
        Key closeKey=Key.Backspace;

        public void Initialize(TMP_InputField field,Transform toggle,SignalSearchEntry[] entries,string noResults,
            Action<SignalSearchEntry> selected,Key shortcut) {
            input=field;opener=toggle;candidates=entries;choose=selected;closeKey=shortcut;
            frame=(RectTransform)transform;
            var sizing=frame.GetComponent<LayoutElement>()??gameObject.AddComponent<LayoutElement>();
            sizing.minHeight=sizing.preferredHeight=Height;sizing.flexibleHeight=0;sizing.layoutPriority=10;
            var stack=GetComponent<LayoutGroup>();if(stack!=null)stack.enabled=false;
            var fitter=GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
            var background=frame.GetComponent<Image>()??gameObject.AddComponent<Image>();
            background.color=new Color(.065f,.105f,.126f);
            var viewport=WorkspaceSurface.Rect(frame,"Signal results viewport");
            WorkspaceSurface.Fill(viewport,0,0,14,0);
            viewport.gameObject.AddComponent<RectMask2D>();
            content=WorkspaceSurface.Rect(viewport,"Signal result rows");
            content.anchorMin=new Vector2(0,1);
            content.anchorMax=content.pivot=new Vector2(1,1);
            scroll=gameObject.AddComponent<ScrollRect>();
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
            scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=RowHeight;
            var bar=SignalRuntime.WorkspaceBar(frame,true);
            bar.GetComponent<RectTransform>().offsetMin=new Vector2(-10,0);
            scroll.verticalScrollbar=bar;
            empty=WorkspaceSurface.Text(viewport,"Empty results",noResults,13);
            for(int i=0;i<rows.Length;i++){
                int slot=i;
                var button=EditorSurface.Button(content,"",()=>Pick(slot));
                rows[i]=button.GetComponent<RectTransform>();
                captions[i]=button.GetComponentInChildren<TMP_Text>();
                captions[i].alignment=TextAlignmentOptions.MidlineLeft;
                captions[i].fontSize=13;
                captions[i].textWrappingMode=TextWrappingModes.Normal;

            }
            scroll.onValueChanged.AddListener(_=>RefreshRows());
            input.onSelect.AddListener(_=>Open());
            input.onValueChanged.AddListener(_=>{if(gameObject.activeSelf)Filter();else Open();});
            gameObject.SetActive(false);
        }

        public void Toggle(){if(gameObject.activeSelf)Close();else Open();}
        public void Close(){gameObject.SetActive(false);}
        void Open(){
            if(gameObject.activeSelf)return;
            gameObject.SetActive(true);
            Filter();
            LayoutRebuilder.MarkLayoutForRebuild(frame.parent as RectTransform);
        }
        void Filter() {
            matches=SignalSearch.Find(candidates,input.text,int.MaxValue);
            content.sizeDelta=new Vector2(0,Mathf.Max(Height,matches.Length*RowHeight));
            content.anchoredPosition=Vector2.zero;
            scroll.verticalNormalizedPosition=1;
            RefreshRows();
        }
        int FirstRow=>Mathf.Max(0,Mathf.FloorToInt(content.anchoredPosition.y/RowHeight));
        void RefreshRows() {
            if(content==null)return;
            int first=FirstRow;
            empty.gameObject.SetActive(matches.Length==0);
            for(int i=0;i<rows.Length;i++) {
                int index=first+i;
                bool visible=index<matches.Length;
                rows[i].gameObject.SetActive(visible);
                if(!visible)continue;
                var entry=matches[index];
                rows[i].anchorMin=new Vector2(0,1);rows[i].anchorMax=new Vector2(1,1);rows[i].pivot=new Vector2(0,1);
                rows[i].anchoredPosition=new Vector2(0,-index*RowHeight);
                rows[i].sizeDelta=new Vector2(-2,RowHeight-1);
                if(captions[i].text!=entry.Caption)captions[i].text=entry.Caption;
            }
        }
        void Pick(int slot) {
            int index=FirstRow+slot;
            if(index>=matches.Length)return;
            var entry=matches[index];
            Close();
            choose?.Invoke(entry);
        }
        void Update() {
            var events=EventSystem.current;
            if(events==null)return;
            var focus=events.currentSelectedGameObject;
            bool typing=focus!=null&&(focus.GetComponentInParent<TMP_InputField>()?.isFocused==true||focus.GetComponentInParent<InputField>()?.isFocused==true);
            if(!typing&&closeKey!=Key.None&&Keyboard.current!=null&&Keyboard.current[closeKey].wasPressedThisFrame){Close();return;}
            var mouse=Mouse.current;
            if(mouse==null||!mouse.leftButton.wasPressedThisFrame)return;
            hits.Clear();
            events.RaycastAll(new PointerEventData(events){position=mouse.position.ReadValue()},hits);
            if(hits.Count==0){Close();return;}
            var clicked=hits[0].gameObject.transform;
            if(!clicked.IsChildOf(transform)&&!clicked.IsChildOf(input.transform)&&!clicked.IsChildOf(opener))Close();
        }
    }

    public sealed partial class SignalRuntime {
        void BuildSignalSearch(UIPanelBuilder c,PanelDraftBinding binding,bool alternate=false) {
            var draft=binding.Draft;
            bool numberField=draft.Kind==PanelTileKind.TrainNumber;
            var candidates=inventory.Values.Where(s=>s.IsSignal&&(numberField?s.Kind==MarkerRecord.Main:
                alternate?SignalCatalogue.Supports(s,true,false):true))
                .Select(s=>new SignalSearchEntry(s.Id,s.Name,s.DisplayTitle)).ToArray();
            string ReadId()=>alternate?draft.AlternateSignal:draft.Reference;
            void Select(string id) {
                binding.Do(()=>{
                    if(alternate)draft.AlternateSignal=id;
                    else {
                        string previous=draft.Reference;
                        draft.Reference=id??"";
                        if(!numberField&&(draft.AlternateSignal==null||draft.AlternateSignal==previous)&&
                            Object(id)?.Kind==MarkerRecord.Main&&Object(id).AllowSh1)
                            draft.AlternateSignal=id;
                        else if(!numberField&&draft.AlternateSignal!=null&&draft.AlternateSignal==previous)
                            draft.AlternateSignal=null;
                    }
                    status=id==null?"Zuordnung entfernt. Feld speichern.":"Auswahl: "+Code(id)+". Feld speichern, um die Zuordnung zu übernehmen.";
                    RefreshWorkspaceFields();panelView?.Refresh();
                });
            }
            c.AddSection(numberField?
                alternate?WindowText("Main signal in the opposite direction","Hauptsignal in der Gegenrichtung"):
                    WindowText("Main signal for number transfer","Hauptsignal für Zugnummernwechsel"):
                alternate?WindowText("Shunting button","Rangiertaste"):
                    WindowText("Signal link","Signalzuordnung"));
            c.AddLabel(()=> {
                string id=ReadId();
                var current=string.IsNullOrEmpty(id)?null:candidates.FirstOrDefault(entry=>entry.Id==id);
                return WindowText("Selected: ","Auswahl: ")+(current?.Caption??(string.IsNullOrEmpty(id)?WindowText("None","Keine"):WindowText("Signal unavailable","Signal fehlt")));
            },UIPanelBuilder.Frequency.Periodic);
            var input=c.AddInputField("",_=>{},WindowText("Search by code or name","Kennung oder Bezeichnung suchen"),64).GetComponent<TMP_InputField>();
            var frame=c.VStack(_=>{});
            var picker=frame.gameObject.AddComponent<SignalPickerList>();
            var toggle=c.VStack(_=>{});
            var size=toggle.GetComponent<LayoutElement>()??toggle.gameObject.AddComponent<LayoutElement>();
            size.minHeight=size.preferredHeight=32;size.layoutPriority=10;size.flexibleHeight=0;
            var fitter=toggle.GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
            var button=EditorSurface.Button(toggle,WindowText("Show or hide signals","Signalliste öffnen / schließen"),()=>binding.Do(picker.Toggle));
            (button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;
            WorkspaceSurface.Fill(button.GetComponent<RectTransform>());
            var opener=button.transform;
            picker.Initialize(input,opener,candidates,WindowText("No signals found.","Keine Signale gefunden."),entry=>Select(entry.Id),
                Enum.TryParse(Entry.SearchCloseKey,out Key key)?key:Key.Backspace);
            c.AddButton(WindowText("Clear link","Zuordnung entfernen"),()=>{picker.Close();Select(null);});
        }
    }
}
