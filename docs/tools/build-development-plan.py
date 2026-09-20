from pathlib import Path
import re, html, json
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT, TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, KeepTogether
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs' / 'vr-fps-project-docs'
SRC = OUT / '01-development-plan.md'
PDF = OUT / 'VR-FPS-development-plan.pdf'
pdfmetrics.registerFont(TTFont('Malgun', 'C:/Windows/Fonts/malgun.ttf'))
pdfmetrics.registerFont(TTFont('MalgunBold', 'C:/Windows/Fonts/malgunbd.ttf'))
pdfmetrics.registerFontFamily('Malgun', normal='Malgun', bold='MalgunBold', italic='Malgun', boldItalic='MalgunBold')
WIDTH, HEIGHT = A4
MARGIN = 51
CW = WIDTH - MARGIN * 2
STYLES = {
    'title': ParagraphStyle('title', fontName='MalgunBold', fontSize=21, leading=30, textColor=colors.black, spaceAfter=14, wordWrap='CJK'),
    'h2': ParagraphStyle('h2', fontName='MalgunBold', fontSize=13, leading=20, textColor=colors.black, spaceBefore=15, spaceAfter=9, keepWithNext=True, wordWrap='CJK'),
    'body': ParagraphStyle('body', fontName='Malgun', fontSize=10, leading=16.5, spaceAfter=8, wordWrap='CJK', splitLongWords=True, allowWidows=0, allowOrphans=0),
    'meta': ParagraphStyle('meta', fontName='Malgun', fontSize=9, leading=14, textColor=colors.HexColor('#555555'), spaceAfter=15, wordWrap='CJK'),
    'cell': ParagraphStyle('cell', fontName='Malgun', fontSize=8.5, leading=13.4, wordWrap='CJK'),
    'th': ParagraphStyle('th', fontName='MalgunBold', fontSize=8.5, leading=13.4, wordWrap='CJK', textColor=colors.black),
}
def rich(s):
    s=html.escape(s)
    s=re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)', lambda m: f'<a href="{m.group(2)}" color="#244A68">{m.group(1)}</a>', s)
    s=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',s)
    s=re.sub(r'`([^`]+)`',r'<b>\1</b>',s)
    return s

story=[]
lines=SRC.read_text(encoding='utf-8').splitlines()
i=0
while i<len(lines):
    line=lines[i].strip()
    if not line:
        i+=1; continue
    if line.startswith('# '):
        story.append(Paragraph(rich(line[2:]),STYLES['title'])); i+=1; continue
    if line.startswith('## '):
        story.append(Paragraph(rich(line[3:]),STYLES['h2'])); i+=1; continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            raw=lines[i].strip()
            if not re.fullmatch(r'[|\s:—-]+',raw):
                rows.append([c.strip() for c in raw.strip('|').split('|')])
            i+=1
        n=len(rows[0])
        widths=[CW*.25,CW*.75] if n==2 else [CW*.20,CW*.36,CW*.44]
        if rows[0][0]=='단계': widths=[CW*.23,CW*.18,CW*.59]
        data=[[Paragraph(rich(c),STYLES['th' if ri==0 else 'cell']) for c in row] for ri,row in enumerate(rows)]
        t=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
        t.setStyle(TableStyle([
            ('GRID',(0,0),(-1,-1),.5,colors.HexColor('#D9D9D9')),
            ('BACKGROUND',(0,0),(-1,0),colors.HexColor('#E2EAF0')),
            ('VALIGN',(0,0),(-1,-1),'MIDDLE'),
            ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
            ('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),
        ]))
        story.extend([t,Spacer(1,10)])
        continue
    group=[line]; i+=1
    while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|')):
        group.append(lines[i].strip()); i+=1
    style='meta' if group[0].startswith('2026년') else 'body'
    story.append(Paragraph(rich(' '.join(group)), STYLES[style]))

def footer(canvas,doc):
    canvas.saveState()
    canvas.setFont('Malgun',8)
    canvas.setFillColor(colors.HexColor('#666666'))
    canvas.drawString(MARGIN,29,'현실 공간 기반 VR 좀비 FPS  |  개발 계획 1.1')
    canvas.drawRightString(WIDTH-MARGIN,29,str(doc.page))
    canvas.restoreState()

doc=SimpleDocTemplate(str(PDF),pagesize=A4,rightMargin=MARGIN,leftMargin=MARGIN,topMargin=46,bottomMargin=48,title='현실 공간 기반 VR 좀비 FPS 개발 계획서',author='',subject='개발 일정과 범위 및 단계별 완료 기준')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(PDF)
print(json.dumps({'pdf':str(PDF),'pages':len(reader.pages),'bytes':PDF.stat().st_size,'pageChars':[len(p.extract_text()) for p in reader.pages]},ensure_ascii=False))
