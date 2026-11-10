# -*- coding: utf-8 -*-
"""
จัดฟอร์มทั้งเล่มของรายงานสหกิจศึกษา ให้ตรงแบบฟอร์มคณะวิศวะ TNI  โดยไม่เปลี่ยนถ้อยคำ

ทำงานจาก "ข้อความ" ของแต่ละย่อหน้า ไม่พึ่งสไตล์เดิม  จึงใช้ได้แม้ผู้เขียนกด Clear All Formatting
ไปแล้ว  (สไตล์ทุกอันกลายเป็น Normal เลขข้อและการตัดหน้าหาย)  และใช้ซ้ำได้ทุกครั้งที่ฟอร์แมตเพี้ยน

ข้อกำหนดที่ใช้ (วัดจาก PDF ของแบบฟอร์มจริง  report/exam_report/รูปเล่มสหกิจสำหรับคณะวิศวะ.pdf)
  ขอบ บน-ซ้าย 1.5" ล่าง-ขวา 1"  (ตั้งไว้ที่ส่วนของเอกสารอยู่แล้ว ไม่แตะ)
  TH Sarabun New ทั้งเล่ม สีดำ
  ปก                       ตัวหนา 18 กึ่งกลาง
  หน้าอนุมัติ / แถวหัวบทสรุป   16 ธรรมดา แท็บ 6.0 ซม. เยื้อง hanging
  หัวบทสรุป ฯลฯ            ตัวหนา 18 กึ่งกลาง
  ชื่อบท                    ตัวหนา 20 กึ่งกลาง   หัวข้อ x.x ตัวหนา 18   x.x.x ตัวหนา 16
  เนื้อความ                 16 ธรรมดา ชิดสองข้าง เยื้องบรรทัดแรก 1.5 ซม.
  คำบรรยายรูปอยู่ใต้รูปจัดกลาง  คำบรรยายตารางอยู่บนชิดซ้าย

ตัวตรวจ  ก่อนบันทึกเทียบข้อความทั้งเล่ม  ต้องเหมือนเดิมทุกตัวอักษร  ถ้าไม่ผ่านจะไม่บันทึก

วิธีใช้
  py tools/format_report.py SRC DST          ทำบนสำเนา
  py tools/format_report.py SRC --inplace    สำรองไว้ที่ report/สำรอง/ แล้วแก้ไฟล์เดิม  (Word ต้องปิดไฟล์นั้น)
แล้วตามด้วย  tools/finish-docx.ps1  เพื่ออัปเดตสารบัญและเลขหน้า
"""
import argparse
import datetime
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from docx import Document                       # noqa: E402
from docx.enum.text import WD_ALIGN_PARAGRAPH as AL  # noqa: E402
from docx.oxml import OxmlElement               # noqa: E402
from docx.oxml.ns import qn                     # noqa: E402
from docx.shared import Cm, Pt, RGBColor       # noqa: E402
import md2docx                                  # noqa: E402

FONT = 'TH Sarabun New'
ZW = '​'
RED = 'C00000'
ROW_TAB = 3402            # 6.0 ซม.  กว้างพอสำหรับ "อาจารย์ที่ปรึกษาสหกิจศึกษา"
TEXT_W = 8306             # ความกว้างบรรทัด 14.65 ซม.
LIST_LEFT, LIST_HANG = Cm(2.2), Cm(-0.7)
SIGN_LEFT = Cm(2.5)

RE_PLACEHOLDER = re.compile(r'\[ [^\]\n]{2,140} \]')
RE_H2 = re.compile(r'^\d+\.\d{1,2}\s+\S')
RE_H3 = re.compile(r'^\d+\.\d+\.\d+\s+\S')
RE_LIST = re.compile(r'^\d+\.\t')
RE_CAP_FIG = re.compile(r'^รูปที่\s+\d+\.\d+\s')
RE_CAP_TBL = re.compile(r'^ตารางที่\s+\d+\.\d+\s')
RE_TERM = re.compile(r'^(\d+\.\t)(.+?)( หมายถึง .*)$', re.S)


