# -*- coding: utf-8 -*-
"""วาดรูปที่ Claude วาดเองได้ (แผนภาพล้วน ไม่ใช่ภาพจากหน้าจอ) ลง picture/  ใช้ TH Sarabun New
  fig-3-02.png  ขั้นตอนการดำเนินงานสี่ขั้น (บทที่ 3)
  fig-4-02.png  ทิศทางการพึ่งพาระหว่างโมดูล (บทที่ 4)
วิธีใช้  py tools/make_figures.py"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'picture')
FR = 'C:/Windows/Fonts/THSarabunNew.ttf'
FB = 'C:/Windows/Fonts/THSarabunNew Bold.ttf'


def font(size, bold=False):
    return ImageFont.truetype(FB if bold else FR, size)


def center(d, xy, text, f, fill='black'):
    x, y = xy
    w = d.textlength(text, font=f)
    d.text((x - w / 2, y), text, font=f, fill=fill)


def arrow(d, a, b, fill='black', width=4, head=16):
    d.line([a, b], fill=fill, width=width)
    import math
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    for da in (2.7, -2.7):
        d.line([b, (b[0] + head * math.cos(ang + da), b[1] + head * math.sin(ang + da))], fill=fill, width=width)


def fig_stages():
    W, H = 1800, 520
    im = Image.new('RGB', (W, H), 'white')
    d = ImageDraw.Draw(im)
    stages = [
        ('ขั้นที่ 1', 'ออกแบบแนวคิด\nและเนื้อหา', ['เนื้อเรื่องและธีมนาฬิกา', 'ปริศนาห้าองก์', 'ออกแบบห้อง 1, 4, 5, 2, 3']),
        ('ขั้นที่ 2', 'ออกแบบระบบเกม', ['แบ่งโมดูลตามหน้าที่', 'กำหนดส่วนต่อประสาน', 'ระบบนาฬิกาแบบชิ้นส่วน']),
        ('ขั้นที่ 3', 'พัฒนาโปรแกรม', ['ระบบผู้เล่นและโต้ตอบ', 'ระบบนาฬิกาและปริศนา', 'ปรับโครงสร้างและสร้างฉาก']),
        ('ขั้นที่ 4', 'ทดสอบการทำงาน', ['ชุดทดสอบ PlayMode 15 กรณี', 'เพื่อนทดลองเล่น 5 คน', 'แก้ไขข้อผิดพลาด']),
    ]
    n = len(stages)
    gap = 70
    bw = (W - 80 - gap * (n - 1)) // n
    fills = ['#E8EEF7', '#E8F3EA', '#FFF3DC', '#F6E8EE']
    for i, (a, b, items) in enumerate(stages):
        x0 = 40 + i * (bw + gap)
        d.rounded_rectangle([x0, 60, x0 + bw, 460], radius=22, fill=fills[i], outline='#444444', width=3)
        center(d, (x0 + bw / 2, 78), a, font(40, True), '#333333')
        for k, line in enumerate(b.split('\n')):
            center(d, (x0 + bw / 2, 126 + k * 50), line, font(46, True))
        y = 126 + len(b.split('\n')) * 50 + 24
        d.line([x0 + 30, y, x0 + bw - 30, y], fill='#888888', width=2)
        y += 16
        for t in items:
            center(d, (x0 + bw / 2, y), t, font(34))
            y += 48
        if i < n - 1:
            arrow(d, (x0 + bw + 8, 260), (x0 + bw + gap - 8, 260))
    im.save(os.path.join(OUT, 'fig-3-02.png'), dpi=(200, 200))


def fig_deps():
    W, H = 1800, 1380
    im = Image.new('RGB', (W, H), 'white')
    d = ImageDraw.Draw(im)
    box = {
        'Items': (450, 130, '#E8EEF7', 'EscapeRoom.Items'),
        'World': (1400, 130, '#E8EEF7', 'EscapeRoom.World'),
        'Interaction': (450, 420, '#E8F3EA', 'EscapeRoom.Interaction'),
        'Clock': (450, 710, '#FFF3DC', 'EscapeRoom.Clock'),
        'Puzzle': (1000, 710, '#FFF3DC', 'EscapeRoom.Puzzle'),
        'Player': (450, 1000, '#F6E8EE', 'Assembly-CSharp'),
        'UI': (1000, 1000, '#F6E8EE', 'Assembly-CSharp'),
        'Game': (450, 1260, '#F6E8EE', 'Assembly-CSharp'),
    }
    bw, bh = 360, 120
    fn, fs = font(48, True), font(30)
    for k, (x, y, c, a) in box.items():
        d.rounded_rectangle([x - bw / 2, y - bh / 2, x + bw / 2, y + bh / 2], radius=18, fill=c, outline='#444444', width=3)
        center(d, (x, y - 42), k, fn)
        center(d, (x, y + 14), a, fs, '#555555')

    def vert(a, b, dx=0, col='black'):
        ax, ay = box[a][0] + dx, box[a][1]
        bx, by = box[b][0] + dx, box[b][1]
        up = by < ay
        arrow(d, (ax, ay + (-bh / 2 if up else bh / 2)), (bx, by + (bh / 2 if up else -bh / 2)), col)
    vert('Interaction', 'Items')
    vert('Clock', 'Interaction')
    vert('Player', 'Clock')
    vert('Game', 'Player')
    # Puzzle -> Interaction (เฉียง)
    arrow(d, (1000 - bw / 2 + 30, 710 - bh / 2), (450 + bw / 2, 420 + 30), 'black')
    # UI -> Interaction (ผ่านขอบขวาของ Puzzle ด้านนอก) แสดงเป็นเส้นตรงจาก UI ขึ้นไป  ไม่ใช้ เพื่อให้อ่านง่าย
    # Player <-> UI  (วงจรที่ยอมรับ) สีแดง
    arrow(d, (450 + bw / 2, 975), (1000 - bw / 2, 975), '#C00000')
    arrow(d, (1000 - bw / 2, 1025), (450 + bw / 2, 1025), '#C00000')
    center(d, (725, 1040), 'วงจร Player-UI', font(32, True), '#C00000')
    # คำอธิบาย
    lx = 1240
    d.rounded_rectangle([lx, 330, 1760, 600], radius=14, outline='#999999', width=2)
    for k, (t, c) in enumerate([('ลูกศรชี้จากโมดูลที่ใช้', '#333333'), ('ไปยังโมดูลที่ถูกใช้', '#333333'),
                                ('กล่องสีเดียวกันอยู่ชั้นเดียวกัน', '#333333'),
                                ('เส้นสีแดง คือวงจรการพึ่งพา', '#C00000'), ('ที่มีอยู่จริงในโครงงาน', '#C00000')]):
        d.text((lx + 24, 346 + k * 48), t, font=font(34), fill=c)
    d.text((lx, 640), 'ไม่วาดเส้นไปยัง Items และ Interaction', font=font(30), fill='#555555')
    d.text((lx, 680), 'ของทุกโมดูล เพื่อให้อ่านง่าย', font=font(30), fill='#555555')
    im.save(os.path.join(OUT, 'fig-4-02.png'), dpi=(200, 200))


if __name__ == '__main__':
    fig_stages()
    fig_deps()
    print('วาดแล้ว:', os.listdir(OUT))
