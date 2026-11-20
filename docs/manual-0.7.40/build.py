#!/usr/bin/env python3
"""Build the standalone illustrated 0.7.40 DE/EN PDFs, HTML and Markdown."""
from pathlib import Path
import sys,re,json,hashlib,base64,html,shutil,csv
from datetime import date
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle,getSampleStyleSheet
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.platypus import BaseDocTemplate,PageTemplate,Frame,Paragraph,Spacer,PageBreak,Table,LongTable,TableStyle,KeepTogether,Image
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.graphics.shapes import Drawing,Rect,Line,String,Circle,Polygon
from reportlab.graphics import renderSVG
sys.path.insert(0,str(Path(__file__).parent))
from content import CHAPTERS
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'build/packages/EnusDE-Signal-Interlocking-0.7.40-P0.zip'
EXPECTED='7fad126628bb6952f90002780fd23c28d26046f43e26a21a9b4c87e35cc2881e'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==EXPECTED,'Wrong source release'
OUT=ROOT/'build/manuals/0.7.40-r1';OUT.mkdir(parents=True,exist_ok=True)
ASSETS=OUT/'assets';ASSETS.mkdir(exist_ok=True)
FONTS=Path('/usr/share/fonts/truetype/dejavu')
for name,file in [('Manual','DejaVuSans.ttf'),('Manual-Bold','DejaVuSans-Bold.ttf'),('Manual-Italic','DejaVuSans-Oblique.ttf')]:pdfmetrics.registerFont(TTFont(name,str(FONTS/file)))
pdfmetrics.registerFontFamily('Manual',normal='Manual',bold='Manual-Bold',italic='Manual-Italic',boldItalic='Manual-Bold')
NAVY=colors.HexColor('#142d3e');TEAL=colors.HexColor('#087f8c');GREEN=colors.HexColor('#237a45');AMBER=colors.HexColor('#a15d00');RED=colors.HexColor('#b52736');INK=colors.HexColor('#203744');GRAY=colors.HexColor('#81949d');LIGHT=colors.HexColor('#edf4f6');WHITE=colors.white
W=486