def txt(p):
    return p.text.replace(ZW, '')


def norm(s):
    return re.sub(r'\s+', '', s.replace(ZW, ''))


def W(t):
    return qn('w:' + t)


# =============================================================== เครื่องมือ
def has_field(p):
    return any(True for _ in p._p.iter(W('fldChar'))) or any(True for _ in p._p.iter(W('fldSimple')))


def has_br(p):
    return any(True for _ in p._p.iter(W('br')))


def reset(doc, p, style='Normal'):
    """ล้างการจัดย่อหน้าและรูปแบบตัวอักษรที่ค้าง  คง sectPr / ฟิลด์ / ข้อความ"""
    p.style = doc.styles[style]
    pPr = p._p.get_or_add_pPr()
    for tag in ('ind', 'spacing', 'jc', 'tabs', 'keepNext', 'keepLines', 'pageBreakBefore',
                'numPr', 'outlineLvl', 'rPr', 'contextualSpacing', 'shd', 'pBdr'):
        for e in pPr.findall(W(tag)):
            pPr.remove(e)
    for r in p._p.iter(W('r')):
        rPr = r.find(W('rPr'))
        if rPr is not None:
            r.remove(rPr)


def flat(p, left=None, first=0):
    """ไม่เยื้องบรรทัดแรก (ค่าตั้งต้นของ Normal คือเยื้อง 1.5 ซม.)"""
    pf = p.paragraph_format
    pf.first_line_indent = Cm(first) if not hasattr(first, 'cm') else first
    if left is not None:
        pf.left_indent = left


def tabs(p, stops):
    pPr = p._p.get_or_add_pPr()
    for e in pPr.findall(W('tabs')):
        pPr.remove(e)
    el = md2docx._set(pPr, 'w:tabs')
    for pos, kind in stops:
        t = OxmlElement('w:tab')
        t.set(W('val'), kind)
        t.set(W('pos'), str(pos))
        el.append(t)


def page_break_before(p):
    p.paragraph_format.page_break_before = True


def fmt_runs(p, size=None, bold=None, color=None):
    """ตั้งขนาด(half-points)/ตัวหนา/สี ให้ทุก run ของย่อหน้า"""
    for r in p._p.iter(W('r')):
        rPr = r.find(W('rPr'))
        if rPr is None:
            rPr = OxmlElement('w:rPr')
            r.insert(0, rPr)
        if size is not None:
            md2docx._set(rPr, 'w:sz', val=str(size))
            md2docx._set(rPr, 'w:szCs', val=str(size))
        if bold is not None:
            for tag in ('b', 'bCs'):
                for e in rPr.findall(W(tag)):
                    rPr.remove(e)
            if bold:
                md2docx._set(rPr, 'w:b', val='1')
                md2docx._set(rPr, 'w:bCs', val='1')
        if color is not None:
            md2docx._set(rPr, 'w:color', val=color)
        if len(rPr) == 0:
            r.remove(rPr)


def rebuild(p, spans):
    """สร้าง run ใหม่ให้ย่อหน้าตามช่วงตัวอักษร  spans = [(start, end, {bold, color})]
    ใช้เฉพาะย่อหน้าที่ไม่มีฟิลด์ / ตัวแบ่งบรรทัด"""
    if has_field(p) or has_br(p):
        return False
    s = p.text
    cuts = sorted({0, len(s)} | {a for a, b, _ in spans} | {b for a, b, _ in spans})
    for r in list(p._p.findall(W('r'))):
        p._p.remove(r)
    for a, b in zip(cuts, cuts[1:]):
        if a == b:
            continue
        props = {}
        for sa, sb, pr in spans:
            if sa <= a and b <= sb:
                props.update(pr)
        run = p.add_run(s[a:b])
        rPr = None
        if props.get('bold'):
            rPr = run._r.get_or_add_rPr()
            md2docx._set(rPr, 'w:b', val='1')
            md2docx._set(rPr, 'w:bCs', val='1')
        if props.get('color'):
            rPr = run._r.get_or_add_rPr()
            md2docx._set(rPr, 'w:color', val=props['color'])
    return True


