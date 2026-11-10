# -*- coding: utf-8 -*-
"""
ปรับไฟล์ Word ของรายงานให้ตรงตาม "คู่มือการเขียนและการพิมพ์" ของสถาบัน
(report/exam_report/รูปแบบรายงานสหกิจศึกษา.doc)  ทำบนไฟล์ Word โดยตรง ไม่แตะถ้อยคำของเนื้อหา
(ยกเว้นที่คู่มือกำหนดชื่อไว้ คือคำว่า "ภาพที่" ชื่อหัวข้อ 3.2.2 / 4.3 / ประวัติผู้จัดทำ และรายการเอกสารอ้างอิง)

ข้อกำหนดที่ทำ (เลขข้อตามคู่มือ)   ฟอนต์ใช้ TH Sarabun New ตามฟอร์มคณะวิศวะ (ผู้เขียนสั่ง 10 พ.ย.)
  2   ชื่อบท 20 / หัวข้อ 18 / หัวข้อย่อย 16 ตัวหนา  เนื้อความและรายละเอียดส่วนต่าง ๆ 16 ปกติ
      ระยะห่างระหว่างบรรทัด Single  (ตารางก็ 16)
  3.2 ย่อหน้าเว้น 0.5 นิ้ว   3.3 เว้นว่างระหว่างบรรทัดเพียง 1 บรรทัด (ชื่อบท หัวข้อใหญ่ หัวข้อย่อย)
  3.4 ไม่ทิ้งบรรทัดเดียวไว้ท้ายหน้าหรือต้นหน้า (widow/orphan)
  4   ข้อความไทยกระจายแบบไทย  ข้อความอังกฤษล้วน Justify
  5   เลขหน้า  ส่วนนำใช้ ก ข ค (นับจากปกใน)  ส่วนเนื้อเรื่อง 1 2 3  หน้าแรกของบทไม่แสดง  เอกสารอ้างอิงและภาคผนวกไม่แสดง
  8   ภาพที่ X.Y  ตัวหนา  เว้น 3 ช่วงตัวอักษร  ชื่อภาพธรรมดา  กึ่งกลางใต้ภาพ  บรรทัดที่ 2 ตรงอักษรแรกของชื่อ
  9   ตารางที่ X.Y  ตัวหนา  ชิดซ้าย  เว้น 3 ช่วง  ตารางที่ข้ามหน้า (ต่อ)  (ทำที่ finish-docx.ps1)
  ใบรองปก  หน้าว่างหน้าสุดและท้ายสุด     เอกสารอ้างอิงแบบระบบหมายเลขตามตัวอย่างในคู่มือ
วิธีใช้   py tools/conform_form.py SRC DST      หรือ   SRC --inplace   (สำรองที่ report/สำรอง/ ก่อนเสมอ)
แล้วตามด้วย tools/finish-docx.ps1
"""
import argparse
import copy
import datetime
import os
import re
import shutil
import sys
import zipfile

sys.stdout.reconfigure(encoding='utf-8')
from docx import Document                       # noqa: E402
from docx.oxml import OxmlElement               # noqa: E402
from docx.oxml.ns import qn                     # noqa: E402
from PIL import ImageFont                       # noqa: E402

FONT = 'TH Sarabun New'
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
REF_FILE = os.path.join(ROOT, 'md', 'final', 'เอกสารอ้างอิง-แบบฟอร์ม.md')
THAI = re.compile('[\u0e00-\u0e7f]')
TEXT_W_PT = (21.0 - 3.81 - 2.54) / 2.54 * 72          # ความกว้างบรรทัด 14.65 ซม.
RELNS = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id'


def W(t):
    return qn('w:' + t)


def sub(parent, tag, **attrs):
    el = parent.find(W(tag))
    if el is None:
        el = OxmlElement('w:' + tag)
        parent.append(el)
    for k, v in attrs.items():
        el.set(W(k), str(v))
    return el


def ptext(p):
    return ''.join(t.text or '' for t in p.iter(W('t')))


def style_of(p):
    pPr = p.find(W('pPr'))
    if pPr is None:
        return None
    s = pPr.find(W('pStyle'))
    return s.get(W('val')) if s is not None else None


