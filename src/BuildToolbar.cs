// Copyright (c) 2026 EnusDE - Mod Menu / Disconnect Emergency Brake contributors.
// MIT; adapted from EnusDE Mod Menu 0.1.2. Full notice in LICENSE-NativeWindow.txt.
using System;
using System.Reflection;
using UI;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EnusDE.Signaltechnik {
    public sealed class BuildToolbar : IDisposable {
        readonly Action open;
        RectTransform button,row;
        TMP_Text label;
        float reserve,offset;
        public BuildToolbar(Action open){this.open=open;}
        public void Ensure(string caption) {
            if(button!=null){if(label!=null&&label.text!=caption)label.text=caption;return;}Dispose();
            var toolbar=UnityEngine.Object.FindFirstObjectByType<TopRightArea>();var assets=UIBuilderAssets.Shared;
            if(toolbar==null||assets==null)return;
            var reference=typeof(TopRightArea).GetField("timetableButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(toolbar) as Button;
            if(reference==null)return;
            HorizontalLayoutGroup layout=null;
            for(var parent=reference.transform.parent;parent!=null;parent=parent.parent){var candidate=parent.GetComponent<HorizontalLayoutGroup>();if(candidate!=null&&candidate.enabled){layout=candidate;break;}if(parent==toolbar.transform)break;}
            if(layout==null)return; // Never overlay a guessed screen coordinate over the clock.
            var rect=reference.GetComponent<RectTransform>();float width=rect.rect.width,height=rect.rect.height;
            if(width<=0)width=LayoutUtility.GetPreferredWidth(rect);if(height<=0)height=LayoutUtility.GetPreferredHeight(rect);if(width<=0||height<=0)return;
            var fitter=layout.GetComponent<ContentSizeFitter>();var outer=layout.transform.parent?.GetComponent<HorizontalOrVerticalLayoutGroup>();
            bool driven=fitter!=null&&fitter.enabled&&fitter.horizontalFit!=ContentSizeFitter.FitMode.Unconstrained||outer!=null&&outer.enabled&&outer.childControlWidth;
            try {
                var created=assets.CreateButton(UIBuilderAssets.ButtonStyle.Compact,layout.transform,open);button=created.Item1;row=layout.GetComponent<RectTransform>();
                button.gameObject.name="EnusDE Signal & Interlocking";button.SetAsLastSibling();button.sizeDelta=new Vector2(width,height);
                var size=button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>();size.ignoreLayout=false;size.minWidth=size.preferredWidth=width;size.flexibleWidth=0;size.minHeight=size.preferredHeight=height;size.flexibleHeight=0;
                label=created.Item2;
                label.text=caption;label.richText=false;label.margin=Vector4.zero;
                label.alignment=TextAlignmentOptions.Center;label.fontStyle=FontStyles.Bold;
                label.enableAutoSizing=true;label.fontSizeMin=7;label.fontSizeMax=Mathf.Max(7,Mathf.Min(16,height*.48f));
                label.textWrappingMode=TextWrappingModes.NoWrap;
                var labelLayout=label.GetComponent<LayoutElement>()??label.gameObject.AddComponent<LayoutElement>();
                labelLayout.ignoreLayout=true;
                WorkspaceSurface.Fill(label.rectTransform,1,0,1,0);
                var control=button.GetComponent<Button>();var background=button.GetComponent<Image>();if(background!=null){background.sprite=null;background.color=Color.clear;}
                if(control!=null){if(reference.targetGraphic!=null)label.color=reference.targetGraphic.color;control.targetGraphic=label;control.colors=reference.colors;control.transition=Selectable.Transition.ColorTint;}
                button.Tooltip("EnusDE Signal & Interlocking","");
                if(!driven){reserve=width+layout.spacing;offset=reserve*(1-row.pivot.x);row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,row.rect.width+reserve);var position=row.anchoredPosition;position.x-=offset;row.anchoredPosition=position;}
                LayoutRebuilder.MarkLayoutForRebuild(row);
            }catch{Dispose();throw;}
        }
        public void Dispose(){if(button!=null){button.SetParent(null,false);UnityEngine.Object.Destroy(button.gameObject);}button=null;label=null;if(row!=null){if(reserve!=0){row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Math.Max(0,row.rect.width-reserve));var position=row.anchoredPosition;position.x+=offset;row.anchoredPosition=position;}LayoutRebuilder.MarkLayoutForRebuild(row);}row=null;reserve=offset=0;}
    }
}
