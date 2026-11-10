# -*- coding: utf-8 -*-
"""แทนกรอบ "[ วางภาพที่ X.Y ที่นี่ ]" ด้วยภาพใน picture/ready/ แล้วลบบรรทัด "ชื่อไฟล์ที่เตรียมไว้"
ใช้กับไฟล์ที่ผ่านการจัดรูปเล่มแล้ว  สำรองก่อนเสมอ  ปิด Word ก่อน
วิธีใช้  py tools/insert_figures_1110.py SRC DST"""
import os, re, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf
from docx import Document
from docx.shared import Cm
from docx.text.paragraph import Paragraph
from docx.oxml import OxmlElement

W = cf.W
READY = os.path.join(os.path.dirname(HERE), 'picture', 'ready')
# ความกว้างภาพ (ซม.)  กว้างข้อความของหน้าคือ 14.65
WIDTH = {'1.1': 6.8, '2.1': 14.6, '2.2': 7.6, '2.3': 9.0, '3.3': 9.5, '3.4': 14.6,
         '4.9': 14.6, '4.10': 14.6, '4.12': 8.2, '4.13': 9.2, '4.14': 14.6}
DEFAULT = 14.0


def find_image(fig):
    base = 'fig-' + fig.replace('.', '-').replace('-', '-', 1)
    n = fig.split('.')
    base = 'fig-%s-%02d' % (n[0], int(n[1]))
    for ext in ('.png', '.jpg'):
        p = os.path.join(READY, base + ext)
        if os.path.exists(p):
            return p
    return None


def run(src, dst):
    doc = Document(src)
    body = doc.element.body
    log, missing = [], []
    for p in list(body.iter(W('p'))):
        t = cf.ptext(p)
        m = re.match(r'^\[ วางภาพที่ (\d+\.\d+) ที่นี่ \]$', t.strip())
        if not m:
            continue
        fig = m.group(1)
        img = find_image(fig)
        if img is None:
            missing.append(fig)
            continue
        nxt = p.getnext()
        if nxt is not None and cf.ptext(nxt).startswith('ชื่อไฟล์ที่เตรียมไว้'):
            body_remove = nxt
        else:
            body_remove = None
        for r in list(p):
            if r.tag != W('pPr'):
                p.remove(r)
        pPr = p.find(W('pPr'))
        if pPr is not None:
            p.remove(pPr)
        pPr = OxmlElement('w:pPr')
        kn = OxmlElement('w:keepNext'); pPr.append(kn)
        sp = OxmlElement('w:spacing'); sp.set(W('before'), '120'); pPr.append(sp)
        ind = OxmlElement('w:ind'); ind.set(W('firstLine'), '0'); pPr.append(ind)
        jc = OxmlElement('w:jc'); jc.set(W('val'), 'center'); pPr.append(jc)
        p.insert(0, pPr)
        par = Paragraph(p, doc._body)
        shape = par.add_run().add_picture(img, width=Cm(WIDTH.get(fig, DEFAULT)))
        shape._inline.docPr.set('descr', 'ภาพที่ ' + fig)
        shape._inline.docPr.set('name', os.path.basename(img))
        if body_remove is not None:
            body_remove.getparent().remove(body_remove)
        log.append('%s <- %s %.1fx%.1f cm' % (fig, os.path.basename(img), shape.width / 360000, shape.height / 360000))
    doc.save(dst)
    return log, missing


if __name__ == '__main__':
    lg, miss = run(sys.argv[1], sys.argv[2])
    for l in lg:
        print(' ', l)
    print('ภาพที่ไม่มีไฟล์:', miss)
