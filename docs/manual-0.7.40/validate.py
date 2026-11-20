"""Validate final generated document artifacts, without changing runtime files."""
import hashlib,json,re,sys,zipfile,xml.etree.ElementTree as ET
from pathlib import Path
import fitz
from content import CHAPTERS
from build import ROOT,OUT,SOURCE,EXPECTED

def validate():
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==EXPECTED
    with zipfile.ZipFile(SOURCE) as archive:
        entries=set(archive.namelist())
        for ch in CHAPTERS:
            for source in ch['sources']:
                assert 'EnusDE-Signaltechnik/'+source in entries,(ch['id'],source)
    assert len({ch['id'] for ch in CHAPTERS})==len(CHAPTERS)==23
    for ch in CHAPTERS:
        for block in ch['blocks']:
            if block['type'] in ['table','steps','bullets']:
                assert len(block['de'])==len(block['en']),(ch['id'],block['type'])
    reports=[]
    for lang,base in [('de','EnusDE-0.7.40-Handbuch-DE'),('en','EnusDE-0.7.40-Manual-EN')]:
        pdf=fitz.open(OUT/(base+'.pdf'))
        text='\n'.join(p.get_text() for p in pdf)
        assert '\ufffd' not in text and '\x00' not in text
        toc=pdf.get_toc();assert len(toc)==24
        assert all(ch['id'] in (OUT/(base+'.html')).read_text() for ch in CHAPTERS)
        assert len(pdf[1].get_links())>=23
        assert all(1<=entry[2]<=len(pdf) for entry in toc)
        assert len(re.findall(r'(?:Abb\.|Fig\.)\s+\d{2}\s*·',text))==18
        empty=[];bounds=[]
        for n,page in enumerate(pdf,1):
            body=[b for b in page.get_text('blocks') if b[1]>44 and b[3]<page.rect.height-45]
            if not body:empty.append(n)
            for b in page.get_text('blocks'):
                if b[0]<0 or b[1]<0 or b[2]>page.rect.width+1 or b[3]>page.rect.height+1:bounds.append(n)
        assert not empty,empty
        assert not bounds,bounds
        md=(OUT/(base+'.md')).read_text()
        for asset in re.findall(r'\]\((assets/[^)]+)\)',md):assert (OUT/asset).is_file(),asset
        html=(OUT/(base+'.html')).read_text();assert 'src="assets/' not in html
        reports.append({'language':lang,'pages':len(pdf),'chapters':23,'figures':18,'outline_entries':len(toc),'contents_links':len(pdf[1].get_links()),'extracted_words':len(text.split()),'empty_body_pages':empty,'out_of_page_text':bounds,'pdf_sha256':hashlib.sha256((OUT/(base+'.pdf')).read_bytes()).hexdigest()})
    for path in (OUT/'assets').glob('*.svg'):ET.parse(path)
    for lang in ['DE','EN']:
        agenda=fitz.open(OUT/f'Screenshot-Agenda-{lang}.pdf')
        agenda_text=''.join(p.get_text() for p in agenda)
        assert all(f'S{i:02d}' in agenda_text for i in range(1,17))
    result={'source_archive_unchanged':True,'source_evidence_paths_checked':True,'bilingual_structure_checked':True,'html_self_contained':True,'markdown_assets_present':True,'svg_xml_valid':True,'screenshot_agenda_items_per_language':16,'manuals':reports,'validation_scope':'Document artifacts and archived evidence. No gameplay test or official DB-rulebook certification.'}
    (OUT/'VALIDATION.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))

if __name__=='__main__':validate()
