# -*- coding: utf-8 -*-
"""ประกอบภาพที่พร้อมวางในรายงาน -> picture/ready/fig-X-YY.(png|jpg)
ที่มา: ภาพจับหน้าต่าง Unity (evidence/screens) ภาพมุมมองผู้เล่น (picture/fig-4-*.png) แผนที่/ผัง/ไดอะแกรมที่วาดเอง
วิธีใช้  py tools/make_ready_figures.py"""
import os, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
P = lambda *a: os.path.join(ROOT, *a)
OUT = P('picture', 'ready'); os.makedirs(OUT, exist_ok=True)
SC = P('evidence', 'screens')

def border(im, c=None):
    # ไม่ใส่กรอบรอบภาพ (ผู้เขียนไม่ต้องการเส้นประ/เส้นขอบ) คงฟังก์ชันไว้เพื่อให้เรียกชื่อเดิมได้
    return im.convert('RGB')
def save(im, name, jpg=False, maxw=1700):
    im = im.convert('RGB')
    if im.width > maxw:
        im = im.resize((maxw, round(im.height * maxw / im.width)), Image.LANCZOS)
    path = os.path.join(OUT, name + ('.jpg' if jpg else '.png'))
    im.save(path, quality=90, optimize=True) if jpg else im.save(path, optimize=True)
    print(name, im.size, os.path.getsize(path) // 1024, 'KB')
def label_pair(a, b, la, lb, jpg=True):
    w = 960
    a = a.convert('RGB').resize((w, 540), Image.LANCZOS); b = b.convert('RGB').resize((w, 540), Image.LANCZOS)
    sheet = Image.new('RGB', (w * 2 + 12, 540 + 70), 'white')
    sheet.paste(a, (0, 0)); sheet.paste(b, (w + 12, 0))
    d = ImageDraw.Draw(sheet); f = ImageFont.truetype('C:/Windows/Fonts/THSarabunNew Bold.ttf', 46)
    for x, t in ((w // 2, la), (w + 12 + w // 2, lb)):
        d.text((x - d.textlength(t, font=f) / 2, 540 + 8), t, font=f, fill='black')
    return sheet

# 1.1 แผนที่  2.x หน้าจอ Unity  3.x
save(Image.open(P('picture', 'fig-1-01-simple.png')), 'fig-1-01')
save(border(Image.open(f'{SC}/e21.png').crop((9, 9, 1929, 1090))), 'fig-2-01')
save(border(Image.open(f'{SC}/e22.png').crop((1447, 108, 1929, 745))), 'fig-2-02')
save(border(Image.open(f'{SC}/e23.png').crop((1447, 108, 1929, 570))), 'fig-2-03')
save(border(Image.open(f'{SC}/menu2.png').crop((420, 25, 985, 405))), 'fig-3-03')
save(Image.open(P('picture', 'fig-3-04.png')), 'fig-3-04')
# 4.1-4.8 มุมมองผู้เล่น
for n in ('01', '02', '03', '04', '05', '06', '07', '08'):
    save(Image.open(P('picture', f'fig-4-{n}.png')), f'fig-4-{n}', jpg=True, maxw=1600)
save(label_pair(Image.open(P('picture', 'fig-4-09.png')), Image.open(P('picture', 'fig-4-09-ticket.png')),
                'ภาพถ่ายเก่า', 'ป้ายเลขหลังถ้วยชา'), 'fig-4-09', jpg=True, maxw=1900)
save(label_pair(Image.open(P('picture', 'fig-4-10-before.png')), Image.open(P('picture', 'fig-4-10-after.png')),
                'ก่อนหมุนปริซึม', 'หลังหมุนปริซึมถูกต้อง'), 'fig-4-10', jpg=True, maxw=1900)
save(border(Image.open(f'{SC}/e412.png').crop((1447, 108, 1929, 690))), 'fig-4-12')
save(border(Image.open(f'{SC}/testrunner.png')), 'fig-4-13')
save(border(Image.open(f'{SC}/e414.png').crop((9, 764, 1447, 960))), 'fig-4-14')