def row(p):
    """แถว  ป้าย⇥ค่า  แท็บ 6 ซม. ค่าที่ขึ้นสองบรรทัดชิดใต้ค่าเดิม"""
    p.alignment = AL.LEFT
    flat(p, left=Cm(6.0), first=Cm(-6.0))
    tabs(p, [(ROW_TAB, 'left')])


def is_row(t):
    return t.count('\t') == 1 and 0 < len(t.split('\t')[0]) <= 32 and not re.match(r'^\d', t)


# ================================================================ สไตล์
def fix_styles(doc):
    el = doc.styles.element
    md2docx.strip_theme_fonts(el)
    rf = el.find(qn('w:docDefaults')).find(qn('w:rPrDefault')).find(qn('w:rPr')).find(qn('w:rFonts'))
    for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
        rf.set(qn('w:' + a), FONT)

    def style(name):
        try:
            return doc.styles[name]
        except KeyError:
            return None

    # ฟอนต์ระบุชัดทุกสไตล์ที่ใช้ ไม่พึ่งค่าตั้งต้น
    for s in doc.styles:
        if s.type != 1:
            continue
        n = s.name.lower()
        if n.startswith(('toc', 'table of figures', 'caption', 'normal', 'heading', 'header', 'footer', 'list paragraph')):
            rPr = s.element.get_or_add_rPr()
            f = md2docx._set(rPr, 'w:rFonts')
            for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
                f.set(qn('w:' + a), FONT)

    # หัวข้อเป็นสีน้ำเงินตามแม่แบบ ต้องเป็นสีดำ  ขนาดตามแบบฟอร์ม 20 / 18 / 16 หนา
    for name, pt in (('Heading 1', 20), ('Heading 2', 18), ('Heading 3', 16)):
        s = style(name)
        if s is None:
            continue
        s.font.size = Pt(pt)
        s.font.bold = True
        s.font.color.rgb = RGBColor(0, 0, 0)
        for e in s.element.get_or_add_rPr().findall(W('szCs')):
            e.set(W('val'), str(pt * 2))
    h3 = style('Heading 3')
    if h3 is not None:
        h3.paragraph_format.left_indent = Cm(1.27)
        h3.paragraph_format.first_line_indent = Cm(0)
    n = style('Normal')
    n.font.size = Pt(16)
    n.font.bold = False
    n.paragraph_format.alignment = AL.JUSTIFY
    n.paragraph_format.first_line_indent = Cm(1.5)
    n.paragraph_format.space_after = Pt(0)
    n.paragraph_format.space_before = Pt(0)
    n.paragraph_format.line_spacing = 1.0
    for cap, al in (('CaptionFigure', AL.CENTER), ('CaptionTable', AL.LEFT)):
        s = style(cap)
        if s is not None:
            s.font.size = Pt(16)
            s.font.bold = False
            s.paragraph_format.alignment = al
            s.paragraph_format.first_line_indent = Cm(0)