def drawing(name,lang):
    d=Drawing(W,205);de=lang=='de'
    def txt(x,y,t,size=10,color=INK,anchor='start',bold=False):d.add(String(x,y,t,fontName='Manual-Bold' if bold else 'Manual',fontSize=size,fillColor=color,textAnchor=anchor))
    def line(x1,y1,x2,y2,color=GRAY,width=3,dash=None):
        obj=Line(x1,y1,x2,y2,strokeColor=color,strokeWidth=width)
        if dash:obj.strokeDashArray=dash
        d.add(obj)
    def box(x,y,w,h,label,fill=LIGHT):
        d.add(Rect(x,y,w,h,rx=7,ry=7,fillColor=fill,strokeColor=colors.HexColor('#c9dade'),strokeWidth=.8))
        labels=label.split('\n')
        for i,t in enumerate(labels):txt(x+w/2,y+h/2+4-(i-(len(labels)-1)/2)*14,t,9,anchor='middle',bold=i==0)
    def arrow(x1,y1,x2,y2,color=TEAL,width=2):
        import math
        line(x1,y1,x2,y2,color,width);a=math.atan2(y2-y1,x2-x1);sz=6
        pts=[x2,y2,x2-sz*math.cos(a-.55),y2-sz*math.sin(a-.55),x2-sz*math.cos(a+.55),y2-sz*math.sin(a+.55)]
        d.add(Polygon(pts,fillColor=color,strokeColor=color))
    def sig(x,y,label,c=RED,right=True):
        line(x,y,x,y+24,INK,1.5);d.add(Rect(x-5,y+17,10,19,rx=3,ry=3,fillColor=NAVY,strokeColor=NAVY));d.add(Circle(x,y+26,2.7,fillColor=c,strokeColor=c));txt(x,y+42,label,9,anchor='middle',bold=True);arrow(x-6 if right else x+6,y-8,x+10 if right else x-10,y-8,TEAL,1)
    def boundary(x,y,label):
        line(x-3,y-3,x+3,y+3,TEAL,1.3);line(x-3,y+3,x+3,y-3,TEAL,1.3);txt(x,y-18,label,8,anchor='middle')
    if name=='workflow':
        for x,label in [(12,'1  Bauen\nWeltobjekte' if de else '1  Build\nWorld objects'),(178,'2  Zuordnen\nGleisbild + GFM' if de else '2  Bind\nPanel + detection'),(344,'3  Bedienen\nFahrstraßen' if de else '3  Operate\nRoutes')]:box(x,75,130,80,label)
        arrow(145,114,172,114);arrow(311,114,338,114)
        txt(243,37,'Ein Gleisbild ist eine verknüpfte Darstellung.' if de else 'A diagram is a linked representation.',10,anchor='middle')
    elif name=='station':
        line(25,108,450,108);line(225,108,345,48);line(345,48,450,48)
        sig(65,108,'S1',right=True);sig(405,108,'S2');sig(405,48,'S3')
        txt(225,133,'W1',11,bold=True);txt(270,93,'1',9);txt(340,67,'2',9)
        boundary(105,108,'A');boundary(195,108,'B');boundary(290,108,'B');boundary(302,69,'B')
        txt(18,185,'S1 → S2  /  S1 → S3',12,bold=True)
        txt(18,17,'Schematische Lernanlage · keine echte Betriebsanzeige' if de else 'Teaching schematic · not a live operating display',9)
    elif name=='move':
        for n,x in enumerate([9,173,337]):
            title=['1  Markieren','2  Verschieben','3  Ecke wählen'][n] if de else ['1  Select','2  Move','3  Pick corner'][n]
            txt(x,183,title,10,bold=True)
            for row in range(4):
                for col in range(5):d.add(Rect(x+col*26,48+row*26,26,26,fillColor=WHITE,strokeColor=colors.HexColor('#d6e1e5'),strokeWidth=.5))
            xx=x+26 if n<2 else x+52; yy=100 if n<2 else 74
            d.add(Rect(xx,yy,52,26,fillColor=colors.HexColor('#cbe6ee'),strokeColor=TEAL,strokeWidth=1.5))
            line(xx+3,yy+13,xx+49,yy+13,GRAY,3)
        arrow(147,98,166,98);arrow(311,98,330,98)
        txt(243,20,'Links bleiben beim Verschieben erhalten.' if de else 'Bindings move with the selection.',9,anchor='middle')
    elif name=='aspects':
        rows=[('Hp 0',['#d92838'],'Halt' if de else 'Stop'),('Hp 1',['#37b965'],'Fahrt' if de else 'Proceed'),('Hp 2',['#37b965','#f4c645'],'Langsamfahrt' if de else 'Reduced speed'),('Sh 1',['white','white'],'Rangieren' if de else 'Shunting'),('Vr 0',['#f4c645','#f4c645'],'Halt erwarten' if de else 'Expect Stop'),('Vr 1',['#37b965','#37b965'],'Fahrt erwarten' if de else 'Expect Proceed'),('Vr 2',['#f4c645','#37b965'],'Langsam erwarten' if de else 'Expect reduced'),('K',['white'],'Kennlicht' if de else 'Identification')]
        for i,(aspect,lamps,label) in enumerate(rows):
            x=6+(i%4)*121;y=109-(i//4)*100
            d.add(Rect(x,y,112,91,rx=6,ry=6,fillColor=LIGHT,strokeColor=colors.HexColor('#d4e0e4')))
            txt(x+56,y+75,aspect,12,anchor='middle',bold=True)
            d.add(Rect(x+32,y+27,48,35,rx=5,ry=5,fillColor=NAVY,strokeColor=NAVY))
            for j,c in enumerate(lamps):
                # Hp 2 has green above yellow; distant/shunting pairs are diagonal.
                cx=x+56 if aspect=='Hp 2' or len(lamps)==1 else x+56+(j-.5)*17
                cy=y+52-j*16 if aspect=='Hp 2' else (y+44+(j-.5)*8 if len(lamps)>1 else y+44)
                d.add(Circle(cx,cy,5,fillColor=colors.HexColor(c) if c!='white' else WHITE,strokeColor=colors.HexColor(c) if c!='white' else WHITE))
            txt(x+56,y+12,label,8.3,anchor='middle')
    elif name=='directions':
        box(15,96,205,80,'Gleisbild' if de else 'Panel');box(266,96,205,80,'Spielgleis' if de else 'World track')
        arrow(45,115,185,115,TEAL);txt(117,61,'Drehen / Spiegeln' if de else 'Rotate / mirror',10,anchor='middle')
        arrow(300,115,439,115,GREEN);txt(369,61,'Gleis-ID + Meter + Richtung' if de else 'Track ID + metres + facing',9,anchor='middle')
        boundary(369,90,'GFM');txt(243,22,'Einstellungen getrennt prüfen.' if de else 'Check these settings separately.',10,anchor='middle',bold=True)
    elif name=='materials':
        for x,label in [(8,'Übergabegleis\nFM / XM' if de else 'Interchange\nFM / XM'),(174,'MOW + Material\nAusrüstung aktiv' if de else 'MOW + supplies\nEquipment enabled'),(340,'Baustelle\n≤ 250 Gleis-m' if de else 'Worksite\n≤ 250 track m')]:box(x,85,138,70,label)
        arrow(149,120,168,120);arrow(315,120,334,120)
        txt(243,39,'Alternative: Materiallager → geplante Anlage' if de else 'Alternative: material depot → planned object',10,anchor='middle')
    elif name=='point':
        line(40,94,440,94,GRAY,4);line(240,94,414,169,GRAY,4)
        for a,b,c,e in [(160,94,240,94),(240,94,328,94),(240,94,314,126)]:line(a,b,c,e,colors.HexColor('#b4dfe5'),13)
        line(40,94,440,94,GRAY,3);line(240,94,414,169,GRAY,3)
        boundary(160,94,'B');boundary(328,94,'B');boundary(314,126,'B')
        txt(65,117,'Stamm / Spitze' if de else 'Common / tip',9);txt(350,65,'Normal',9);txt(360,185,'Abzweig' if de else 'Reverse',9);txt(235,55,'W1',12,bold=True)
        txt(243,20,'Heller Bereich: tatsächlicher Weichenschutz, je Arm prüfen.' if de else 'Light band: actual point protection; inspect each arm.',9,anchor='middle')
    elif name=='boundaries':
        line(45,111,360,111,GRAY,5);line(360,111,449,111,AMBER,5)
        for x,label in [(45,'A'),(204,'B'),(360,'C')]:boundary(x,111,label)
        arrow(360,148,300,148,TEAL);txt(300,169,'C überwacht nach links' if de else 'C monitors to the left',9,anchor='middle')
        txt(120,64,'Abschnitt 1' if de else 'Section 1',10,anchor='middle');txt(278,64,'Abschnitt 2' if de else 'Section 2',10,anchor='middle')
        txt(405,64,'nicht durch C' if de else 'not assured',8,anchor='middle');txt(405,49,'zugesichert' if de else 'by this C',8,anchor='middle')
    elif name=='gfm-link':
        labels=['1  Grenzen\nWelt + Anker','2  Abschnitt\nphysischer Weg','3  Gleisbild\nFelder zuordnen'] if de else ['1  Boundaries\nWorld + anchors','2  Section\nPhysical path','3  Diagram\nAssign fields']
        for x,label in zip([8,174,340],labels):box(x,85,138,75,label)
        arrow(149,122,169,122);arrow(315,122,335,122)
        txt(243,40,'Physisch gültig ≠ frei ≠ im Gleisbild zugeordnet' if de else 'Physically valid ≠ clear ≠ assigned on the diagram',10,anchor='middle',bold=True)
    elif name=='route':
        labels=['1  Anfordern','2  Prüfen','3  Festlegen','4  Fahren'] if de else ['1  Request','2  Check','3  Establish','4  Move']
        for i,label in enumerate(labels):
            x=6+i*121;box(x,98,108,60,label)
            if i<3:arrow(x+110,128,x+118,128)
        txt(243,52,'Erst der tatsächliche Fahrtbegriff zählt.' if de else 'The actual proceed aspect is what counts.',11,anchor='middle',bold=True)
        txt(243,26,'Mehrdeutiger Weg → Auswahl; gesperrter Weg → Meldung.' if de else 'Ambiguous path → choice; blocked path → explanation.',9,anchor='middle')
    elif name=='passage':
        for i,y in enumerate([157,96,35]):
            line(20,y,460,y,GRAY,2);line(175,y-9,175,y+15,TEAL,1);line(227,y-9,227,y+15,TEAL,1,dash=[3,3])
            x=[70,135,243][i];d.add(Rect(x,y-5,100,10,fillColor=NAVY,strokeColor=NAVY));arrow(x+100,y+18,x+114,y+18,GREEN)
            label=['1  Vor dem Signal','2  Spitze passiert','3  Schluss frei → prüfen'][i] if de else ['1  Before signal','2  Leading end passes','3  Tail clear → prove'][i]
            txt(20,y+31,label,9,bold=True)
        txt(175,192,'Signal',9,anchor='middle');txt(250,192,'+ 5 m*',9,anchor='middle');txt(385,9,'* Standard / default',8,anchor='middle')
    elif name=='protection':
        line(25,103,457,103,GREEN,5);line(210,103,365,175,colors.HexColor('#8a6ba7'),6)
        d.add(Rect(180,87,83,32,fillColor=None,strokeColor=TEAL,strokeWidth=2))
        sig(346,166,'RS',RED,right=False);txt(218,56,'W1',11,bold=True)
        txt(40,130,'Fahrweg' if de else 'Route',10,color=GREEN);txt(274,76,'Weichenschutz' if de else 'Point guards',9,color=TEAL)
        txt(86,179,'Seitenschutz' if de else 'Flank protection',10,color=colors.HexColor('#775192'))
        txt(243,19,'Schutzsignal oder abweisende Weiche + Gefahrenkorridor' if de else 'Protective signal or diverting point + threat corridor',9,anchor='middle')
    elif name=='numbers':
        box(163,127,160,60,'Quelle / Source\n00123');box(13,25,190,60,'Anzeigekopie 1\n00123' if de else 'Repeated display 1\n00123');box(283,25,190,60,'Anzeigekopie 2\n00123' if de else 'Repeated display 2\n00123')
        arrow(210,124,115,88);arrow(276,124,371,88)
        txt(243,104,'nur lesen' if de else 'read only',9,anchor='middle')
    elif name=='gwb':
        box(5,108,110,70,'Stellwerk A' if de else 'Interlocking A');box(371,108,110,70,'Stellwerk B' if de else 'Interlocking B')
        line(115,142,371,142,GRAY,4);arrow(169,172,319,172,AMBER,3)
        txt(243,188,'Erlaubnis A → B' if de else 'Permission A → B',10,anchor='middle',bold=True)
        box(165,24,156,60,'STR-001\nAnzeige / Indicator');line(243,85,243,119,GRAY,1,dash=[3,3])
        txt(243,8,'0.7.40: separates Feld ohne Gleisanschlüsse' if de else '0.7.40: separate field without rail ports',8.8,anchor='middle')
    elif name=='master':
        box(22,133,183,57,'Örtliche Tafel A' if de else 'Local panel A');box(282,133,183,57,'Örtliche Tafel B' if de else 'Local panel B')
        box(61,19,364,76,'ESTW / Master\nModul A                 Modul B');arrow(112,130,151,98);arrow(374,130,334,98)
        txt(243,109,'Verknüpfte Ansichten' if de else 'Linked views',8.7,anchor='middle')
    elif name=='automation':
        labels=['Kontakt','Zugrolle','Signalziel','Prüfungen','Fahrtbegriff'] if de else ['Contact','Train duty','Signal target','Checks','Aspect']
        for i,label in enumerate(labels):
            x=4+i*97;box(x,100,87,60,label)
            if i<4:arrow(x+88,130,x+95,130)
        txt(243,51,'Unklar / belegt / keine Erlaubnis → warten mit Grund' if de else 'Unclear / occupied / no permission → wait with a reason',9,anchor='middle')
        txt(243,26,'Achstest ≠ sichere Gleisfreimeldung' if de else 'Axle test ≠ proved track detection',10,anchor='middle',bold=True)
    elif name=='backup':
        labels=['Spielstand\nWelt + Fahrzeuge','Anlagensicherung\nModkonfiguration','Diagnose-ZIP\nFehleraufnahme'] if de else ['Game save\nWorld + vehicles','Installation backup\nMod configuration','Diagnostic ZIP\nIssue snapshot']
        for x,label in zip([5,171,337],labels):box(x,86,145,90,label)
        txt(243,50,'Nur die geprüfte Anlagensicherung im Sicherungsmenü wiederherstellen.' if de else 'Restore only a checked installation backup in the recovery menu.',8.5,anchor='middle')
        txt(243,25,'Diagnoseexport ist kein Wiederherstellungsstand.' if de else 'Diagnostic export is not a recovery snapshot.',10,anchor='middle',bold=True)
    else:raise ValueError(name)
    return d

styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyManual',fontName='Manual',fontSize=9.4,leading=13.2,textColor=INK,spaceAfter=6,allowWidows=0,allowOrphans=0))
styles.add(ParagraphStyle(name='Chapter',parent=styles['BodyManual'],fontName='Manual-Bold',fontSize=23,leading=28,spaceAfter=18,keepWithNext=True))
styles.add(ParagraphStyle(name='Sub',parent=styles['BodyManual'],fontName='Manual-Bold',fontSize=12,leading=16,spaceBefore=11,spaceAfter=7,keepWithNext=True))
styles.add(ParagraphStyle(name='CaptionManual',parent=styles['BodyManual'],fontSize=8,leading=11,textColor=TEAL,spaceAfter=12))
styles.add(ParagraphStyle(name='TableManual',parent=styles['BodyManual'],fontSize=8.2,leading=11.7,spaceAfter=0))
styles.add(ParagraphStyle(name='TableHeadManual',parent=styles['TableManual'],fontName='Manual-Bold',textColor=WHITE))
styles.add(ParagraphStyle(name='SmallManual',parent=styles['BodyManual'],fontSize=8,leading=11,textColor=GRAY))
styles.add(ParagraphStyle(name='CoverTitle',parent=styles['Chapter'],fontSize=34,leading=40,spaceAfter=15))
styles.add(ParagraphStyle(name='TOCManual',parent=styles['BodyManual'],leftIndent=0,firstLineIndent=0,fontSize=9.4,leading=13,spaceBefore=2))

def inline(s):
    s=html.escape(s)
    return re.sub(r'\*\*(.+?)\*\*',r'<b>\1</b>',s).replace('\n','<br/>')
def para(s,style='BodyManual'):return Paragraph(inline(s),styles[style])

class Doc(BaseDocTemplate):
    def __init__(self,path,lang):
        self.manual_language=lang;self.chapter_name='';super().__init__(str(path),pagesize=A4,leftMargin=54,rightMargin=54,topMargin=58,bottomMargin=50,title='EnusDE 0.7.40 '+('Handbuch' if lang=='de' else 'User manual'),author='EnusDE',subject='Public 0.7.40-P0 / Railroader 0.6.2',pageCompression=1)
        self.addPageTemplates(PageTemplate(id='manual',frames=[Frame(54,50,A4[0]-108,A4[1]-108,id='main',leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)],onPage=self.chrome))
    def chrome(self,c,doc):
        c.saveState()
        if doc.page>1:
            c.setFont('Manual',8);c.setFillColor(TEAL);c.drawString(54,A4[1]-30,'ENUSDE  /  SIGNAL & INTERLOCKING')
            c.setFillColor(GRAY);c.drawRightString(A4[0]-54,A4[1]-30,'0.7.40  ·  '+self.manual_language.upper());c.setStrokeColor(colors.HexColor('#cfdee3'));c.line(54,A4[1]-39,A4[0]-54,A4[1]-39)
        c.setFont('Manual',7.8);c.setFillColor(GRAY);c.drawString(54,29,'Ausgabe 1.0 · 16.11.2026' if self.manual_language=='de' else 'Edition 1.0 · 16 Nov 2026');c.drawRightString(A4[0]-54,29,str(doc.page));c.restoreState()
    def afterFlowable(self,flowable):
        if isinstance(flowable,Paragraph) and flowable.style.name=='Chapter':
            key=getattr(flowable,'anchor_key',None)
            if key:
                self.canv.bookmarkPage(key);self.canv.addOutlineEntry(flowable.getPlainText(),key,0,False)
                if key!='contents':self.notify('TOCEntry',(0,flowable.getPlainText(),self.page,key))

def header(title,key):
    obj=para(title,'Chapter');obj.anchor_key=key;return obj

def render(lang):
    de=lang=='de';base='EnusDE-0.7.40-'+('Handbuch-DE' if de else 'Manual-EN');figcount=0
    story=[Spacer(1,40),para('ENUSDE','SmallManual'),Spacer(1,20),para('Signal &<br/>Interlocking'.replace('<br/>','\n'),'CoverTitle'),para('Das illustrierte Handbuch' if de else 'The illustrated user manual','Sub'),para('Für Lokführer, Fahrdienstleiter und Anlagenbauer' if de else 'For drivers, dispatchers and installation builders'),Spacer(1,16),para('0.7.40-P0  /  Railroader 0.6.2','Sub'),Spacer(1,20),drawing('station',lang),Spacer(1,16),para('Veröffentlichte Version 0.7.40. Separate Dokumentation, Ausgabe 1.0. Mit Schrittfolgen, Schaubildern und Fehlerhilfe.' if de else 'Public release 0.7.40. Standalone documentation, edition 1.0. With step-by-step workflows, diagrams and troubleshooting.'),para('H/V-Schwerpunkt · keine späteren Testfunktionen' if de else 'H/V focus · excludes later test features','SmallManual'),PageBreak(),header('Inhalt' if de else 'Contents','contents')]
    toc=TableOfContents();toc.levelStyles=[styles['TOCManual']];story.extend([toc,PageBreak()])
    md=[f'# EnusDE Signal & Interlocking — {"Handbuch" if de else "User manual"}\n\n**0.7.40-P0 · Railroader 0.6.2 · {"Ausgabe" if de else "Edition"} 1.0 · 2026-11-16**\n']
    htmlparts=[]
    htmltoc=[]
    for n,ch in enumerate(CHAPTERS,1):
        if n>1:story.append(PageBreak())
        title=f'{n:02d}  {ch[lang]}';story.append(header(title,ch['id']));md.append(f'\n## {n}. {ch[lang]}\n');htmltoc.append(f'<li><a href="#{ch["id"]}">{n}. {html.escape(ch[lang])}</a></li>');htmlparts.append(f'<section id="{ch["id"]}"><h2>{html.escape(title)}</h2>')
        for b in ch['blocks']:
            kind=b['type'];text=b[lang]
            if kind=='pagebreak':
                story.append(PageBreak());htmlparts.append('<div class="printbreak"></div>')
            elif kind in ['p','h','note']:
                if kind=='note':
                    t=Table([[para(text)]],colWidths=[W-2],style=TableStyle([('BACKGROUND',(0,0),(-1,-1),LIGHT),('BOX',(0,0),(-1,-1),.7,TEAL),('LEFTPADDING',(0,0),(-1,-1),11),('RIGHTPADDING',(0,0),(-1,-1),11),('TOPPADDING',(0,0),(-1,-1),10),('BOTTOMPADDING',(0,0),(-1,-1),5)]));story.extend([t]+([Spacer(1,11)] if b is not ch['blocks'][-1] else []));md.append('\n> '+text+'\n');htmlparts.append('<aside>'+inline(text)+'</aside>')
                elif kind=='h':story.append(para(text,'Sub'));md.append('\n### '+text+'\n');htmlparts.append('<h3>'+inline(text)+'</h3>')
                else:story.append(para(text));md.append('\n'+text+'\n');htmlparts.append('<p>'+inline(text)+'</p>')
            elif kind in ['steps','bullets']:
                lines=[]
                for i,line_ in enumerate(text,1):
                    prefix=f'{i:02d}' if kind=='steps' else '•'
                    row=Table([[para(prefix,'CaptionManual'),para(line_)]],colWidths=[28,W-28],style=TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),0),('RIGHTPADDING',(0,0),(-1,-1),3),('TOPPADDING',(0,0),(-1,-1),2),('BOTTOMPADDING',(0,0),(-1,-1),1)]));story.append(row);lines.append(f'{i}. {line_}' if kind=='steps' else '- '+line_)
                md.append('\n'+'\n'.join(lines)+'\n');tag='ol' if kind=='steps' else 'ul';htmlparts.append('<'+tag+'>'+''.join('<li>'+inline(x)+'</li>' for x in text)+'</'+tag+'>')
            elif kind=='table':
                rows=[[para(cell,'TableHeadManual' if i==0 else 'TableManual') for cell in row] for i,row in enumerate(text)]
                t=LongTable(rows,colWidths=[W*.32,W*.68],repeatRows=1,hAlign='LEFT',style=TableStyle([('BACKGROUND',(0,0),(-1,0),NAVY),('ROWBACKGROUNDS',(0,1),(-1,-1),[WHITE,LIGHT]),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),('TOPPADDING',(0,0),(-1,-1),5.5),('BOTTOMPADDING',(0,0),(-1,-1),5.5),('LINEBELOW',(0,0),(-1,0),1,TEAL)]));story.extend([t]+([Spacer(1,11)] if b is not ch['blocks'][-1] else []))
                md.append('\n'+' | '.join(text[0])+'\n'+' | '.join(['---']*len(text[0]))+'\n'+'\n'.join(' | '.join(row) for row in text[1:])+'\n');htmlparts.append('<table>'+''.join('<tr>'+''.join(('<th>' if i==0 else '<td>')+inline(cell)+('</th>' if i==0 else '</td>') for cell in row)+'</tr>' for i,row in enumerate(text))+'</table>')
            elif kind=='figure':
                figcount+=1;name=b['name'];caption=f'{"Abb." if de else "Fig."} {figcount:02d} · '+text
                if name=='gfm-preview':
                    imagepath=ASSETS/'gfm-abc-preview.png';shutil.copyfile(ROOT/'build/manuals/evidence-0.7.40/EnusDE-Signaltechnik/Guides/images/gfm-abc-preview.png',imagepath)
                    from PIL import Image as PILImage
                    im=PILImage.open(imagepath);graphic=Image(str(imagepath),width=W,height=W*im.height/im.width);uri='data:image/png;base64,'+base64.b64encode(imagepath.read_bytes()).decode();htmlparts.append(f'<figure><img src="{uri}" alt="{html.escape(caption)}"><figcaption>{html.escape(caption)}</figcaption></figure>');asset='gfm-abc-preview.png'
                else:
                    graphic=drawing(name,lang);graphic.scale(.9,.9);graphic.width*=.9;graphic.height*=.9;asset=f'{name}-{lang}.svg';svg=renderSVG.drawToString(graphic);(ASSETS/asset).write_text(svg);htmlparts.append('<figure>'+svg+'<figcaption>'+html.escape(caption)+'</figcaption></figure>')
                story.append(KeepTogether([Spacer(1,5),graphic,para(caption,'CaptionManual')]));md.append(f'\n![{caption}](assets/{asset})\n')
        htmlparts.append('</section>')
    (OUT/(base+'.md')).write_text('\n'.join(md))
    css='''body{font:16px/1.65 Arial,sans-serif;color:#203744;max-width:980px;margin:40px auto;padding:0 24px}h1,h2,h3{color:#142d3e;line-height:1.2}h2{border-top:3px solid #087f8c;padding-top:25px;margin-top:60px}a{color:#087f8c}aside{padding:18px;border-left:4px solid #087f8c;background:#edf4f6}table{border-collapse:collapse;width:100%;font-size:.93em;margin:22px 0}th{background:#142d3e;color:white;text-align:left}td,th{padding:10px;vertical-align:top}tr:nth-child(even){background:#edf4f6}figure{margin:24px 0}svg,img{max-width:100%;height:auto}svg text{font-family:DejaVu Sans,Arial,sans-serif}figcaption{font-size:.86em;color:#087f8c}li{margin:8px 0}@media print{body{font-size:10pt;margin:0}h2{break-before:page}figure,aside{break-inside:avoid}nav{display:none}.printbreak{break-before:page}}'''
    document='<!doctype html><html lang="'+lang+'"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+base+'</title><style>'+css+'</style><body><h1>EnusDE Signal &amp; Interlocking</h1><p><strong>0.7.40-P0 · Railroader 0.6.2 · '+('Ausgabe' if de else 'Edition')+' 1.0</strong></p><nav><h2>'+('Inhalt' if de else 'Contents')+'</h2><ol>'+''.join(htmltoc)+'</ol></nav>'+''.join(htmlparts)+'</body></html>'
    (OUT/(base+'.html')).write_text(document)
    Doc(OUT/(base+'.pdf'),lang).multiBuild(story)
    return {'language':lang,'chapters':len(CHAPTERS),'figures':figcount,'base':base}

