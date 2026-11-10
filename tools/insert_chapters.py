# -*- coding: utf-8 -*-
"""
แทรกบทที่ 2–5 และเอกสารอ้างอิง (จาก md/final/) ลงรายงาน v2 โดยไม่แตะข้อความที่ผู้เขียนแก้เอง

ตำแหน่งที่แทรก  ต่อจากย่อหน้าสุดท้ายของบทที่ 1  ก่อนรอยต่อส่วนที่ปิดเนื้อเรื่อง (ก่อนภาคผนวก)
ทุกบทเป็นส่วน (section) ของตัวเอง  หน้าแรกของบทไม่แสดงเลขหน้าแต่นับหน้า  เลขหน้าเดินต่อจากบทที่ 1
ใช้ซ้ำได้  ถ้ามีบทที่ 2 อยู่แล้วจะลบช่วง "บทที่ 2 .. เอกสารอ้างอิง" ที่เคยแทรกแล้วแทรกใหม่
(ข้อความที่ผู้เขียนแก้ในช่วงนั้นจะหาย  ตัวเครื่องมือเตือนก่อนเสมอ)

วิธีใช้
  py tools/insert_chapters.py SRC DST            ทำบนสำเนา
  py tools/insert_chapters.py SRC --inplace      สำรองที่ report/สำรอง/ แล้วแก้ไฟล์เดิม (Word ต้องปิดไฟล์นั้น)
แล้วตามด้วย  tools/format_report.py <ไฟล์> --inplace  และ  tools/finish-docx.ps1
"""
import argparse
import copy
import datetime
import os
import re
import shutil
import sys

sys.argv_orig = list(sys.argv)
sys.argv = [sys.argv[0], '--final']            # md2docx อ่านโหมดตอน import
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import md2docx                                  # noqa: E402
sys.argv = sys.argv_orig
from docx import Document                       # noqa: E402
from docx.oxml.ns import qn                     # noqa: E402

sys.stdout.reconfigure(encoding='utf-8')
W = lambda t: qn('w:' + t)                      # noqa: E731
CHAPTERS = md2docx.CHAPTERS[1:]                 # บทที่ 2–5


def ptext(el):
    return ''.join(t.text or '' for t in el.iter(W('t')))


def has_sect(el):
    return el.tag == W('p') and el.find(W('pPr')) is not None and el.find(W('pPr')).find(W('sectPr')) is not None


def build_scratch():
    doc = Document()
    md2docx.build_styles(doc)
    sections = []                                # [(ชื่อ, [elements])]
    for idx, (num, name, fname) in enumerate(CHAPTERS, start=2):
        if not os.path.exists(os.path.join(md2docx.MD_CH, fname)):
            raise SystemExit('ไม่พบ md/final/%s' % fname)
        start = sum(1 for e in doc.element.body if e.tag != W('sectPr'))
        p = doc.add_paragraph(style='Heading 1')
        p.add_run(num)
        p.add_run().add_break()
        p.add_run(name)
        md2docx.render_chapter(doc, md2docx.load_chapter(fname), idx)
        sections.append((num, start))
    ref_start = sum(1 for e in doc.element.body if e.tag != W('sectPr'))
    md2docx.build_references(doc, md2docx.load_references())
    kids = [e for e in doc.element.body if e.tag != W('sectPr')]
    cut = [s for _, s in sections] + [ref_start]
    parts = [kids[a:b] for a, b in zip(cut, cut[1:] + [len(kids)])]
    return doc, parts


def style_map(scratch, target):
    by_name = {s.name: s.style_id for s in target.styles}
    m = {}
    for s in scratch.styles:
        if s.name in by_name:
            m[s.style_id] = by_name[s.name]
    return m


