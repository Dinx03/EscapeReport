# -*- coding: utf-8 -*-
"""แผนที่เส้นทางจากสถาบันฯ ไปบริษัท แบบอย่างง่าย (วาดจากภาพจับหน้าจอแผนที่ที่ผู้เขียนให้มา)
ใช้เฉพาะชื่อสถานที่ที่ปรากฏในภาพต้นฉบับ  ผลลัพธ์ picture/fig-1-01-simple.png"""
from PIL import Image, ImageDraw, ImageFont
import os

S = 4                                   # วาดใหญ่แล้วย่อ ให้เส้นเรียบ
W0, H0 = 560, 860                       # ขนาดหลังย่อ
FD = 'C:/Windows/Fonts/'
def font(sz, bold=False):
    return ImageFont.truetype(FD + ('THSarabunNew Bold.ttf' if bold else 'THSarabunNew.ttf'), int(sz * S))

img = Image.new('RGB', (W0 * S, H0 * S), (244, 242, 236))
d = ImageDraw.Draw(img)
def P(x, y):                            # พิกัดภาพต้นฉบับ 506x776 -> ผืนผ้าใบนี้
    return (x * 1.0 + 20) * S, (y * 1.0 + 40) * S
def L(pts, w, col):
    xy = [P(*p) for p in pts]
    d.line(xy, fill=col, width=int(w * S), joint='curve')
    for x, y in xy:
        d.ellipse((x - w * S / 2, y - w * S / 2, x + w * S / 2, y + w * S / 2), fill=col)

# น้ำและสวน
d.rounded_rectangle((*P(335, 485), *P(445, 585)), radius=30 * S, fill=(190, 220, 238))
d.rounded_rectangle((*P(385, 575), *P(506, 705)), radius=18 * S, fill=(205, 228, 200))
# คลอง
L([(0, 205), (60, 190), (150, 175), (195, 120)], 3, (200, 224, 240))
L([(0, 590), (120, 600), (250, 612)], 3, (200, 224, 240))
# ถนนรอง (ไม่ติดชื่อ)
minor = [[(0, 330), (260, 300), (506, 250)], [(0, 450), (270, 440)], [(70, 70), (110, 400), (150, 700)],
         [(275, 440), (506, 420)], [(0, 520), (260, 560)], [(300, 90), (330, 300)], [(120, 20), (190, 50)],
         [(330, 640), (506, 640)], [(275, 600), (506, 580)]]
for m in minor:
    L(m, 3, (252, 252, 250))
    L(m, 1.2, (226, 224, 218))

# ถนนหลักตามเส้นทาง (ขอบถนน)
route = [(45, 30), (195, 50), (205, 150), (235, 330), (265, 480), (275, 560), (262, 650), (253, 745), (50, 745)]
L(route, 12, (255, 255, 255))
L(route, 7, (36, 18, 200))
d_end = (0, 745)
L([(0, 745), (50, 745)], 7, (36, 18, 200))

def label(x, y, text, sz=17, bold=False, col=(60, 60, 60), anchor='la', halo=True):
    f = font(sz, bold)
    xy = P(x, y)
    if halo:
        for dx in (-1.5, 0, 1.5):
            for dy in (-1.5, 0, 1.5):
                d.text((xy[0] + dx * S / 2, xy[1] + dy * S / 2), text, font=f, fill=(244, 242, 236), anchor=anchor)
    d.text(xy, text, font=f, fill=col, anchor=anchor)

def pin(x, y, col, r=7):
    px, py = P(x, y)
    d.ellipse((px - (r + 3) * S, py - (r + 3) * S, px + (r + 3) * S, py + (r + 3) * S), fill=(255, 255, 255))
    d.ellipse((px - r * S, py - r * S, px + r * S, py + r * S), fill=col)

# จุดสนใจ
for (x, y, t, a) in [(205, 155, 'ธัญญาพาร์ค', 'la'), (270, 468, 'บีวายดี ศรีนครินทร์', 'la'),
                     (272, 545, 'โรงแรม MII ศรีนครินทร์', 'la'), (268, 608, 'พาราไดซ์ พาร์ค', 'la')]:
    pin(x + 14, y + 9, (214, 96, 44), 5)
    label(x + 26, y, t, 16, False, (110, 60, 30))
label(392, 600, 'สวนหลวง ร.9', 17, False, (50, 100, 60))
label(395, 535, '', 1)
label(210, 38, 'หัวหมาก', 16, False, (110, 110, 110))
label(20, 150, 'เขตสวนหลวง', 17, False, (110, 110, 110))

# ชื่อถนน
label(240, 250, 'ถนนศรีนครินทร์', 17, True, (36, 18, 200), 'la')
label(95, 722, 'ถนนเฉลิมพระเกียรติ ร.9', 17, True, (36, 18, 200), 'la')

# ต้นทาง ปลายทาง
pin(45, 30, (255, 255, 255), 9)
pin(45, 30, (60, 60, 60), 5)
label(60, 8, 'สถาบันเทคโนโลยีไทย-ญี่ปุ่น', 19, True, (40, 40, 40))
px, py = P(50, 745)
d.ellipse((px - 11 * S, py - 11 * S, px + 11 * S, py + 11 * S), fill=(255, 255, 255))
d.ellipse((px - 6 * S, py - 6 * S, px + 6 * S, py + 6 * S), fill=(200, 40, 40))
label(62, 768, 'บริษัท Lumas corp co., LTD.', 19, True, (170, 30, 30))

# ชื่อภาพ ลูกศรทิศเหนือ
nx, ny = 520 * S, 70 * S
d.polygon([(nx, ny - 22 * S), (nx - 9 * S, ny + 8 * S), (nx, ny), (nx + 9 * S, ny + 8 * S)], fill=(80, 80, 80))
d.text((nx, ny + 22 * S), 'N', font=font(18, True), fill=(80, 80, 80), anchor='mm')

out = img.resize((W0, H0), Image.LANCZOS)
os.makedirs('picture', exist_ok=True)
out.save('picture/fig-1-01-simple.png', dpi=(200, 200))
print('ok', out.size)
