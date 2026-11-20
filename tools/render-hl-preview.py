"""Render the isolated Hl gallery from the lamp plans exported by the real C# checks."""
from pathlib import Path
import html
import json

ROOT = Path(__file__).resolve().parent.parent
data = json.loads((ROOT / 'build/checks/hl-signalplans.json').read_text())
colors = {'Off': '#252b2d', 'Red': '#ff3030', 'Green': '#45f175', 'Yellow': '#ffd347', 'White': '#f1f4ff'}


def diagram(plan, animate=False):
    parts = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 180 210" role="img" aria-label="' + html.escape(plan['Code']) + '">',
             '<path d="M90 144V182" stroke="#8c9698" stroke-width="8"/>',
             '<path d="M54 24H126L136 34V131L126 141H54L44 131V34Z" fill="#101415" stroke="#758085"/>']
    for i, (position, color) in enumerate(zip(data['lamps'], plan['lamps'])):
        x, y = 90 + position[0] * 100, 24 + (4.15 - position[1]) * 100
        fill = colors[color]
        blink = bool(plan['FlashMask'] & (1 << i))
        css = ' class="blink"' if blink and animate else ''
        if i < 6:
            parts.append(f'<circle cx="{x}" cy="{y}" r="11" fill="#080b0c" stroke="#30383b"/>')
            if color != 'Off':
                parts.append(f'<circle cx="{x}" cy="{y}" r="14" fill="{fill}" opacity=".12"{css}/>')
            parts.append(f'<circle cx="{x}" cy="{y}" r="8" fill="{fill}"{css}/>')
            parts.append(f'<path d="M{x-13} {y-12}h26" stroke="#080b0c" stroke-width="4"/>')
            if blink:
                parts.append(f'<circle cx="{x}" cy="{y}" r="17" fill="none" stroke="#91a1aa" stroke-width="1" stroke-dasharray="3 3"/>')
        else:
            parts.append(f'<rect x="{x-37}" y="{y-7.5}" width="74" height="15" rx="2" fill="#101415" stroke="#465156"/>')
            parts.append(f'<rect x="{x-31}" y="{y-3.5}" width="62" height="7" fill="{fill}"/>')
    parts.append(f'<text x="90" y="204" fill="#e6eef2" text-anchor="middle" font-size="18" font-family="sans-serif">{html.escape(plan["Code"])}</text></svg>')
    return ''.join(parts)


def speed(value, german):
    if value == 2147483647:
        return 'Streckengeschwindigkeit' if german else 'Line speed'
    if value == 0:
        return 'Halt' if german else 'Stop'
    return f'{value} km/h'


cards = []
for p in data['gallery']:
    preview = p['current'] in (60, 100) or p['next'] == 100
    en = f'At this signal: {speed(p["current"], False)}. Next: {speed(p["next"], False)}.'
    de = f'Ab Signal: {speed(p["current"], True)}. Danach: {speed(p["next"], True)}.'
    if p['next'] == 40:
        en = en.replace('Next: 40 km/h', 'Next: 40/60 km/h')
        de = de.replace('Danach: 40 km/h', 'Danach: 40/60 km/h')
    badge = '<small><span lang="en">Preview only</span><span lang="de">Nur Vorschau</span></small>' if preview else ''
    cards.append('<article>' + diagram(p, True) + badge + '<p><span lang="en">' + en + '</span><span lang="de">' + de + '</span></p></article>')

document = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>EnusDE · Hl signal preview</title><style>
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#10171d;color:#e6eef2;font:16px/1.5 system-ui,sans-serif}main{max-width:1120px;margin:auto;padding:24px}
nav{display:flex;gap:8px;flex-wrap:wrap}button{background:#26343e;color:#e6eef2;border:1px solid #607786;border-radius:6px;padding:8px 16px;cursor:pointer}button[aria-pressed="true"]{border-color:#65d9bd;background:#18453e}
h1{font-size:clamp(25px,4vw,38px);line-height:1.2}p{max-width:78ch}.notice{padding:14px 18px;border-left:3px solid #e7bb56;background:#25271f}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:14px;margin:24px 0}
article{display:flex;flex-direction:column;align-items:center;background:#18232b;border:1px solid #3c505b;border-radius:8px;padding:12px}article svg{width:100%;max-width:180px;height:210px}article p{font-size:14px;margin:8px 0 0}small{font-size:12px;color:#e7bb56}a{color:#65d9bd}
body[data-lang="en"] [lang="de"],body[data-lang="de"] [lang="en"]{display:none}
@keyframes flash{0%,49.99%{opacity:1}50%,100%{opacity:0}}.blink{animation:flash 1s step-end infinite}.paused .blink{animation:none}
@media(prefers-reduced-motion:reduce){.blink{animation:none}}
</style><body data-lang="en"><main><nav><button id="en" aria-pressed="true">English</button><button id="de" aria-pressed="false">Deutsch</button><button id="pause" aria-pressed="false"><span lang="en">Freeze flashing</span><span lang="de">Blinken anhalten</span></button></nav>
<h1><span lang="en">Hl signals · visual test</span><span lang="de">Hl-Signale · Ansichtstest</span></h1>
<p><span lang="en">German Hl aspects following Ril 301.0103. Original diagrams generated from the tested lamp plans. Dashed rings mark flashing lights. The lower speed light and strips stay steady.</span><span lang="de">Deutsche Hl-Signalbilder nach Ril 301.0103. Eigene Zeichnungen aus den geprüften Lampenplänen. Gestrichelte Ringe markieren blinkende Lampen. Unteres Geschwindigkeitslicht und Lichtstreifen bleiben stetig.</span></p>
<p class="notice"><span lang="en">This page is an isolated preview and cannot control game signals. The live test supports unrestricted and 40 km/h display tiers. 60/100 km/h aspects are preview only. Native AI speed handling is unchanged. The drawings show a stylized head, not a certified installation drawing.</span><span lang="de">Diese Seite ist eine isolierte Vorschau und steuert keine Spielsignale. Der Betriebstest unterstützt Anzeigen ohne Signalbegrenzung und die 40-km/h-Stufe. 60/100 km/h nur als Vorschau. Die native KI-Geschwindigkeitsbehandlung bleibt unverändert. Die Zeichnungen zeigen einen stilisierten Schirm, keine verbindliche Anlagenzeichnung.</span></p>
<section class="grid">''' + ''.join(cards) + '''</section>
<p><span lang="en">Speed restrictions still apply. A warning for 40/60 km/h does not distinguish the two tiers. Hp 0 is the modern stop name. Configure and test distant-target bindings separately.</span><span lang="de">Sonstige Geschwindigkeitsbeschränkungen gelten weiterhin. Die Ankündigung 40/60 km/h unterscheidet diese beiden Stufen nicht. Hp 0 ist die heutige Bezeichnung des Haltbegriffs. Vorsignal-Zielbindungen getrennt einrichten und prüfen.</span></p>
<p><a href="https://www.dbinfrago.com/resource/blob/13174940/cb13a4d1ee7bc79e64ecad219bbd91c4/Ril-301-INB-2026-data.pdf">DB InfraGO · Ril 301 · 301.0103 / 301.0103A01</a></p>
<script>
for(const lang of ['en','de'])document.getElementById(lang).onclick=()=>{document.body.dataset.lang=lang;document.documentElement.lang=lang;for(const choice of ['en','de'])document.getElementById(choice).setAttribute('aria-pressed',String(choice===lang));};
document.getElementById('pause').onclick=function(){const paused=document.body.classList.toggle('paused');this.setAttribute('aria-pressed',String(paused));};
</script></main></body></html>'''
guide = ROOT / 'mod/EnusDE-Signaltechnik/Guides/Hl-Signalbilder.html'
guide.write_text(document)

# Static diagram for a quick chat attachment. Same exported plans and coordinates.
cells = []
for i, p in enumerate(data['gallery']):
    x, y = 18 + (i % 4) * 220, 110 + (i // 4) * 244
    drawing = diagram(p).replace('<svg ', f'<svg x="{x}" y="{y}" width="204" height="225" ', 1)
    cells.append(drawing)
height = 110 + ((len(cells) + 3) // 4) * 244
svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="916" height="{height}" viewBox="0 0 916 {height}"><rect width="100%" height="100%" fill="#10171d"/><text x="26" y="45" fill="#e6eef2" font-family="sans-serif" font-size="28">Hl-Signalbilder · Ril 301 · Testvorschau</text><text x="26" y="77" fill="#a9bac5" font-family="sans-serif" font-size="15">Gestrichelter Ring = blinkend · 60/100 km/h derzeit nur Vorschau</text>' + ''.join(cells) + '</svg>'
output = ROOT / 'build/packages/Hl-Signalbilder-0.7.30.svg'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(svg)
print(f'Rendered {len(cards)} signal aspects: {guide}')
try:
    import cairosvg
    cairosvg.svg2png(bytestring=svg.encode(), write_to=str(output.with_suffix('.png')))
    print(output.with_suffix('.png'))
except ImportError:
    from PIL import Image, ImageDraw, ImageFont
    ratio = 2
    canvas = Image.new('RGB', (916 * ratio, height * ratio), '#10171d')
    draw = ImageDraw.Draw(canvas)
    font_path = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    font = ImageFont.truetype(font_path, 18 * ratio)
    heading = ImageFont.truetype(font_path, 28 * ratio)
    note = ImageFont.truetype(font_path, 15 * ratio)
    draw.text((26 * ratio, 15 * ratio), 'Hl-Signalbilder · Ril 301 · Testvorschau', font=heading, fill='#e6eef2')
    draw.text((26 * ratio, 60 * ratio), 'Gestrichelter Ring = blinkend · 60/100 km/h derzeit nur Vorschau', font=note, fill='#a9bac5')
    for i, plan in enumerate(data['gallery']):
        ox, oy, zoom = 18 + (i % 4) * 220, 110 + (i // 4) * 244, 204 / 180
        def point(x, y):
            return (round((ox + x * zoom) * ratio), round((oy + y * zoom) * ratio))
        draw.line([point(90, 144), point(90, 182)], fill='#8c9698', width=8 * ratio)
        draw.polygon([point(x, y) for x, y in [(54, 24), (126, 24), (136, 34), (136, 131), (126, 141), (54, 141), (44, 131), (44, 34)]], fill='#101415', outline='#758085', width=ratio)
        for index, (position, color) in enumerate(zip(data['lamps'], plan['lamps'])):
            x, y = 90 + position[0] * 100, 24 + (4.15 - position[1]) * 100
            if index < 6:
                draw.ellipse([point(x-11, y-11), point(x+11, y+11)], fill='#080b0c', outline='#30383b', width=ratio)
                draw.ellipse([point(x-8, y-8), point(x+8, y+8)], fill=colors[color])
                draw.line([point(x-13, y-12), point(x+13, y-12)], fill='#080b0c', width=4 * ratio)
                if plan['FlashMask'] & (1 << index):
                    for a in range(0, 360, 35):
                        draw.arc([point(x-17, y-17), point(x+17, y+17)], a, a+18, fill='#91a1aa', width=ratio)
            else:
                draw.rectangle([point(x-37, y-7.5), point(x+37, y+7.5)], fill='#101415', outline='#465156', width=ratio)
                draw.rectangle([point(x-31, y-3.5), point(x+31, y+3.5)], fill=colors[color])
        draw.text(point(90, 204), plan['Code'], font=font, fill='#e6eef2', anchor='ms')
    canvas.resize((916, height), Image.Resampling.LANCZOS).save(output.with_suffix('.png'))
    print(output.with_suffix('.png'))