# --------------------------------------------------------------------------- ฟอนต์
def fix_fonts(doc):
    parts = [doc.element, doc.styles.element]
    for rel in doc.part.package.iter_parts():
        n = str(rel.partname)
        if '/header' in n or '/footer' in n:
            parts.append(rel._element if hasattr(rel, '_element') else None)
    try:
        parts.append(doc.part.numbering_part.element)
    except Exception:
        pass
    n = 0
    for root in [p for p in parts if p is not None]:
        for rf in root.iter(W('rFonts')):
            for a in list(rf.attrib):
                del rf.attrib[a]
            for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
                rf.set(W(a), FONT)
            n += 1
    # docDefaults / สไตล์ที่ไม่มี rFonts ให้ระบุฟอนต์
    dd = doc.styles.element.find(W('docDefaults'))
    if dd is not None:
        rpr = dd.find(W('rPrDefault')).find(W('rPr'))
        rf = sub(rpr, 'rFonts')
        for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
            rf.set(W(a), FONT)
    return n


def set_theme_fonts(path):
    tmp = path + '.theme'
    with zipfile.ZipFile(path) as zin, zipfile.ZipFile(tmp, 'w', zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename.startswith('word/theme/') and item.filename.endswith('.xml'):
                x = data.decode('utf-8')
                x = re.sub(r'(<a:(?:major|minor)Font><a:latin typeface=")[^"]*(")', r'\g<1>' + FONT + r'\g<2>', x)
                x = re.sub(r'(<a:font script="Thai" typeface=")[^"]*(")', r'\g<1>' + FONT + r'\g<2>', x)
                data = x.encode('utf-8')
            zout.writestr(item, data)
    os.replace(tmp, path)


# --------------------------------------------------------------------------- สไตล์
def spacing(pPr, before_lines=None, after_lines=None):
    sp = sub(pPr, 'spacing')
    sp.set(W('line'), '240')
    sp.set(W('lineRule'), 'auto')
    for side, v in (('before', before_lines), ('after', after_lines)):
        if v is None:
            continue
        sp.set(W(side), str(int(v * 360)))
        sp.set(W(side + 'Lines'), str(int(v * 100)))
    # สคีมาบังคับลำดับ  spacing ต้องมาก่อน ind และ jc
    order = ['pStyle', 'keepNext', 'keepLines', 'pageBreakBefore', 'widowControl', 'numPr', 'pBdr', 'shd', 'tabs',
             'spacing', 'ind', 'contextualSpacing', 'jc', 'outlineLvl', 'rPr', 'sectPr']
    kids = list(pPr)
    kids.sort(key=lambda e: order.index(e.tag.split('}')[1]) if e.tag.split('}')[1] in order else 99)
    for k in kids:
        pPr.remove(k)
    for k in kids:
        pPr.append(k)


def fix_styles(doc):
    def st(name):
        try:
            return doc.styles[name].element
        except KeyError:
            return None

    def pp(el):
        return el.find(W('pPr')) if el.find(W('pPr')) is not None else el.insert(0, OxmlElement('w:pPr')) or el.find(W('pPr'))

    def rp(el):
        r = el.find(W('rPr'))
        if r is None:
            r = OxmlElement('w:rPr')
            el.append(r)
        return r

    def size(el, pt, bold=None):
        r = rp(el)
        for tag in ('sz', 'szCs'):
            sub(r, tag, val=pt * 2)
        if bold is not None:
            for tag in ('b', 'bCs'):
                for e in r.findall(W(tag)):
                    r.remove(e)
                if bold:
                    sub(r, tag)
        # ลำดับใน rPr: rFonts b bCs ... color ... sz szCs
        order = ['rStyle', 'rFonts', 'b', 'bCs', 'i', 'iCs', 'caps', 'color', 'spacing', 'sz', 'szCs', 'u', 'cs']
        kids = list(r)
        kids.sort(key=lambda e: order.index(e.tag.split('}')[1]) if e.tag.split('}')[1] in order else 50)
        for k in kids:
            r.remove(k)
        for k in kids:
            r.append(k)

    n = st('Normal')
    p = pp(n)
    for t in ('ind', 'jc', 'widowControl'):
        for e in p.findall(W(t)):
            p.remove(e)
    sub(p, 'widowControl')
    spacing(p, 0, 0)
    sub(p, 'ind', firstLine=720)               # 0.5 นิ้ว
    sub(p, 'jc', val='thaiDistribute')
    spacing(p)
    size(n, 16, bold=False)

    for name, pt, before, after, jc, ind in (
            ('Heading 1', 20, 0, 1, 'center', 0), ('Heading 2', 18, 1, 0, 'left', 0), ('Heading 3', 16, 1, 0, 'left', None)):
        e = st(name)
        if e is None:
            continue
        p = pp(e)
        spacing(p, before, after)
        sub(p, 'jc', val=jc)
        if ind is not None:
            sub(p, 'ind', left=0, firstLine=0)
        spacing(p)
        size(e, pt, bold=True)
    e = st('Heading 3')
    if e is not None:
        sub(pp(e), 'ind', left=720, firstLine=0)       # หัวข้อย่อยเว้นเข้ามาเล็กน้อย
    for name, jc, b, a in (('CaptionFigure', 'center', 1, 1), ('CaptionTable', 'left', 1, 1)):
        e = st(name)
        if e is None:
            continue
        p = pp(e)
        spacing(p, b, a)
        sub(p, 'ind', left=0, firstLine=0)
        sub(p, 'jc', val=jc)
        spacing(p)
        size(e, 16, bold=False)
    e = st('CaptionTable')
    if e is not None and pp(e).find(W('keepNext')) is None:
        k = OxmlElement('w:keepNext')
        pp(e).insert(1 if pp(e).find(W('pStyle')) is not None else 0, k)


# --------------------------------------------------------------------------- ย่อหน้า
def fix_paragraphs(doc):
    body = doc.element.body
    in_tbl = set()
    for t in body.iter(W('tbl')):
        for p in t.iter(W('p')):
            in_tbl.add(id(p))
    changed = 0
    for p in body.iter(W('p')):
        if id(p) in in_tbl:
            continue
        pPr = p.find(W('pPr'))
        if pPr is None:
            continue
        sname = style_of(p)
        ind = pPr.find(W('ind'))
        if ind is not None:
            if ind.get(W('firstLine')) in ('850', '851', '852'):
                ind.set(W('firstLine'), '720')
                changed += 1
            if ind.get(W('left')) in ('850', '851', '852') and ind.get(W('hanging')) is None:
                ind.set(W('left'), '720')
        jc = pPr.find(W('jc'))
        text = ptext(p)
        if jc is not None and jc.get(W('val')) in ('both', 'distribute'):
            jc.set(W('val'), 'thaiDistribute' if THAI.search(text) else 'both')
        elif jc is None and text.strip() and not THAI.search(text) and len(text) > 60 and '\t' not in text:
            pPr.append(OxmlElement('w:jc'))
            pPr.find(W('jc')).set(W('val'), 'both')
        for tag in ('widowControl',):
            for e in pPr.findall(W(tag)):
                pPr.remove(e)
    return changed


def fix_tables(doc):
    n = 0
    for t in doc.element.body.iter(W('tbl')):
        text = ''.join(x.text or '' for x in t.iter(W('t')))
        if 'วางรูปที่' in text or 'วางภาพที่' in text:
            continue
        for r in t.iter(W('r')):
            rPr = r.find(W('rPr'))
            if rPr is None:
                rPr = OxmlElement('w:rPr')
                r.insert(0, rPr)
            sub(rPr, 'sz', val=32)
            sub(rPr, 'szCs', val=32)
            order = ['rStyle', 'rFonts', 'b', 'bCs', 'i', 'iCs', 'color', 'sz', 'szCs', 'shd', 'cs']
            kids = list(rPr)
            kids.sort(key=lambda e: order.index(e.tag.split('}')[1]) if e.tag.split('}')[1] in order else 50)
            for k in kids:
                rPr.remove(k)
            for k in kids:
                rPr.append(k)
            n += 1
        for tr in t.findall(W('tr')):
            trPr = tr.find(W('trPr'))
            if trPr is None:
                trPr = OxmlElement('w:trPr')
                tr.insert(0, trPr)
            if trPr.find(W('cantSplit')) is None:
                trPr.insert(0, OxmlElement('w:cantSplit'))
    # โค้ดบล็อก (ย่อหน้ามีพื้นสีเทาและฟอนต์เดียวกับเนื้อ) ปรับเป็น 16 ด้วย
    for p in doc.element.body.iter(W('p')):
        pPr = p.find(W('pPr'))
        if pPr is not None and pPr.find(W('shd')) is not None and not THAI.search(ptext(p)[:3] or 'a'):
            for r in p.iter(W('r')):
                rPr = r.find(W('rPr'))
                if rPr is not None and rPr.find(W('sz')) is not None:
                    rPr.find(W('sz')).set(W('val'), '32')
                    if rPr.find(W('szCs')) is not None:
                        rPr.find(W('szCs')).set(W('val'), '32')
    return n


# --------------------------------------------------------------------------- ข้อความ
def replace_in_w_t(doc, old, new):
    n = 0
    for t in doc.element.body.iter(W('t')):
        if t.text and old in t.text:
            t.text = t.text.replace(old, new)
            n += t.text.count(new)
    return n


def merge_citations(doc):
    pat = re.compile(r'\[(\d+)\],\s*\[(\d+)\]')
    n = 0
    for t in doc.element.body.iter(W('t')):
        if t.text and '], [' in t.text:
            old = t.text
            while True:
                new = pat.sub(r'[\1, \2]', old)
                new = re.sub(r'\[(\d+(?:, \d+)*)\],\s*\[(\d+)\]', r'[\1, \2]', new)
                if new == old:
                    break
                old = new
            if old != t.text:
                t.text = old
                n += 1
    return n


def set_text(p, text):
    runs = p.findall(W('r'))
    keep = None
    for r in runs:
        if r.find(W('t')) is not None and keep is None:
            keep = r
    if keep is None:
        return False
    for r in runs:
        if r is not keep:
            p.remove(r)
    for t in keep.findall(W('t')):
        keep.remove(t)
    t = OxmlElement('w:t')
    t.text = text
    t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    keep.append(t)
    return True


def retitle_headings(doc):
    log = []
    for p in doc.element.body.iter(W('p')):
        s = ptext(p)
        if '\t' in s:
            continue
        if re.match(r'^3\.2\.2\s', s) and style_of(p) not in (None,):
            if set_text(p, '3.2.2 เครื่องมือและอุปกรณ์ที่ใช้ในการทำโครงงาน'):
                log.append('หัวข้อ 3.2.2')
        elif re.match(r'^4\.3\s', s) and len(s) < 120:
            if set_text(p, '4.3 วิจารณ์ข้อมูลโดยเปรียบเทียบผลที่ได้รับกับวัตถุประสงค์และจุดมุ่งหมายของการปฏิบัติงานหรือการจัดทำโครงงาน'):
                log.append('หัวข้อ 4.3')
        elif s.strip() == 'ประวัติผู้จัดทำโครงงาน':
            if set_text(p, 'ประวัติผู้จัดทำโครงงานสหกิจศึกษา'):
                log.append('หัวข้อประวัติผู้จัดทำ')
    return log


def add_bio_row(doc):
    ps = list(doc.element.body.iter(W('p')))
    if any('ผลงานที่ได้รับการตีพิมพ์' in ptext(p) for p in ps):
        return False
    for p in ps:
        if ptext(p).startswith('ประวัติการฝึกอบรม'):
            new = copy.deepcopy(p)
            for t in new.iter(W('t')):
                if 'ประวัติการฝึกอบรม' in (t.text or ''):
                    t.text = t.text.replace('ประวัติการฝึกอบรม', 'ผลงานที่ได้รับการตีพิมพ์')
            p.addnext(new)
            return True
    return False


def add_degree(doc):
    # คู่มือ "ข้อผิดพลาดที่พบบ่อย" ข้อ 3  หน้าปกใน: ต่อท้ายชื่อด้วยชื่อย่อวุฒิและสาขาวิชาในวงเล็บ
    for p in doc.element.body.iter(W('p')):
        if ptext(p).strip() == 'นายอวกาศ ดีคำ':
            ts = [t for t in p.iter(W('t'))]
            ts[-1].text = ts[-1].text + ' (วศ.บ. วิศวกรรมคอมพิวเตอร์และปัญญาประดิษฐ์)'
            ts[-1].set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
            return True
    return False


# --------------------------------------------------------------------------- คำบรรยายภาพ/ตาราง
def measure(text, bold=False):
    f = ImageFont.truetype('C:/Windows/Fonts/THSarabunNew Bold.ttf' if bold else 'C:/Windows/Fonts/THSarabunNew.ttf', 160)
    return f.getlength(text) / 10.0                 # จุด ที่ขนาด 16


def segments(p):
    out = []
    for r in p.findall(W('r')):
        t = ''.join(x.text or '' for x in r.findall(W('t')))
        if t:
            out.append((t, r.find(W('rPr'))))
    return out


def make_run(text, rPr=None, bold=False):
    r = OxmlElement('w:r')
    rp = copy.deepcopy(rPr) if rPr is not None else OxmlElement('w:rPr')
    for tag in ('b', 'bCs'):
        for e in rp.findall(W(tag)):
            rp.remove(e)
    if bold:
        rp.insert(0, OxmlElement('w:bCs'))
        rp.insert(0, OxmlElement('w:b'))
        rf = rp.find(W('rFonts'))
        if rf is not None:
            rp.remove(rf)
            rp.insert(0, rf)
    if rp.find(W('cs')) is None:
        rp.append(OxmlElement('w:cs'))
    r.append(rp)
    t = OxmlElement('w:t')
    t.text = text
    t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    r.append(t)
    return r


def fix_captions(doc):
    stats = {'fig': 0, 'tbl': 0, 'hang': 0}
    for p in doc.element.body.iter(W('p')):
        s = style_of(p)
        if s is None:
            continue
        name = None
        for sty in doc.styles:
            if sty.style_id == s:
                name = sty.name
                break
        if name not in ('CaptionFigure', 'CaptionTable'):
            continue
        fig = name == 'CaptionFigure'
        segs = segments(p)
        full = ''.join(t for t, _ in segs)
        m = re.match(r'^(รูปที่|ภาพที่|ตารางที่)\s*(\d+\.\d+)\s*', full)
        if not m:
            continue
        label = ('ภาพที่ ' if fig else 'ตารางที่ ') + m.group(2)
        rest_from = m.end()
        # ส่วนที่เหลือ  ใช้ run เดิมเพื่อรักษาตัวเอียง/โค้ด
        new_runs = [make_run(label, segs[0][1], bold=True), make_run('   ', segs[0][1])]
        pos = 0
        for t, rPr in segs:
            a, b = pos, pos + len(t)
            pos = b
            if b <= rest_from:
                continue
            piece = t[max(0, rest_from - a):]
            if piece:
                new_runs.append(make_run(piece, rPr))
        for r in p.findall(W('r')):
            p.remove(r)
        for r in new_runs:
            p.append(r)
        rest = full[rest_from:]
        label_w = measure(label + '   ', bold=True)
        total = label_w + measure(rest)
        pPr = p.find(W('pPr'))
        for e in pPr.findall(W('ind')):
            pPr.remove(e)
        ind = OxmlElement('w:ind')
        if total > TEXT_W_PT - 4:                      # ยาวเกิน 1 บรรทัด  บรรทัดต่อไปตรงอักษรแรกของชื่อ
            hang = int(label_w * 20)
            if fig:
                ind.set(W('left'), str(hang + 567))
                ind.set(W('right'), '567')
                ind.set(W('hanging'), str(hang))
                jc = 'left'
            else:
                ind.set(W('left'), str(hang))
                ind.set(W('hanging'), str(hang))
                jc = 'left'
            stats['hang'] += 1
        else:
            ind.set(W('left'), '0')
            ind.set(W('firstLine'), '0')
            jc = 'center' if fig else 'left'
        pPr.append(ind)
        for e in pPr.findall(W('jc')):
            pPr.remove(e)
        j = OxmlElement('w:jc')
        j.set(W('val'), jc)
        pPr.append(j)
        spacing(pPr)
        stats['fig' if fig else 'tbl'] += 1
    return stats


# --------------------------------------------------------------------------- ส่วน/เลขหน้า
def sect_of(el):
    if el.tag == W('sectPr'):
        return el
    pPr = el.find(W('pPr')) if el.tag == W('p') else None
    return pPr.find(W('sectPr')) if pPr is not None else None


def fix_sections(doc):
    body = doc.element.body
    secs = [(e, sect_of(e)) for e in body if sect_of(e) is not None]
    if len(secs) < 10:
        raise SystemExit('โครงส่วนไม่ตรงที่คาด (พบ %d ส่วน)' % len(secs))
    sp = [s for _, s in secs]

    def pg(s, fmt=None, start=None, remove=False):
        e = s.find(W('pgNumType'))
        if remove:
            if e is not None:
                s.remove(e)
            return
        if e is None:
            e = OxmlElement('w:pgNumType')
            ref = s.find(W('cols'))
            if ref is not None:
                ref.addprevious(e)
            else:
                s.append(e)
        for a in list(e.attrib):
            del e.attrib[a]
        if fmt:
            e.set(W('fmt'), fmt)
        if start:
            e.set(W('start'), str(start))

    def default_header(s):
        for h in s.findall(W('headerReference')):
            if h.get(W('type')) == 'default':
                return h.get(RELNS)

    empty_rid = default_header(sp[-2])           # ภาคผนวก: ไม่มีเลขหน้า
    # 1 ปกใน + หน้าอนุมัติ  นับเป็น ก ข แต่ไม่แสดงเลข
    pg(sp[0], 'thaiLetters', 1)
    # 2 บทสรุป..สารบัญภาพ  ต่อจากเดิม ไม่เริ่มใหม่
    pg(sp[1], 'thaiLetters')
    # 3 บทที่ 1 เริ่ม 1  4-7 ต่อเนื่อง (มีอยู่แล้ว)
    # 8 เอกสารอ้างอิง ไม่แสดงเลขหน้า
    for h in sp[7].findall(W('headerReference')):
        if h.get(W('type')) == 'default':
            h.set(RELNS, empty_rid)
    for e in sp[7].findall(W('titlePg')):
        sp[7].remove(e)
    pg(sp[7], remove=True)
    # 9 ภาคผนวก ไม่แสดง   10 ประวัติ นับต่อเนื่องและแสดงเลข
    pg(sp[9], remove=True)

    # ใบรองปก  หน้าว่างต้นเล่ม
    first = body[0]
    fly = OxmlElement('w:p')
    pPr = OxmlElement('w:pPr')
    new_s = copy.deepcopy(sp[0])
    e = new_s.find(W('pgNumType'))
    if e is not None:
        new_s.remove(e)
    pPr.append(new_s)
    fly.append(pPr)
    first.addprevious(fly)
    # หน้าว่างท้ายเล่ม  ส่วนสุดท้ายเดิม (ประวัติ) ย้ายไปไว้ที่ย่อหน้าปิดส่วน แล้วให้ส่วนสุดท้ายใหม่ว่างเปล่า
    final = body.find(W('sectPr'))
    if final is sp[-1]:
        closer = OxmlElement('w:p')
        cp = OxmlElement('w:pPr')
        cp.append(copy.deepcopy(final))
        closer.append(cp)
        final.addprevious(closer)
        blank = OxmlElement('w:p')
        final.addprevious(blank)
        for h in final.findall(W('headerReference')):
            if h.get(W('type')) == 'default':
                h.set(RELNS, empty_rid)
        for e in final.findall(W('pgNumType')):
            final.remove(e)
    return len(sp)


# --------------------------------------------------------------------------- ลำดับการเว้นบรรทัดของหัวข้อ
def fix_heading_gaps(doc):
    n = 0
    prev = None
    names = {s.style_id: s.name for s in doc.styles}
    for p in doc.element.body.iter(W('p')):
        sn = names.get(style_of(p))
        if sn == 'Heading 2' and prev == 'Heading 1':
            pPr = p.find(W('pPr'))
            spacing(pPr, 0, None)
            n += 1
        prev = sn if sn in ('Heading 1', 'Heading 2', 'Heading 3') else (None if ptext(p).strip() else prev)
    return n


# --------------------------------------------------------------------------- เอกสารอ้างอิง
def parse_bold(text):
    parts = re.split(r'(\*\*[^*]+\*\*)', text)
    return [(q[2:-2], True) if q.startswith('**') else (q, False) for q in parts if q]


def rebuild_references(doc):
    body = doc.element.body
    kids = list(body)
    h = next((e for e in kids if e.tag == W('p') and ptext(e).strip() == 'เอกสารอ้างอิง'), None)
    if h is None or not os.path.exists(REF_FILE):
        return 0
    i = kids.index(h)
    j = i + 1
    while j < len(kids) and sect_of(kids[j]) is None:
        j += 1
    for e in kids[i + 1:j]:
        body.remove(e)
    entries = [ln.strip() for ln in open(REF_FILE, encoding='utf-8') if re.match(r'^\d+\.\s', ln.strip())]
    anchor = h
    for ent in entries:
        p = OxmlElement('w:p')
        pPr = OxmlElement('w:pPr')
        pPr.append(OxmlElement('w:keepLines'))
        ww = OxmlElement('w:wordWrap')          # ให้ลิงก์ยาวตัดบรรทัดกลางคำได้ ไม่ล้นขอบขวา
        ww.set(W('val'), '0')
        pPr.append(ww)
        ind = OxmlElement('w:ind')
        ind.set(W('left'), '0')
        ind.set(W('firstLine'), '0')
        pPr.append(ind)
        jc = OxmlElement('w:jc')
        jc.set(W('val'), 'left' if 'http' in ent else ('thaiDistribute' if THAI.search(ent) else 'both'))
        pPr.append(jc)
        p.append(pPr)
        spacing(pPr, 0, 1)
        for text, bold in parse_bold(ent):
            p.append(make_run(text, None, bold=bold))
        anchor.addnext(p)
        anchor = p
    return len(entries)


# --------------------------------------------------------------------------- หลัก
def run(src, dst, refresh=False):
    """refresh=True  ทำเฉพาะขั้นที่ทำซ้ำได้ (ฟอนต์ สไตล์ ย่อหน้า ตาราง คำบรรยาย เอกสารอ้างอิง)
    ข้ามขั้นที่เพิ่มของใหม่ (ใบรองปก ปรับส่วน ต่อท้ายชื่อบนปก แถวประวัติ ชื่อหัวข้อ)"""
    doc = Document(src)
    log = []
    log.append('ฟอนต์ %s: %d จุด' % (FONT, fix_fonts(doc)))
    fix_styles(doc)
    log.append('สไตล์: Normal 16 ย่อหน้า 0.5 นิ้ว กระจายแบบไทย · หัวข้อ 20/18/16 · เว้น 1 บรรทัด')
    log.append('ย่อหน้าที่ปรับเยื้อง 1.5 ซม. → 0.5 นิ้ว: %d' % fix_paragraphs(doc))
    log.append('ตาราง 16 pt: %d run' % fix_tables(doc))
    if not refresh:
        n1 = replace_in_w_t(doc, 'สารบัญรูป', 'สารบัญภาพ')
        n2 = replace_in_w_t(doc, 'รูปที่', 'ภาพที่')
        log.append('"รูปที่" → "ภาพที่": %d แห่ง  (สารบัญภาพ %d)' % (n2, n1))
        log.append('รวมวงเล็บอ้างอิง [a], [b] → [a, b]: %d แห่ง' % merge_citations(doc))
        log.extend(retitle_headings(doc))
        log.append('แถวประวัติ "ผลงานที่ได้รับการตีพิมพ์": %s' % add_bio_row(doc))
        log.append('ต่อท้ายชื่อบนปกใน (วศ.บ. + สาขา): %s' % add_degree(doc))
    log.append('คำบรรยาย: %s' % fix_captions(doc))
    log.append('เว้นหัวข้อบทต่อหัวข้อ x.1 ปรับ: %d' % fix_heading_gaps(doc))
    log.append('เอกสารอ้างอิงตามแบบคู่มือ: %d รายการ' % rebuild_references(doc))
    if not refresh:
        log.append('ส่วนของเอกสาร %d ส่วน · ใบรองปกหน้า-หลังเพิ่มแล้ว' % fix_sections(doc))
    doc.save(dst)
    set_theme_fonts(dst)
    return log


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('src')
    ap.add_argument('dst', nargs='?')
    ap.add_argument('--inplace', action='store_true')
    ap.add_argument('--refresh', action='store_true', help='ทำเฉพาะขั้นที่ทำซ้ำได้ (ใช้ได้กับไฟล์ที่ผ่านการจัดแล้ว)')
    a = ap.parse_args()
    if a.inplace:
        bdir = os.path.join(os.path.dirname(os.path.abspath(a.src)), 'สำรอง')
        os.makedirs(bdir, exist_ok=True)
        bak = os.path.join(bdir, os.path.splitext(os.path.basename(a.src))[0] + '-ก่อนจัดตามคู่มือ-' +
                           datetime.datetime.now().strftime('%Y%m%d-%H%M%S') + '.docx')
        shutil.copy2(a.src, bak)
        print('สำรองไว้ที่:', bak)
        tmp = a.src + '.tmp'
        for l in run(a.src, tmp, a.refresh):
            print(' ', l)
        os.replace(tmp, a.src)
    else:
        if not a.dst:
            ap.error('ต้องระบุ DST หรือใช้ --inplace')
        for l in run(a.src, a.dst, a.refresh):
            print(' ', l)
    print('เสร็จ')
