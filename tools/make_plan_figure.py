# -*- coding: utf-8 -*-
"""วาดผังบ้านมุมบนจากข้อมูลใน HousePlan.asset  ผลลัพธ์ picture/fig-3-04.png
ใช้ข้อมูลห้อง ผนัง ประตู หน้าต่างจากไฟล์จริง ไม่ได้วัดจากภาพ  โปรเจกต์เกมอ่านอย่างเดียว
วิธีใช้  py tools/make_plan_figure.py [path HousePlan.asset]"""
import sys, os, re
import yaml
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
SRC = sys.argv[1] if len(sys.argv) > 1 else r'C:\Users\User\Desktop\work\project\EscapeRoom\Assets\Recipes\HousePlan.asset'
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
txt = open(SRC, encoding='utf-8').read()
txt = re.sub(r'^%.*\n', '', txt, flags=re.M)
txt = re.sub(r'^--- .*$', '---', txt, flags=re.M)
data = yaml.safe_load(txt)['MonoBehaviour']
rooms, walls = data['rooms'], data['walls']
DOOR, WIN = data['doorWidth'], data['windowWidth']

THAI = {'Room_I_Showroom': ('I', 'หน้าร้าน'), 'Room_II_Workshop': ('II', 'ห้องช่าง'), 'Room_III_Parlour': ('III', 'ห้องนั่งเล่น'),
        'Room_IV_Bedroom': ('IV', 'ห้องนอน'), 'Room_V_Behind': ('V', 'หลังตู้นาฬิกา')}
FD = 'C:/Windows/Fonts/'
def font(sz, bold=False):
    return ImageFont.truetype(FD + ('THSarabunNew Bold.ttf' if bold else 'THSarabunNew.ttf'), sz)

S = 3
W0, H0 = 1800, 1240
x_min, x_max = min(r['X0'] for r in rooms) - 0.6, max(r['X1'] for r in rooms) + 0.6
z_min, z_max = min(r['Z0'] for r in rooms) - 0.6, max(r['Z1'] for r in rooms) + 0.6
mx, mz = 70, 150                       # ขอบซ้ายขวา / บนล่าง (พิกเซลหลังย่อ)
scale = min((W0 - 2 * mx) / (x_max - x_min), (H0 - 2 * mz) / (z_max - z_min))
def X(x): return (mx + (x - x_min) * scale) * S
def Z(z): return (H0 - mz - (z - z_min) * scale) * S     # z ขึ้นบน

img = Image.new('RGB', (W0 * S, H0 * S), 'white')
d = ImageDraw.Draw(img)

# พื้นห้อง (สีจากแอสเซ็ต ผสมขาวให้อ่านป้ายได้)
for r in rooms:
    c = r['Floor']; col = tuple(int(255 * (0.35 + 0.65 * c[k] * 1.6 if c[k] * 1.6 < 1 else 1) ) for k in ('r', 'g', 'b'))
    col = tuple(min(255, int(235 - (1 - v / 255) * 0 )) for v in col)
    base = tuple(int(255 * c[k]) for k in ('r', 'g', 'b'))
    fill = tuple(int(235 * 0.62 + b * 0.38) for b in base)
    d.rectangle([X(r['X0']), Z(r['Z1']), X(r['X1']), Z(r['Z0'])], fill=fill)

WALLC, WALLW = (40, 40, 40), 9 * S
def seg(a, b, col=WALLC, w=WALLW):
    d.line([a, b], fill=col, width=int(w))
for wl in walls:
    fixed, a, b = wl['Fixed'], wl['From'], wl['To']
    ops = sorted(wl.get('Openings') or [], key=lambda o: o['Centre'])
    cur = a
    def pt(u, v=None):
        return (X(u), Z(fixed)) if wl['AlongX'] else (X(fixed), Z(u))
    for o in ops:
        half = (DOOR if o['Kind'] == 0 else WIN) / 2
        seg(pt(cur), pt(o['Centre'] - half))
        cur = o['Centre'] + half
    seg(pt(cur), pt(b))
    for o in ops:
        half = (DOOR if o['Kind'] == 0 else WIN) / 2
        p0, p1 = pt(o['Centre'] - half), pt(o['Centre'] + half)
        if o['Kind'] == 1:
            seg(p0, p1, (70, 140, 200), 5 * S)                # หน้าต่าง
        else:
            seg(p0, p1, (255, 255, 255), 9 * S)               # ช่องประตู
            seg(p0, p1, (200, 120, 40), 2 * S)