def run(src, dst):
    doc = Document(src)
    body = doc.element.body
    kids = list(body)
    sect_idx = [i for i, e in enumerate(kids) if has_sect(e)]
    if len(sect_idx) < 4:
        raise SystemExit('พบรอยต่อส่วน %d จุด (อย่างน้อย 4)  โครงไฟล์ไม่ตรงที่คาด' % len(sect_idx))
    last_body = sect_idx[-2]                    # รอยต่อที่ปิดส่วนเนื้อเรื่อง (ก่อนภาคผนวก)
    orig = kids[last_body]

    # ---- ถ้าเคยแทรกแล้ว  ลบช่วงเก่า  ใช้ sectPr ของรอยต่อแรกเป็นแบบของบทที่ 1
    # ต้องค้นหลังส่วนหน้าเล่ม (สารบัญมีบรรทัด "บทที่ 2 ..." ด้วย) และเป็นหัวบทจริง คือสั้นและไม่มีแท็บ
    first_ch2 = next((i for i, e in enumerate(kids) if sect_idx[1] < i < last_body and e.tag == W('p')
                      and ptext(e).startswith('บทที่ 2') and len(ptext(e)) < 90 and '	' not in ptext(e)), None)
    tmpl = copy.deepcopy(orig.find(W('pPr')).find(W('sectPr')))
    if first_ch2 is not None:
        brk = first_ch2 - 1
        while brk >= 0 and not has_sect(kids[brk]):
            brk -= 1
        tmpl = copy.deepcopy(kids[brk].find(W('pPr')).find(W('sectPr')))
        n = 0
        for e in kids[brk:last_body]:
            body.remove(e)
            n += 1
        print('ลบช่วงที่เคยแทรกไว้ %d องค์ประกอบ' % n)
        kids = list(body)
        last_body = next(i for i, e in enumerate(kids) if e is orig)

    # ---- สร้างเนื้อหา
    scratch, parts = build_scratch()
    smap = style_map(scratch, doc)
    for part in parts:
        for el in part:
            for tag, attr in ((W('pStyle'), W('val')), (W('tblStyle'), W('val')), (W('rStyle'), W('val'))):
                for s in el.iter(tag):
                    v = s.get(attr)
                    if v in smap:
                        s.set(attr, smap[v])

    # รูปที่ฝังมากับเนื้อหา (blip) ต้องย้ายไฟล์ภาพจากเอกสารชั่วคราวเข้าเอกสารจริง ไม่งั้นเหลือกรอบเปล่า
    import io
    A = '{http://schemas.openxmlformats.org/drawingml/2006/main}blip'
    RID = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}embed'
    moved = 0
    for part in parts:
        for el in part:
            for blip in el.iter(A):
                rid = blip.get(RID)
                blob = scratch.part.related_parts[rid].blob
                new_rid, _ = doc.part.get_or_add_image(io.BytesIO(blob))
                blip.set(RID, new_rid)
                moved += 1
    print('ย้ายรูปที่ฝังมา %d รูป' % moved)

    def sect_para(with_start):
        p = md2docx.OxmlElement('w:p')
        pPr = md2docx.OxmlElement('w:pPr')
        ind = md2docx.OxmlElement('w:ind')
        ind.set(W('firstLine'), '0')
        pPr.append(ind)
        sp = copy.deepcopy(tmpl)
        pg = sp.find(W('pgNumType'))
        if not with_start and pg is not None:
            sp.remove(pg)
        pPr.append(sp)
        p.append(pPr)
        return p

    # ---- ใส่ตามลำดับ  [ch1][รอยต่อ start=1][ch2][รอยต่อ][ch3]...[ch5][รอยต่อ][อ้างอิง][orig]
    pos = list(body).index(orig)
    new = [sect_para(True)]
    for k, part in enumerate(parts):
        new.extend(part)
        if k < len(parts) - 1:
            new.append(sect_para(False))
    for e in new:
        body.insert(pos, e)
        pos += 1
    # รอยต่อสุดท้าย (orig) ปิดส่วนเอกสารอ้างอิง  เลขหน้าต่อเนื่องและแสดงเลขทุกหน้า
    sp = orig.find(W('pPr')).find(W('sectPr'))
    for tag in ('pgNumType', 'titlePg'):
        e = sp.find(W(tag))
        if e is not None:
            sp.remove(e)
    doc.save(dst)
    return ['แทรกบทที่ 2–5 และเอกสารอ้างอิง  %d องค์ประกอบ' % len(new)]


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('src')
    ap.add_argument('dst', nargs='?')
    ap.add_argument('--inplace', action='store_true')
    a = ap.parse_args()
    if a.inplace:
        bdir = os.path.join(os.path.dirname(os.path.abspath(a.src)), 'สำรอง')
        os.makedirs(bdir, exist_ok=True)
        bak = os.path.join(bdir, os.path.splitext(os.path.basename(a.src))[0] + '-' +
                           datetime.datetime.now().strftime('%Y%m%d-%H%M%S') + '.docx')
        shutil.copy2(a.src, bak)
        print('สำรองไว้ที่:', bak)
        tmp = a.src + '.tmp'
        for l in run(a.src, tmp):
            print(' ', l)
        os.replace(tmp, a.src)
    else:
        if not a.dst:
            ap.error('ต้องระบุ DST หรือใช้ --inplace')
        for l in run(a.src, a.dst):
            print(' ', l)
    print('เสร็จ')