if __name__=='__main__':
    result=[render('de'),render('en')]
    (OUT/'content.de.json').write_text(json.dumps([{'id':c['id'],'title':c['de'],'blocks':[{**{k:v for k,v in b.items() if k not in ['de','en']},'content':b['de']} for b in c['blocks']]} for c in CHAPTERS],ensure_ascii=False,indent=2))
    (OUT/'content.en.json').write_text(json.dumps([{'id':c['id'],'title':c['en'],'blocks':[{**{k:v for k,v in b.items() if k not in ['de','en']},'content':b['en']} for b in c['blocks']]} for c in CHAPTERS],ensure_ascii=False,indent=2))
    with (OUT/'FEATURE-COVERAGE.csv').open('w',newline='') as f:
        w=csv.writer(f);w.writerow(['Chapter','Stable ID','German title','English title','0.7.40 archive evidence'])
        for n,c in enumerate(CHAPTERS,1):w.writerow([n,c['id'],c['de'],c['en'],'; '.join(c['sources'])])
    (OUT/'BUILD-RESULT.json').write_text(json.dumps({'mod_version':'0.7.40','package':'0.7.40-P0','archive_sha256':EXPECTED,'documentation_edition':'1.0','date':'2026-11-16','manuals':result,'gameplay_captured':False,'figures':'Original archive model preview plus purpose-made explanatory vectors; no reconstructed UI screenshots.'},indent=2))
    print(json.dumps(result,indent=2))