def txt_c(x, z, s, f, fill=(30, 30, 30)):
    w = d.textlength(s, font=f)
    d.text((X(x) - w / 2, Z(z)), s, font=f, fill=fill)
for r in rooms:
    n, th = THAI[r['Name']]
    cx, cz = (r['X0'] + r['X1']) / 2, (r['Z0'] + r['Z1']) / 2
    f1, f2, f3 = font(100 * S // 2, True), font(72 * S // 2, True), font(52 * S // 2)
    txt_c(cx, cz + 0.85, 'ห้อง ' + n if False else n, f1)
    txt_c(cx, cz - 0.05, th, f2)
    txt_c(cx, cz - 0.95, '%g × %g ม.' % (round(r['X1'] - r['X0'], 2), round(r['Z1'] - r['Z0'], 2)), f3, (70, 70, 70))
    if r['Roofless']:
        txt_c(cx, cz - 1.65, '(โดมกระจก ไม่มีเพดาน)', f3, (70, 70, 70))

# ลำดับเส้นทาง I → II → III → IV → V
order = ['Room_I_Showroom', 'Room_II_Workshop', 'Room_III_Parlour', 'Room_IV_Bedroom', 'Room_V_Behind']
cen = {r['Name']: ((r['X0'] + r['X1']) / 2, (r['Z0'] + r['Z1']) / 2) for r in rooms}

# ชื่อภาพ ทิศ แถบมาตราส่วน คำอธิบายสัญลักษณ์
f = font(64 * S // 2, True)
d.text((mx * S, 40 * S), 'ผังบ้านทั้งห้าห้อง (มุมมองจากด้านบน) สร้างจากข้อมูลใน HousePlan.asset', font=f, fill=(20, 20, 20))
x0, y0 = mx * S, (H0 - 95) * S
d.line([x0, y0, x0 + 5 * scale * S, y0], fill=(30, 30, 30), width=4 * S)
for k in (0, 5):
    d.line([x0 + k * scale * S, y0 - 8 * S, x0 + k * scale * S, y0 + 8 * S], fill=(30, 30, 30), width=3 * S)
d.text((x0 + 5 * scale * S + 14 * S, y0 - 16 * S), '5 เมตร', font=font(50 * S // 2), fill=(30, 30, 30))
lx = (W0 - 760) * S
ly = (H0 - 120) * S
fs = font(48 * S // 2)
d.rectangle([lx, ly, lx + 46 * S, ly + 12 * S], fill=(70, 140, 200)); d.text((lx + 58 * S, ly - 12 * S), 'หน้าต่าง', font=fs, fill=(30, 30, 30))
d.rectangle([lx + 220 * S, ly, lx + 266 * S, ly + 12 * S], fill=(200, 120, 40)); d.text((lx + 278 * S, ly - 12 * S), 'ช่องประตู', font=fs, fill=(30, 30, 30))
d.rectangle([lx + 450 * S, ly - 6 * S, lx + 496 * S, ly + 18 * S], fill=(40, 40, 40)); d.text((lx + 508 * S, ly - 12 * S), 'ผนัง', font=fs, fill=(30, 30, 30))
# ลูกศรทิศเหนือ (+z)
nx, ny = (W0 - 90) * S, 100 * S
d.polygon([(nx, ny - 40 * S), (nx - 16 * S, ny + 12 * S), (nx, ny), (nx + 16 * S, ny + 12 * S)], fill=(60, 60, 60))
d.text((nx - 12 * S, ny + 18 * S), '+z', font=font(48 * S // 2, True), fill=(60, 60, 60))

out = img.resize((W0, H0), Image.LANCZOS)
out.save(os.path.join(ROOT, 'picture', 'fig-3-04.png'), dpi=(200, 200))
print('ok', out.size, len(rooms), 'rooms', len(walls), 'walls')