# ================================================================ งานหลัก
def run(src, dst):
    doc = Document(src)
    P = list(doc.paragraphs)
    before = [txt(p) for p in P]
    before_tbl = [txt(p) for t in doc.tables for rw in t.rows for c in rw.cells for p in c.paragraphs]
    log = []
    fix_styles(doc)

    sect = [i for i, p in enumerate(P) if p._p.pPr is not None and p._p.pPr.find(W('sectPr')) is not None]
    if len(sect) < 4:
        raise SystemExit('พบรอยต่อส่วน %d จุด (คาดอย่างน้อย 4)  โครงไฟล์ไม่ตรงที่คาด  หยุดทำงาน' % len(sect))
    s1, s2, s3, s4 = sect[0], sect[1], sect[-2], sect[-1]

    # บทที่ 2–5 + เอกสารอ้างอิง ที่ tools/insert_chapters.py สร้างไว้แล้ว  จัดรูปครบมาจากตัวสร้าง
    # (ตัวหนา ตัวเอียง โค้ด ช่องสีแดง ย่อหน้าของรายการ ตาราง)  ตัวจัดฟอร์มนี้ไม่ล้างรูปแบบของช่วงนั้น
    gen_lo = next((i for i in range(s2 + 1, s3) if re.match(r'^บทที่ 2', txt(P[i]).strip())
                   and len(txt(P[i]).strip()) < 90), None)
    gen_ids = set()
    gen_tbls = set()
    if gen_lo is not None:
        gen_ids = {id(P[i]._p) for i in range(gen_lo, s3 + 1)}
        seen = False
        for ch in doc.element.body:
            if ch is P[gen_lo]._p:
                seen = True
            if ch is P[s3]._p:
                break
            if seen and ch.tag == W('tbl'):
                gen_tbls.add(id(ch))
        log.append('พบบทที่ 2–5 ที่แทรกไว้แล้ว (ย่อหน้า %d–%d)  คงรูปแบบของตัวสร้าง' % (gen_lo, s3))

    for p in P:
        if id(p._p) in gen_ids:
            continue
        reset(doc, p, 'Normal')
        flat(p)                                  # ค่าตั้งต้น ไม่เยื้อง  ย่อหน้าเนื้อความตั้งเยื้องเอง

    def body(p):
        p.alignment = AL.JUSTIFY
        p.paragraph_format.first_line_indent = Cm(1.5)

    def find(pred, lo, hi, what):
        for i in range(lo, hi + 1):
            if pred(P[i]):
                return i
        raise SystemExit('หาจุดยึด "%s" ไม่พบ  หยุดทำงาน' % what)

    def exact(s):
        return lambda p: txt(p).strip() == s

    # ---------------------------------------------------------- ปก + หน้าอนุมัติ
    ap = find(lambda p: txt(p).startswith('ชื่อโครงงาน\t'), 0, s1, 'แถวแรกหน้าอนุมัติ')
    for i in range(0, ap):
        if txt(P[i]).strip():
            P[i].alignment = AL.CENTER
            fmt_runs(P[i], size=36, bold=True)
    page_break_before(P[ap])
    for i in range(ap, s1 + 1):
        p, t = P[i], txt(P[i])
        if is_row(t):
            row(p)
        elif t.startswith('คณะวิศวกรรมศาสตร์ สถาบัน'):
            body(p)
        elif t.startswith(('....', '(....', 'วันที่ ....')):
            p.alignment = AL.LEFT
            flat(p, left=SIGN_LEFT)
        elif t.startswith('คณะกรรมการสอบ'):
            p.alignment = AL.LEFT
            fmt_runs(p, bold=True)
        elif t.startswith('ลิขสิทธิ์'):
            p.alignment = AL.RIGHT
    log.append('ปก (หนา 18 กึ่งกลาง) + หน้าอนุมัติ (แถว/ลายเซ็น/ลิขสิทธิ์) ตามแบบฟอร์ม')

    # ---------------------------------------------------------- บทสรุป → สารบัญรูป
    lo, hi = s1 + 1, s2
    h_abs = find(exact('บทสรุป'), lo, hi, 'หัว บทสรุป')
    h_sum = find(exact('Summary'), h_abs, hi, 'หัว Summary')
    h_ack = find(exact('กิตติกรรมประกาศ'), h_sum, hi, 'หัว กิตติกรรมประกาศ')
    h_toc = find(exact('สารบัญ'), h_ack, hi, 'หัว สารบัญ')
    h_tbl = find(exact('สารบัญตาราง'), h_toc, hi, 'หัว สารบัญตาราง')
    h_fig = find(exact('สารบัญรูป'), h_tbl, hi, 'หัว สารบัญรูป')

    for i in range(lo, h_abs):                          # แถวหัวบทสรุป
        if is_row(txt(P[i])):
            row(P[i])
    eng = find(lambda p: txt(p).startswith('Title of Project\t'), h_abs, h_sum, 'แถว Title of Project')
    for i in range(h_abs + 1, eng):                     # เนื้อบทสรุป
        if txt(P[i]).strip():
            body(P[i])
    page_break_before(P[eng])
    for i in range(eng, h_sum):                         # แถวหัว Summary
        if is_row(txt(P[i])):
            row(P[i])
    for i in range(h_sum + 1, h_ack):                   # เนื้อ Summary
        if txt(P[i]).strip():
            body(P[i])

    for hd, pbb in ((h_abs, False), (h_sum, False), (h_ack, True)):
        p = P[hd]
        p.alignment = AL.CENTER
        fmt_runs(p, size=36, bold=True)
        if hd != h_ack:
            p.paragraph_format.space_before = Pt(18)
        if pbb:
            page_break_before(p)
    # กิตติกรรมประกาศ  เนื้อ 2 ย่อหน้า + ชื่อผู้เขียนท้าย
    ack_items = [i for i in range(h_ack + 1, h_toc) if txt(P[i]).strip()]
    for i in ack_items[:-1]:
        body(P[i])
    sign = P[ack_items[-1]]
    sign.alignment = AL.LEFT
    flat(sign, left=Cm(9.5))
    P[ack_items[-1]].paragraph_format.space_before = Pt(12)

    for hd in (h_toc, h_tbl, h_fig):                    # หัวสารบัญ 3 ชุด  ขึ้นหน้าใหม่
        p = P[hd]
        p.alignment = AL.CENTER
        fmt_runs(p, size=36, bold=True)
        page_break_before(p)
    right = [(TEXT_W, 'right')]
    for i in range(h_toc + 1, s2 + 1):
        p, t = P[i], txt(P[i])
        if t.strip() == 'หน้า':
            p.alignment = AL.RIGHT
            fmt_runs(p, bold=True)
        elif t.strip() == 'บทที่':
            p.alignment = AL.LEFT
            fmt_runs(p, bold=True)
        elif re.match(r'^(ตารางที่|รูปที่)\t', t):
            p.alignment = AL.LEFT
            tabs(p, right)
            fmt_runs(p, bold=True)
        elif has_field(p) or '\t' in t:
            p.alignment = AL.LEFT
            tabs(p, right)
    log.append('บทสรุป / Summary / กิตติกรรมประกาศ / สารบัญ 3 ชุด: หัวหนา 18 · เนื้อ 16 ชิดสองข้าง · ตัดหน้าก่อนแต่ละหัว')

    # ---------------------------------------------------------- บทที่ 1
    c_lo, c_hi = s2 + 1, (gen_lo - 1 if gen_lo is not None else s3)
    items = [i for i in range(c_lo, c_hi + 1)]
    kind = {}
    h18 = h19 = None
    for i in items:
        t = txt(P[i]).strip()
        if not t:
            continue
        if re.match(r'^บทที่ \d', t) and len(t) < 40:
            kind[i] = 'h1'
        elif RE_H3.match(t) and len(t) < 100:
            kind[i] = 'h3'
        elif RE_H2.match(t) and len(t) < 100 and '\t' not in t:
            kind[i] = 'h2'
            if t.startswith('1.8 '):
                h18 = i
            if t.startswith('1.9 '):
                h19 = i
        elif RE_CAP_FIG.match(t):
            kind[i] = 'capfig'
        elif RE_CAP_TBL.match(t):
            kind[i] = 'captbl'
        elif RE_LIST.match(t):
            kind[i] = 'list'
        else:
            kind[i] = 'body'
    # 1.8 / 1.9  รายการเลขอัตโนมัติ  (ย่อหน้าแรกของ 1.8 คือย่อหน้านำ)
    nxt_head = lambda h: next(j for j in sorted(kind) if j > h and kind[j] in ('h2', 'h1'))
    auto = []
    if h18 is not None:
        e = nxt_head(h18)
        its = [j for j in sorted(kind) if h18 < j < e]
        auto += [(j, '10') for j in its[1:]]
    if h19 is not None:
        e = nxt_head(h19)
        auto += [(j, '12') for j in sorted(kind) if h19 < j < e]
    for j, nid in auto:
        kind[j] = 'auto'

    order = sorted(kind)
    for k, i in enumerate(order):
        p, kd = P[i], kind[i]
        if kd == 'h1':
            reset(doc, p, 'Heading 1')
        elif kd == 'h2':
            reset(doc, p, 'Heading 2')
        elif kd == 'h3':
            reset(doc, p, 'Heading 3')
        elif kd == 'capfig':
            reset(doc, p, 'CaptionFigure')
        elif kd == 'captbl':
            reset(doc, p, 'CaptionTable')
        elif kd == 'list':
            p.alignment = AL.JUSTIFY
            flat(p, left=LIST_LEFT, first=LIST_HANG)
            tabs(p, [(1247, 'left')])
        elif kd == 'auto':
            p.alignment = AL.JUSTIFY
            flat(p, left=LIST_LEFT, first=LIST_HANG)
            nid = dict(auto)[i]
            pPr = p._p.get_or_add_pPr()
            num = md2docx._set(pPr, 'w:numPr')
            il = OxmlElement('w:ilvl'); il.set(W('val'), '0')
            ni = OxmlElement('w:numId'); ni.set(W('val'), nid)
            num.append(il); num.append(ni)
        else:
            body(p)
    # ย่อหน้าคั่นกลางรายการ (ต่อจากข้อก่อนหน้า) ให้เยื้องตรงข้อความของรายการ
    for a, b, c in zip(order, order[1:], order[2:]):
        if kind[a] == 'list' and kind[b] == 'body' and kind[c] == 'list':
            P[b].alignment = AL.JUSTIFY
            flat(P[b], left=LIST_LEFT, first=0)
    # บรรทัดสั้นที่นำเข้ารายการ (เช่น "การให้บริการหลักของบริษัทประกอบด้วย") เป็นตัวหนาตามเล่มตัวอย่าง
    for a, b in zip(order, order[1:]):
        if kind[a] == 'body' and kind[b] == 'list' and len(txt(P[a])) < 70                 and not txt(P[a]).rstrip().endswith('ดังนี้'):
            fmt_runs(P[a], bold=True)
    # กล่องรอรูปในตาราง
    for t in doc.tables:
        if id(t._tbl) in gen_tbls:
            continue
        for rw in t.rows:
            for c in rw.cells:
                for cp in c.paragraphs:
                    reset(doc, cp, 'Normal')
                    flat(cp)
                    cp.alignment = AL.CENTER
                    s = txt(cp)
                    if s.startswith('[ วาง'):
                        fmt_runs(cp, size=28, bold=True, color='808080')
                    elif s.startswith('ชื่อไฟล์'):
                        fmt_runs(cp, size=24, color='A0A0A0')
    log.append('บทที่ 1: ชื่อบท 20 · หัวข้อ 18 · รายการเลขข้อเยื้อง hanging · เลขอัตโนมัติ 1.8 / 1.9 กลับมา · คำบรรยาย')

    # ---------------------------------------------------------- ภาคผนวก
    a_lo, a_hi = s3 + 1, s4
    i_app = find(exact('ภาคผนวก'), a_lo, a_hi, 'หน้าคั่น ภาคผนวก')
    i_ka = find(exact('ภาคผนวก ก.'), i_app, a_hi, 'ภาคผนวก ก.')
    i_wk = find(exact('รายงานปฏิบัติงานประจำสัปดาห์'), i_ka, a_hi, 'รายงานปฏิบัติงานประจำสัปดาห์')
    for i in range(a_lo, a_hi + 1):
        P[i].alignment = AL.CENTER
    reset(doc, P[i_app], 'Heading 1')                   # หัวระดับ 1 เพื่อให้ภาคผนวกขึ้นในสารบัญ
    P[i_app].alignment = AL.CENTER
    fmt_runs(P[i_app], size=40, bold=True)
    for i in (i_ka, i_wk):
        fmt_runs(P[i], size=36, bold=True)
    page_break_before(P[i_app + 1])                     # หน้า "ภาคผนวก ก." เริ่มจากบรรทัดว่างถัดจากหน้าคั่น
    page_break_before(P[i_wk + 1])                      # หน้าที่ต้องแทรกรายงานรายสัปดาห์

    # ---------------------------------------------------------- ประวัติผู้จัดทำ
    b_lo = s4 + 1
    i_bio = find(exact('ประวัติผู้จัดทำโครงงาน'), b_lo, len(P) - 1, 'หัว ประวัติผู้จัดทำโครงงาน')
    reset(doc, P[i_bio], 'Heading 1')
    for i in range(i_bio + 1, len(P)):
        p, t = P[i], txt(P[i])
        if not t.strip():
            continue
        p.alignment = AL.LEFT
        if t.strip() == 'ประวัติการศึกษา':
            continue
        if t.startswith(('ระดับมัธยมศึกษา', 'ระดับอุดมศึกษา', '\t')):
            flat(p, left=Cm(1.2))
            tabs(p, [(ROW_TAB, 'left')])
        elif '\t' in t:
            row(p)
        else:
            flat(p, left=Cm(6.0))                       # บรรทัดชื่อสถานศึกษา ใต้ค่า
    log.append('ภาคผนวก (หน้าคั่น 20 / 18, ตัดหน้า) + ประวัติผู้จัดทำ (แถวแท็บ 6 ซม.)')

    # ---------------------------------------------------------- ตัวหนาเฉพาะที่ และช่องสีแดง
    in_table = {id(cp._p) for t in doc.tables for rw in t.rows for c in rw.cells for cp in c.paragraphs}
    for p in doc.paragraphs:
        if id(p._p) in in_table or id(p._p) in gen_ids:
            continue
        s, spans = p.text, []
        for m in RE_PLACEHOLDER.finditer(s):
            spans.append((m.start(), m.end(), {'bold': True, 'color': RED}))
        m = RE_TERM.match(s)
        if m and not has_field(p):                      # หัวข้อ 1.10  ศัพท์เป็นตัวหนา
            spans.append((m.start(2), m.end(2), {'bold': True}))
        k = s.find('ผู้ช่วยพัฒนาเกม (Game Developer intern)')
        if k >= 0:
            spans.append((k, k + len('ผู้ช่วยพัฒนาเกม (Game Developer intern)'), {'bold': True}))
        if spans:
            # ถ้าย่อหน้ามีตัวหนาทั้งย่อหน้าอยู่แล้ว (บรรทัดนำรายการ) ให้รักษาไว้
            was_bold = all(r.find(W('rPr')) is not None and r.find(W('rPr')).find(W('b')) is not None
                           for r in p._p.findall(W('r')) if (r.findtext(W('t')) or '').strip())
            if was_bold and p._p.findall(W('r')):
                spans.append((0, len(s), {'bold': True}))
            rebuild(p, spans)
    log.append('ช่อง [ ... ] เป็นตัวหนาสีแดง · ศัพท์ใน 1.10 เป็นตัวหนา · ตำแหน่งผู้ช่วยพัฒนาเกมใน 1.4 เป็นตัวหนา')

    # ---------------------------------------------------------- แฟล็กอักษรซับซ้อน
    # run ที่มีอักษรไทยต้องมี <w:cs/>  ไม่อย่างนั้น Word ไม่ตัดคำไทยด้วยพจนานุกรม
    # ข้อความยาว ๆ ถูกยกทั้งก้อนไปบรรทัดใหม่ บรรทัดสั้นกุด ย่อหน้ายาวขึ้นหลายบรรทัด และชื่อยาวถูกตัดกลางคำ
    thai = re.compile('[฀-๿]')
    flagged = 0
    all_p = list(doc.paragraphs) + [cp for t in doc.tables for rw in t.rows for c in rw.cells for cp in c.paragraphs]
    for p in all_p:
        for r in p._p.iter(W('r')):
            if thai.search(''.join(t.text or '' for t in r.iter(W('t')))):
                rPr = r.find(W('rPr'))
                if rPr is None:
                    rPr = OxmlElement('w:rPr')
                    r.insert(0, rPr)
                md2docx._set(rPr, 'w:cs')
                flagged += 1
    log.append('ติดแฟล็กอักษรซับซ้อน (w:cs) ให้ %d run ที่มีอักษรไทย เพื่อให้ Word ตัดคำไทยได้' % flagged)

    # ---------------------------------------------------------- ตัวตรวจ
    after = [txt(p) for p in doc.paragraphs]
    after_tbl = [txt(p) for t in doc.tables for rw in t.rows for c in rw.cells for p in c.paragraphs]
    a, b = norm(''.join(before)), norm(''.join(after))
    if a != b or norm(''.join(before_tbl)) != norm(''.join(after_tbl)):
        n = min(len(a), len(b))
        d = next((i for i in range(n) if a[i] != b[i]), n)
        print('!! ข้อความเปลี่ยนไป  ไม่บันทึก')
        print('   ต่างที่ตัวอักษร %d  เดิม: …%s…  ใหม่: …%s…' % (d, a[max(0, d - 30):d + 30], b[max(0, d - 30):d + 30]))
        raise SystemExit(2)
    log.append('ตัวตรวจ: ข้อความทั้งเล่ม %d อักขระ เหมือนเดิมทุกตัว' % len(b))
    doc.save(dst)
    set_theme_fonts(dst)
    log.append('ฟอนต์ธีมของไฟล์เป็น TH Sarabun New')
    return log


def set_theme_fonts(path):
    import zipfile
    tmp = path + '.theme'
    with zipfile.ZipFile(path) as zin, zipfile.ZipFile(tmp, 'w', zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename.startswith('word/theme/') and item.filename.endswith('.xml'):
                x = data.decode('utf-8')
                x = re.sub(r'(<a:(?:major|minor)Font><a:latin typeface=")[^"]*(")', r'\g<1>' + FONT + r'\g<2>', x)
                x = re.sub(r'(<a:font script="Thai" typeface=")[^"]*(")', r'\g<1>' + FONT + r'\g<2>', x)
                data = x.encode('utf-8')
            if item.filename == 'word/numbering.xml':
                # เลขข้อแบบตัวเลข (decimal) ที่ไม่ระบุฟอนต์ ให้ใช้ TH Sarabun New  ไม่งั้นช่องว่างหลังเลขเป็น Arial
                x = data.decode('utf-8')
                rpr = ('<w:rPr><w:rFonts w:ascii="%s" w:hAnsi="%s" w:eastAsia="%s" w:cs="%s"/></w:rPr>' % ((FONT,) * 4))

                def fix_lvl(m):
                    seg = m.group(0)
                    if '<w:numFmt w:val="decimal"/>' in seg and '<w:rPr>' not in seg:
                        return seg[:-len('</w:lvl>')] + rpr + '</w:lvl>'
                    return seg
                x = re.sub(r'<w:lvl [^>]*>.*?</w:lvl>', fix_lvl, x, flags=re.S)
                data = x.encode('utf-8')
            zout.writestr(item, data)
    os.replace(tmp, path)


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
