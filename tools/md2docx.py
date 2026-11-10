# -*- coding: utf-8 -*-
"""
สร้างรูปเล่มรายงานสหกิจศึกษา (.docx) จากไฟล์ร่าง markdown ในโฟลเดอร์ md/

รูปแบบยึดตาม  report/exam_report/รูปเล่มสหกิจสำหรับคณะวิศวะ.pdf
  ขอบกระดาษ   บน 1.5" ซ้าย 1.5" ล่าง 1" ขวา 1"
  ฟอนต์        TH Sarabun New ทั้งเล่ม รวมเลขหน้า
  ขนาด         ชื่อบทหนา 20 · หัวข้อ x.x หนา 18 · x.x.x หนา 16 · เนื้อความบาง 16
  เลขหน้า      มุมขวาบน · ส่วนหน้า ก ข ค · ตั้งแต่บทที่ 1 เป็น 1 2 3
               หน้าแรกของแต่ละบทไม่แสดงเลขหน้า
  รูป          คำบรรยายใต้รูป · ตาราง คำบรรยายบนตาราง ชิดซ้าย

วิธีใช้   py tools/md2docx.py
ผลลัพธ์   รายงานสหกิจศึกษา-EscapeRoom-ร่าง.docx
"""

import os
import re
import sys

try:                       # คอนโซลไทยของ Windows เป็น cp874 พิมพ์ · ไม่ได้
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

from docx import Document
from docx.enum.table import WD_ROW_HEIGHT_RULE, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.section import WD_SECTION
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

# ----------------------------------------------------------------- ค่าคงที่รูปเล่ม

FONT = 'TH Sarabun New'
MONO = 'TH Sarabun New'

SZ_BODY = 16
SZ_CHAPTER = 20
SZ_H2 = 18
SZ_H3 = 16
SZ_TABLE = 14          # ตารางใช้ 14 เพื่อให้คอลัมน์เยอะ ๆ พอกับหน้ากระดาษ
SZ_CODE = 13

MARGIN_TOP = Cm(3.81)
MARGIN_LEFT = Cm(3.81)
MARGIN_BOTTOM = Cm(2.54)
MARGIN_RIGHT = Cm(2.54)
TEXT_WIDTH = Cm(21.0) - MARGIN_LEFT - MARGIN_RIGHT

INDENT_FIRST = Cm(1.5)

SHADE_DRAFT = 'FFF4D6'     # กล่องหมายเหตุของร่าง — ลบก่อนส่ง
SHADE_NOTE = 'F0F0F0'      # กล่องหมายเหตุในเนื้อหา
SHADE_CODE = 'F4F4F4'
SHADE_HEAD = 'E8E8E8'      # แถวหัวตาราง

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MD = os.path.join(ROOT, 'md')

# โหมดร่าง  อ่านบทจาก md/ ซึ่งมีกล่องหมายเหตุและหลักฐานกำกับไว้ทุกจุด
# โหมดจริง  อ่านบทจาก md/final/ ซึ่งเป็นเนื้อความสำหรับเข้าเล่ม ไม่มีกล่องหมายเหตุ
#           และสร้างเฉพาะบทที่มีไฟล์อยู่ใน md/final/ แล้ว
FINAL = '--final' in sys.argv
MD_CH = os.path.join(MD, 'final') if FINAL else MD
# ฉบับจริงเขียนลงโฟลเดอร์แยก  ห้ามเขียนทับไฟล์ใน report/ เพราะผู้เขียนแก้ใน Word
# (ไฟล์หลักคือ report/รายงานสหกิจศึกษา-EscapeRoom-v2.docx)  ผลจากสคริปต์ใช้เป็นแค่ต้นแบบ
OUT = (os.path.join(ROOT, 'report', '_สร้างจากสคริปต์', 'รายงานสหกิจศึกษา-EscapeRoom-จากสคริปต์.docx')
       if FINAL
       else os.path.join(ROOT, 'รายงานสหกิจศึกษา-EscapeRoom-ร่าง.docx'))

CHAPTERS = [
    ('บทที่ 1', 'บทนำ', 'บทที่-1-บทนำ.md'),
    ('บทที่ 2', 'ทฤษฎีหรือเทคโนโลยีที่ใช้ในการปฏิบัติงาน', 'บทที่-2-ทฤษฎีที่เกี่ยวข้อง.md'),
    ('บทที่ 3', 'แผนงานการปฏิบัติงานและขั้นตอนการดำเนินงาน', 'บทที่-3-ขั้นตอนการดำเนินงาน.md'),
    ('บทที่ 4', 'ผลการดำเนินงาน', 'บทที่-4-ผลการดำเนินงาน.md'),
    ('บทที่ 5', 'บทสรุปและข้อเสนอแนะ', 'บทที่-5-บทสรุปและข้อเสนอแนะ.md'),
]

# ชื่อโครงงานจริงที่ผู้เขียนกำหนด 9 พฤศจิกายน 2569 — ต้องตรงกันทุกที่ในเล่ม
#
# ZWSP คั่นขอบคำไว้ตั้งแต่ตอนเข้าใจว่า Word ไม่ตัดคำไทย  ที่จริงต้นเหตุคือ run ไม่มีแฟล็ก w:cs
# (แก้ใน style_run แล้ว) ZWSP จึงไม่จำเป็นอีก แต่ไม่เสียหาย  ถ้าจะเอาออกให้ต่อสตริงตรง ๆ
ZWSP = '​'
TITLE_TH = ZWSP.join(('การพัฒนา', 'เกม', 'ห้องหลบหนี', 'ด้วย', 'การออกแบบ',
                      'ซอฟต์แวร์', 'แบบ', 'มอดูลาร์'))
TITLE_EN = 'Development of an Escape Room Game Using Modular Software Design'

stats = {'tables': 0, 'figures': 0, 'notes_draft': 0, 'notes_body': 0,
         'code': 0, 'images': 0}


# ------------------------------------------------------------- เครื่องมือ XML ย่อย
#
# OOXML บังคับ "ลำดับ" ของ element ลูกไว้ตามสกีมา ไม่ใช่แค่ชื่อ
# ถ้าแทรกผิดลำดับ Word จะฟ้องว่าไฟล์เสียหายแล้วไม่เปิดให้
# ตารางด้านล่างคือลำดับตามมาตรฐาน ECMA-376 ของ element ที่สคริปต์นี้แตะ

SEQ = {
    'w:pPr': (
        'w:pStyle', 'w:keepNext', 'w:keepLines', 'w:pageBreakBefore',
        'w:framePr', 'w:widowControl', 'w:numPr', 'w:suppressLineNumbers',
        'w:pBdr', 'w:shd', 'w:tabs', 'w:suppressAutoHyphens', 'w:kinsoku',
        'w:wordWrap', 'w:overflowPunct', 'w:topLinePunct', 'w:autoSpaceDE',
        'w:autoSpaceDN', 'w:bidi', 'w:adjustRightInd', 'w:snapToGrid',
        'w:spacing', 'w:ind', 'w:contextualSpacing', 'w:mirrorIndents',
        'w:suppressOverlap', 'w:jc', 'w:textDirection', 'w:textAlignment',
        'w:textboxTightWrap', 'w:outlineLvl', 'w:divId', 'w:cnfStyle',
        'w:rPr', 'w:sectPr', 'w:pPrChange'),
    'w:rPr': (
        'w:rStyle', 'w:rFonts', 'w:b', 'w:bCs', 'w:i', 'w:iCs', 'w:caps',
        'w:smallCaps', 'w:strike', 'w:dstrike', 'w:outline', 'w:shadow',
        'w:emboss', 'w:imprint', 'w:noProof', 'w:snapToGrid', 'w:vanish',
        'w:webHidden', 'w:color', 'w:spacing', 'w:w', 'w:kern', 'w:position',
        'w:sz', 'w:szCs', 'w:highlight', 'w:u', 'w:effect', 'w:bdr', 'w:shd',
        'w:fitText', 'w:vertAlign', 'w:rtl', 'w:cs', 'w:em', 'w:lang',
        'w:eastAsianLayout', 'w:specVanish', 'w:oMath'),
    'w:tblPr': (
        'w:tblStyle', 'w:tblpPr', 'w:tblOverlap', 'w:bidiVisual',
        'w:tblStyleRowBandSize', 'w:tblStyleColBandSize', 'w:tblW', 'w:jc',
        'w:tblCellSpacing', 'w:tblInd', 'w:tblBorders', 'w:shd',
        'w:tblLayout', 'w:tblCellMar', 'w:tblLook', 'w:tblCaption',
        'w:tblDescription', 'w:tblPrChange'),
    'w:tcPr': (
        'w:cnfStyle', 'w:tcW', 'w:gridSpan', 'w:hMerge', 'w:vMerge',
        'w:tcBorders', 'w:shd', 'w:noWrap', 'w:tcMar', 'w:textDirection',
        'w:tcFitText', 'w:vAlign', 'w:hideMark', 'w:headers', 'w:tcPrChange'),
    'w:trPr': (
        'w:cnfStyle', 'w:divId', 'w:gridBefore', 'w:gridAfter', 'w:wBefore',
        'w:wAfter', 'w:cantSplit', 'w:trHeight', 'w:tblHeader',
        'w:tblCellSpacing', 'w:jc', 'w:hidden', 'w:ins', 'w:del',
        'w:trPrChange'),
    'w:sectPr': (
        'w:headerReference', 'w:footerReference',
        'w:footnotePr', 'w:endnotePr', 'w:type', 'w:pgSz', 'w:pgMar',
        'w:paperSrc', 'w:pgBorders', 'w:lnNumType', 'w:pgNumType', 'w:cols',
        'w:formProt', 'w:vAlign', 'w:noEndnote', 'w:titlePg',
        'w:textDirection', 'w:bidi', 'w:rtlGutter', 'w:docGrid',
        'w:printerSettings', 'w:sectPrChange'),
}


def _set(el, tag, **attrs):
    """หา child ชื่อ tag ถ้าไม่มีให้สร้างและแทรก *ตามลำดับที่สกีมากำหนด*
    แล้วตั้ง attribute"""
    child = el.find(qn(tag))
    if child is None:
        child = OxmlElement(tag)
        seq = SEQ.get(el.tag.split('}')[-1] and 'w:' + el.tag.split('}')[-1])
        placed = False
        if seq and tag in seq:
            after = seq[seq.index(tag) + 1:]
            for sib in el:
                name = 'w:' + sib.tag.split('}')[-1]
                if name in after:
                    sib.addprevious(child)
                    placed = True
                    break
        if not placed:
            el.append(child)
    for k, v in attrs.items():
        child.set(qn('w:' + k), v)
    return child


def style_run(run, size=SZ_BODY, bold=False, italic=False, font=FONT,
              shade=None, color=None):
    """ตั้งฟอนต์ให้ครบทั้ง ascii / hAnsi / cs — ภาษาไทยเป็น complex script
    ถ้าไม่ตั้ง w:cs, w:szCs, w:bCs ไว้ Word จะไม่ใช้ค่าที่ตั้งกับตัวอักษรไทย"""
    rPr = run._element.get_or_add_rPr()

    rf = rPr.find(qn('w:rFonts'))
    if rf is None:
        rf = OxmlElement('w:rFonts')
        rPr.insert(0, rf)
    for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
        rf.set(qn('w:' + a), font)

    half = str(int(round(size * 2)))
    _set(rPr, 'w:sz', val=half)
    _set(rPr, 'w:szCs', val=half)

    if bold:
        _set(rPr, 'w:b', val='1')
        _set(rPr, 'w:bCs', val='1')
    if italic:
        _set(rPr, 'w:i', val='1')
        _set(rPr, 'w:iCs', val='1')
    if shade:
        _set(rPr, 'w:shd', val='clear', color='auto', fill=shade)
    _set(rPr, 'w:lang', val='en-US', bidi='th-TH')
    # run ที่มีอักษรไทยต้องติด <w:cs/> ไม่งั้น Word ไม่ใช้พจนานุกรมตัดคำไทย (บรรทัดสั้นกุด ตัดกลางคำ)
    if any('฀' <= ch <= '๿' for ch in run.text):
        _set(rPr, 'w:cs')
    if color:
        run.font.color.rgb = RGBColor.from_string(color)
    return run


def para_shade(p, fill):
    _set(p._p.get_or_add_pPr(), 'w:shd', val='clear', color='auto', fill=fill)


def para_border(p, left=None, box=None, dashed=False):
    pPr = p._p.get_or_add_pPr()
    pbdr = _set(pPr, 'w:pBdr')
    sides = ['left'] if left else []
    if box:
        sides = ['top', 'left', 'bottom', 'right']
    for side in sides:
        e = OxmlElement('w:' + side)
        e.set(qn('w:val'), 'dashed' if dashed else 'single')
        e.set(qn('w:sz'), '24' if (left and not box) else '8')
        e.set(qn('w:space'), '4')
        e.set(qn('w:color'), 'BFBFBF' if box else 'C8A24B')
        pbdr.append(e)


def keep_with_next(p):
    _set(p._p.get_or_add_pPr(), 'w:keepNext', val='1')


BOOKMARK_ID = [1000]


def bookmark(p, name):
    """คร่อมย่อหน้าด้วย bookmark เพื่อให้ field PAGEREF อ้างเลขหน้าของย่อหน้านี้ได้
    ใช้กับหัวข้อในส่วนหน้าเล่ม ซึ่งไม่ใช่ Heading จึงไม่เข้าสารบัญอัตโนมัติ"""
    BOOKMARK_ID[0] += 1
    i = str(BOOKMARK_ID[0])
    s = OxmlElement('w:bookmarkStart')
    s.set(qn('w:id'), i)
    s.set(qn('w:name'), name)
    e = OxmlElement('w:bookmarkEnd')
    e.set(qn('w:id'), i)
    # w:pPr ต้องเป็นลูกตัวแรกของย่อหน้าเสมอ จึงแทรก bookmarkStart ถัดจากนั้น
    pos = 1 if p._p.find(qn('w:pPr')) is not None else 0
    p._p.insert(pos, s)
    p._p.append(e)
    return p


def add_field(p, instr, placeholder='', size=SZ_BODY, bold=False):
    """แทรก field code เช่น PAGE หรือ TOC — Word จะคำนวณค่าให้เมื่อกด F9"""
    r = p.add_run()
    b = OxmlElement('w:fldChar')
    b.set(qn('w:fldCharType'), 'begin')
    b.set(qn('w:dirty'), 'true')
    r._r.append(b)
    style_run(r, size=size, bold=bold)

    r = p.add_run()
    it = OxmlElement('w:instrText')
    it.set(qn('xml:space'), 'preserve')
    it.text = instr
    r._r.append(it)
    style_run(r, size=size, bold=bold)

    r = p.add_run()
    s = OxmlElement('w:fldChar')
    s.set(qn('w:fldCharType'), 'separate')
    r._r.append(s)
    style_run(r, size=size, bold=bold)

    if placeholder:
        style_run(p.add_run(placeholder), size=size, bold=bold)

    r = p.add_run()
    e = OxmlElement('w:fldChar')
    e.set(qn('w:fldCharType'), 'end')
    r._r.append(e)
    style_run(r, size=size, bold=bold)


# ------------------------------------------------------------------- สไตล์เอกสาร

def strip_theme_fonts(styles_el):
    """แม่แบบของ python-docx ใส่ asciiTheme/hAnsiTheme/cstheme ไว้ในฟอนต์ของสไตล์
    และ Word ให้ attribute ธีมชนะ ascii/hAnsi/cs เสมอ  ข้อความที่ไม่ได้ตั้งฟอนต์เอง
    (เช่นที่ผู้เขียนพิมพ์เพิ่มใน Word) จึงตกไปเป็น Calibri ไม่ใช่ TH Sarabun New
    ล้างออกทั้งไฟล์สไตล์ให้ค่า ascii/hAnsi/cs ทำงานจริง"""
    for rf in styles_el.iter(qn('w:rFonts')):
        for a in ('asciiTheme', 'hAnsiTheme', 'eastAsiaTheme', 'cstheme'):
            if rf.get(qn('w:' + a)) is not None:
                del rf.attrib[qn('w:' + a)]


def build_styles(doc):
    # ค่าตั้งต้นของเอกสารทั้งเล่ม
    styles_el = doc.styles.element
    dflt = styles_el.find(qn('w:docDefaults'))
    rPrD = dflt.find(qn('w:rPrDefault')).find(qn('w:rPr'))
    rf = _set(rPrD, 'w:rFonts')
    for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
        rf.set(qn('w:' + a), FONT)
    strip_theme_fonts(styles_el)
    _set(rPrD, 'w:sz', val=str(SZ_BODY * 2))
    _set(rPrD, 'w:szCs', val=str(SZ_BODY * 2))
    _set(rPrD, 'w:lang', val='en-US', bidi='th-TH')

    def shape(name, size, bold, align, space_before=0, space_after=0,
              indent_first=None, indent_left=None, keep=False):
        st = doc.styles[name]
        st.font.name = FONT
        st.font.size = Pt(size)
        st.font.bold = bold
        rPr = st.element.get_or_add_rPr()
        rf = _set(rPr, 'w:rFonts')
        for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
            rf.set(qn('w:' + a), FONT)
        _set(rPr, 'w:sz', val=str(size * 2))
        _set(rPr, 'w:szCs', val=str(size * 2))
        if bold:
            _set(rPr, 'w:b', val='1')
            _set(rPr, 'w:bCs', val='1')
        else:
            _set(rPr, 'w:b', val='0')
            _set(rPr, 'w:bCs', val='0')
        pf = st.paragraph_format
        pf.alignment = align
        pf.space_before = Pt(space_before)
        pf.space_after = Pt(space_after)
        pf.line_spacing = 1.0
        if indent_first is not None:
            pf.first_line_indent = indent_first
        if indent_left is not None:
            pf.left_indent = indent_left
        pf.keep_with_next = keep
        return st

    shape('Normal', SZ_BODY, False, WD_ALIGN_PARAGRAPH.JUSTIFY,
          indent_first=INDENT_FIRST)
    shape('Heading 1', SZ_CHAPTER, True, WD_ALIGN_PARAGRAPH.CENTER,
          space_after=SZ_BODY * 1.33, indent_first=Cm(0), keep=True)
    shape('Heading 2', SZ_H2, True, WD_ALIGN_PARAGRAPH.LEFT,
          space_before=SZ_BODY * 1.33, indent_first=Cm(0), keep=True)
    # หัวข้อทศนิยม 2 ตำแหน่งเยื้องเข้า 1.27 ซม. ตามที่วัดได้จากรูปเล่มจริง
    # (หัวข้อ x.x อยู่ที่ x=108 pt · x.x.x อยู่ที่ x=144 pt · ต่างกัน 36 pt)
    shape('Heading 3', SZ_H3, True, WD_ALIGN_PARAGRAPH.LEFT,
          space_before=SZ_BODY * 0.7, indent_first=Cm(0),
          indent_left=Cm(1.27), keep=True)

    # Word สร้างสไตล์ TOC 1..TOC 3 ขึ้นเองตอนอัปเดตสารบัญ และสไตล์ที่สร้างเองนั้น
    # ไม่สืบฟอนต์จาก Normal — เลขหน้าในสารบัญจะออกมาเป็น AngsanaNew กับ Cambria
    # จึงต้องประกาศสไตล์พร้อม styleId ตรงตามที่ Word ใช้ (TOC1, TOC2, TOC3) ไว้ล่วงหน้า
    for lvl in (1, 2, 3):
        xml = (
            '<w:style xmlns:w="http://schemas.openxmlformats.org/'
            'wordprocessingml/2006/main" w:type="paragraph" w:styleId="TOC%d">'
            '<w:name w:val="toc %d"/><w:basedOn w:val="Normal"/>'
            '<w:next w:val="Normal"/><w:uiPriority w:val="39"/>'
            '<w:unhideWhenUsed/>'
            '<w:pPr><w:spacing w:after="0" w:line="240" w:lineRule="auto"/>'
            '<w:ind w:left="%d" w:firstLine="0"/>'
            '<w:jc w:val="left"/></w:pPr>'
            '<w:rPr><w:rFonts w:ascii="%s" w:hAnsi="%s" w:cs="%s" '
            'w:eastAsia="%s"/><w:b w:val="0"/><w:bCs w:val="0"/>'
            '<w:sz w:val="%d"/><w:szCs w:val="%d"/></w:rPr></w:style>'
        ) % (lvl, lvl, (lvl - 1) * 567, FONT, FONT, FONT, FONT,
             SZ_BODY * 2, SZ_BODY * 2)
        styles_el.append(parse_xml(xml))

    for nm, align in (('CaptionTable', WD_ALIGN_PARAGRAPH.LEFT),
                      ('CaptionFigure', WD_ALIGN_PARAGRAPH.CENTER)):
        st = doc.styles.add_style(nm, 1)       # 1 = WD_STYLE_TYPE.PARAGRAPH
        st.base_style = doc.styles['Normal']
        st.quick_style = True
        rPr = st.element.get_or_add_rPr()
        rf = _set(rPr, 'w:rFonts')
        for a in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
            rf.set(qn('w:' + a), FONT)
        _set(rPr, 'w:sz', val=str(SZ_BODY * 2))
        _set(rPr, 'w:szCs', val=str(SZ_BODY * 2))
        pf = st.paragraph_format
        pf.alignment = align
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(6)
        pf.space_after = Pt(6)
        pf.line_spacing = 1.0
        pf.keep_with_next = (nm == 'CaptionTable')

    # ต้องทำหลังสร้างสไตล์ทั้งหมด เพราะ Heading 1-3 ได้ attribute ธีมมาจากแม่แบบ
    strip_theme_fonts(styles_el)


# -------------------------------------------------------------- ส่วน (section)

def setup_section(sec, numfmt=None, start=None, title_page=False,
                  show_number=True):
    sec.page_height = Cm(29.7)
    sec.page_width = Cm(21.0)
    sec.top_margin = MARGIN_TOP
    sec.left_margin = MARGIN_LEFT
    sec.bottom_margin = MARGIN_BOTTOM
    sec.right_margin = MARGIN_RIGHT
    sec.header_distance = Cm(2.54)
    sec.footer_distance = Cm(1.5)

    # doc.add_section คัดลอก sectPr ของส่วนก่อนหน้ามาทั้งก้อน
    # ถ้าไม่ล้าง w:start ทิ้ง ทุกบทจะเริ่มนับหน้าที่ 1 ใหม่หมด
    sectPr = sec._sectPr
    if numfmt:
        pg = _set(sectPr, 'w:pgNumType', fmt=numfmt)
        if start:
            pg.set(qn('w:start'), str(start))
        elif pg.get(qn('w:start')) is not None:
            del pg.attrib[qn('w:start')]
    else:
        pg = sectPr.find(qn('w:pgNumType'))
        if pg is not None:
            sectPr.remove(pg)

    sec.different_first_page_header_footer = title_page

    hdr = sec.header
    hdr.is_linked_to_previous = False
    for p in list(hdr.paragraphs)[1:]:
        p._element.getparent().remove(p._element)
    p = hdr.paragraphs[0]
    p.text = ''
    p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.space_after = Pt(0)
    if show_number:
        add_field(p, 'PAGE', '1')

    if title_page:
        fh = sec.first_page_header
        fh.is_linked_to_previous = False
        fh.paragraphs[0].text = ''
    return sec


# --------------------------------------------------------------- ตัวแปลง markdown

RE_HEAD = re.compile(r'^(#{1,6})\s+(.*)$')
RE_TABLE_SEP = re.compile(r'^\|[\s:|-]+\|$')
RE_BULLET = re.compile(r'^(\s*)[-*]\s+(.*)$')
RE_NUM = re.compile(r'^(\s*)(\d+)\.\s+(.*)$')
RE_CAP_TABLE = re.compile(r'^ตารางที่\s+\d+\.\d+\s')
RE_CAP_FIG = re.compile(r'^รูปที่\s+(\d+)\.(\d+)\s')
RE_FIG_MARK = re.compile(
    r'^\*\*\[(?:ต้องแคปใหม่|ต้องแคป|ต้องวาด)\]\*\*\s*(รูปที่\s+(\d+)\.(\d+)\s.*)$')
RE_FIG_HERE = re.compile(r'^\*\(แทรกรูป(ที่นี่)?\)\*$')
RE_CAPLINE = re.compile(r'คำบรรยาย:\s*`(รูปที่\s+(\d+)\.(\d+)[^`]*)`')
# กล่อง 📸 ที่ตัวหนาเป็นคำบรรยายเต็มอยู่แล้ว (บทที่ 1) — ไม่มี — หรือ : คั่น
RE_FIG_BOLD = re.compile(r'📸\s*\*\*(รูปที่\s+(\d+)\.(\d+)\s[^*—:]+)\*\*')

# รูปที่มีไฟล์อยู่แล้วในโฟลเดอร์ picture/ — ฝังลงเล่มได้ทันที
FIG_FILES = {
    ('1', '3'): os.path.join('picture', 'Design.png'),
    ('3', '1'): os.path.join('picture', 'Design.png'),
    ('3', '2'): os.path.join('picture', 'fig-3-02.png'),
    ('4', '11'): os.path.join('picture', 'fig-4-02.png'),
}


def fig_picture(chap, num):
    rel = FIG_FILES.get((str(chap), str(num)))
    if not rel:
        return None
    path = os.path.join(ROOT, rel)
    return path if os.path.exists(path) else None


def parse_blocks(lines):
    """แปลง markdown เป็นลิสต์ของ block —
    ('head', level, text) ('para', text) ('table', rows, aligns)
    ('code', lines, lang) ('bullet', indent, text) ('num', indent, n, text)
    ('quote', blocks, raw_lines) ('hr',)"""
    blocks = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i].rstrip('\n')
        s = line.strip()

        if not s:
            i += 1
            continue

        if s in ('---', '***', '___'):
            blocks.append(('hr',))
            i += 1
            continue

        m = RE_HEAD.match(s)
        if m:
            blocks.append(('head', len(m.group(1)), m.group(2).strip()))
            i += 1
            continue

        if s.startswith('```'):
            lang = s[3:].strip()
            body = []
            i += 1
            while i < n and not lines[i].strip().startswith('```'):
                body.append(lines[i].rstrip('\n'))
                i += 1
            i += 1
            blocks.append(('code', body, lang))
            continue

        if s.startswith('>'):
            raw = []
            while i < n and lines[i].strip().startswith('>'):
                t = lines[i].rstrip('\n').strip()
                t = t[1:]
                if t.startswith(' '):
                    t = t[1:]
                raw.append(t)
                i += 1
            blocks.append(('quote', parse_blocks(raw), raw))
            continue

        if s.startswith('|'):
            rows = []
            aligns = None
            while i < n and lines[i].strip().startswith('|'):
                t = lines[i].strip()
                if RE_TABLE_SEP.match(t) and aligns is None and rows:
                    cells = [c.strip() for c in t.strip('|').split('|')]
                    aligns = []
                    for c in cells:
                        if c.endswith(':') and c.startswith(':'):
                            aligns.append('c')
                        elif c.endswith(':'):
                            aligns.append('r')
                        else:
                            aligns.append('l')
                else:
                    rows.append([c.strip() for c in t.strip('|').split('|')])
                i += 1
            blocks.append(('table', rows, aligns))
            continue

        m = RE_BULLET.match(line)
        if m:
            blocks.append(('bullet', len(m.group(1)) // 2, m.group(2).strip()))
            i += 1
            continue

        m = RE_NUM.match(line)
        if m:
            blocks.append(('num', len(m.group(1)) // 2, m.group(2),
                           m.group(3).strip()))
            i += 1
            continue

        # ย่อหน้า — รวมบรรทัดต่อเนื่องเข้าด้วยกัน
        buf = [s]
        i += 1
        while i < n:
            t = lines[i].rstrip('\n')
            ts = t.strip()
            if (not ts or ts.startswith('|') or ts.startswith('>')
                    or ts.startswith('```') or RE_HEAD.match(ts)
                    or RE_BULLET.match(t) or RE_NUM.match(t)
                    or ts in ('---', '***', '___')):
                break
            buf.append(ts)
            i += 1
        blocks.append(('para', ' '.join(buf)))
    return blocks


RE_INLINE = re.compile(
    r'(\{\{[^}]+\}\}|\*\*.+?\*\*|`[^`]+`|(?<!\*)\*[^*]+?\*(?!\*)|~~.+?~~)')

BLANKS = [0]       # นับช่องที่ต้องเติมของทั้งเล่ม


def emit_inline(p, text, size=SZ_BODY, bold=False, italic=False):
    """เขียนข้อความพร้อมรูปแบบ **หนา** *เอียง* `โค้ด` ลงในย่อหน้า
    และ {{ข้อความ}} คือช่องที่ผู้เขียนต้องเติม พิมพ์เป็นตัวแดงให้หาง่าย"""
    for part in RE_INLINE.split(text):
        if not part:
            continue
        if part.startswith('{{') and part.endswith('}}') and len(part) > 4:
            BLANKS[0] += 1
            style_run(p.add_run('[ ' + part[2:-2] + ' ]'), size=size,
                      bold=True, italic=italic, color='C00000')
        elif part.startswith('**') and part.endswith('**') and len(part) > 4:
            emit_inline(p, part[2:-2], size, True, italic)
        elif part.startswith('~~') and part.endswith('~~') and len(part) > 4:
            r = p.add_run(part[2:-2])
            style_run(r, size=size, bold=bold, italic=italic)
            r.font.strike = True
        elif part.startswith('`') and part.endswith('`') and len(part) > 2:
            style_run(p.add_run(part[1:-1]), size=size, bold=bold,
                      italic=italic, shade='EDEDED')
        elif (part.startswith('*') and part.endswith('*')
              and not part.startswith('**') and len(part) > 2):
            emit_inline(p, part[1:-1], size, bold, True)
        else:
            style_run(p.add_run(part), size=size, bold=bold, italic=italic)


def strip_md(text):
    text = re.sub(r'\*\*(.+?)\*\*', r'\1', text)
    text = re.sub(r'`([^`]+)`', r'\1', text)
    text = re.sub(r'\*([^*]+)\*', r'\1', text)
    return text.strip()


# ---------------------------------------------------------------- ตัวเขียนเนื้อหา

def add_para(doc, text, style='Normal', size=SZ_BODY, indent_first=None,
             indent_left=None, align=None):
    p = doc.add_paragraph(style=style)
    if indent_first is not None:
        p.paragraph_format.first_line_indent = indent_first
    if indent_left is not None:
        p.paragraph_format.left_indent = indent_left
    if align is not None:
        p.paragraph_format.alignment = align
    if text:
        emit_inline(p, text, size=size)
    return p


def add_table(doc, rows, aligns):
    if not rows:
        return
    ncol = max(len(r) for r in rows)
    rows = [r + [''] * (ncol - len(r)) for r in rows]
    t = doc.add_table(rows=len(rows), cols=ncol)
    t.style = 'Table Grid'
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = True
    _set(t._tbl.tblPr, 'w:tblW', w='5000', type='pct')
    _set(t._tbl.tblPr, 'w:tblLayout', type='autofit')

    amap = {'l': WD_ALIGN_PARAGRAPH.LEFT, 'c': WD_ALIGN_PARAGRAPH.CENTER,
            'r': WD_ALIGN_PARAGRAPH.RIGHT}
    for ri, row in enumerate(rows):
        for ci, cell in enumerate(row):
            c = t.cell(ri, ci)
            p = c.paragraphs[0]
            p.style = doc.styles['Normal']
            pf = p.paragraph_format
            pf.first_line_indent = Cm(0)
            pf.left_indent = Cm(0)
            pf.space_before = Pt(1)
            pf.space_after = Pt(1)
            pf.line_spacing = 1.0
            if ri == 0:
                pf.alignment = WD_ALIGN_PARAGRAPH.CENTER
            elif aligns and ci < len(aligns):
                pf.alignment = amap[aligns[ci]]
            else:
                pf.alignment = WD_ALIGN_PARAGRAPH.LEFT
            txt = cell.replace('<br>', ' ')
            emit_inline(p, txt, size=SZ_TABLE, bold=(ri == 0))
            if ri == 0:
                _set(c._tc.get_or_add_tcPr(), 'w:shd', val='clear',
                     color='auto', fill=SHADE_HEAD)
    # ความกว้างคอลัมน์ตามความยาวข้อความ  (Word autofit ให้คอลัมน์ท้ายแคบจนตัดคำทีละพยางค์)
    weights = []
    for ci in range(ncol):
        longest = max(len(strip_md(r[ci]).replace('<br>', ' ')) for r in rows)
        head = len(strip_md(rows[0][ci]))
        weights.append(min(max(longest, head, 5), 46))
    total_w = TEXT_WIDTH / 360000.0
    mins = [min(0.3 * max(w, 3) + 0.35, 3.2) if w <= 14 else 2.0 for w in weights]
    raw = [w / sum(weights) * total_w for w in weights]
    widths = [max(a, b) for a, b in zip(raw, mins)]
    scale = total_w / sum(widths)
    widths = [w * scale for w in widths]
    _set(t._tbl.tblPr, 'w:tblLayout', type='fixed')
    _set(t._tbl.tblPr, 'w:tblW', w=str(int(total_w / 2.54 * 1440)), type='dxa')
    t.autofit = False
    for ci, gc in enumerate(t._tbl.tblGrid.findall(qn('w:gridCol'))):
        gc.set(qn('w:w'), str(int(widths[ci] / 2.54 * 1440)))
    for r in t.rows:
        for ci, c in enumerate(r.cells):
            c.width = Cm(widths[ci])
    # แถวหัวตารางซ้ำเมื่อตารางยาวข้ามหน้า
    _set(t.rows[0]._tr.get_or_add_trPr(), 'w:tblHeader', val='1')
    stats['tables'] += 1
    doc.add_paragraph()
    return t


# ตารางแผนการฝึกสหกิจ  กริด 4 เดือน x 4 สัปดาห์  ช่องที่แรเงาคือสัปดาห์ที่ทำกิจกรรมนั้น
GANTT_MONTHS = ('กรกฎาคม 2569', 'สิงหาคม 2569', 'กันยายน 2569', 'ตุลาคม 2569')
GANTT_ROWS = (
    ('1. ศึกษาข้อมูลของโครงงาน', range(0, 7)),
    ('2. วางแผนการทำโครงงาน', range(5, 8)),
    ('3. ดำเนินโครงงาน', range(7, 10)),
    ('4. ตรวจสอบโครงงานและแก้ไขปัญหาการดำเนินงาน', range(9, 13)),
    ('5. จัดทำรายงานและเอกสารประกอบ', range(12, 16)),
)


def add_gantt(doc):
    t = doc.add_table(rows=2 + len(GANTT_ROWS), cols=17)
    t.style = 'Table Grid'
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    _set(t._tbl.tblPr, 'w:tblLayout', type='fixed')
    wid = [Cm(5.45)] + [Cm(0.575)] * 16
    for r in t.rows:
        for ci, c in enumerate(r.cells):
            c.width = wid[ci]

    def put(cell, text, size=SZ_TABLE, bold=False, align=WD_ALIGN_PARAGRAPH.CENTER, fill=None):
        p = cell.paragraphs[0]
        p.style = doc.styles['Normal']
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.left_indent = Cm(0)
        pf.space_before = Pt(1)
        pf.space_after = Pt(1)
        pf.line_spacing = 1.0
        pf.alignment = align
        if text:
            style_run(p.add_run(text), size=size, bold=bold)
        if fill:
            _set(cell._tc.get_or_add_tcPr(), 'w:shd', val='clear', color='auto', fill=fill)

    head = t.cell(0, 0).merge(t.cell(1, 0))
    put(head, 'กิจกรรม', bold=True, fill=SHADE_HEAD)
    _set(head._tc.get_or_add_tcPr(), 'w:vAlign', val='center')
    for m, name in enumerate(GANTT_MONTHS):
        c = t.cell(0, 1 + m * 4).merge(t.cell(0, 4 + m * 4))
        put(c, name, bold=True, fill=SHADE_HEAD)
        for w in range(4):
            put(t.cell(1, 1 + m * 4 + w), str(w + 1), size=12, fill=SHADE_HEAD)
    for ri, (name, weeks) in enumerate(GANTT_ROWS):
        put(t.cell(2 + ri, 0), name, align=WD_ALIGN_PARAGRAPH.LEFT)
        for k in range(16):
            put(t.cell(2 + ri, 1 + k), '', fill=('7F7F7F' if k in weeks else None))
    _set(t.rows[0]._tr.get_or_add_trPr(), 'w:tblHeader', val='1')
    _set(t.rows[1]._tr.get_or_add_trPr(), 'w:tblHeader', val='1')
    stats['tables'] += 1
    doc.add_paragraph()
    return t


def add_code(doc, body, lang=''):
    for ln in body:
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.left_indent = Cm(0.6)
        pf.space_before = Pt(0)
        pf.space_after = Pt(0)
        pf.line_spacing = 1.0
        pf.alignment = WD_ALIGN_PARAGRAPH.LEFT
        para_shade(p, SHADE_CODE)
        style_run(p.add_run(ln if ln else ' '), size=SZ_CODE, font=MONO)
    stats['code'] += 1
    doc.add_paragraph()


def add_note(doc, blocks, draft):
    """กล่องหมายเหตุ — draft=True คือกล่องที่ต้องลบก่อนส่ง"""
    fill = SHADE_DRAFT if draft else SHADE_NOTE
    first = True
    for b in blocks:
        if b[0] == 'table':
            add_table(doc, b[1], b[2])
            continue
        if b[0] == 'code':
            add_code(doc, b[1], b[2])
            continue
        if b[0] == 'hr':
            continue
        if b[0] == 'head':
            text = b[2]
        elif b[0] == 'bullet':
            text = '–  ' + b[2]
        elif b[0] == 'num':
            text = '%s. %s' % (b[2], b[3])
        else:
            text = b[1]
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.left_indent = Cm(0.5) + (Cm(0.5) if b[0] in ('bullet', 'num') else Cm(0))
        pf.right_indent = Cm(0.2)
        pf.space_before = Pt(3 if first else 0)
        pf.space_after = Pt(0)
        pf.line_spacing = 1.0
        pf.alignment = WD_ALIGN_PARAGRAPH.LEFT
        para_border(p, left=True)
        para_shade(p, fill)
        if draft and first:
            style_run(p.add_run('[ หมายเหตุของร่าง — ลบก่อนส่ง ]  '),
                      size=SZ_TABLE, bold=True, color='9C6500')
        emit_inline(p, text, size=SZ_TABLE)
        first = False
    stats['notes_draft' if draft else 'notes_body'] += 1
    doc.add_paragraph()


def add_figure_box(doc, chapter, num, picture=None):
    """ช่องว่างรอรูป — ให้ผู้เขียนลบกล่องนี้แล้ววางรูปจริงลงไป"""
    if picture and os.path.exists(picture):
        p = doc.add_paragraph()
        p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.first_line_indent = Cm(0)
        p.paragraph_format.space_before = Pt(6)
        p.paragraph_format.space_after = Pt(0)
        keep_with_next(p)
        p.add_run().add_picture(picture, width=Cm(12.5))
        stats['images'] += 1
        return
    t = doc.add_table(rows=1, cols=1)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    _set(t._tbl.tblPr, 'w:tblW', w='4200', type='pct')
    cell = t.cell(0, 0)
    row = t.rows[0]
    row.height = Cm(5.5)
    row.height_rule = WD_ROW_HEIGHT_RULE.AT_LEAST
    tcPr = cell._tc.get_or_add_tcPr()
    borders = _set(tcPr, 'w:tcBorders')
    for side in ('top', 'left', 'bottom', 'right'):
        e = OxmlElement('w:' + side)
        e.set(qn('w:val'), 'dashed')
        e.set(qn('w:sz'), '8')
        e.set(qn('w:color'), 'A6A6A6')
        borders.append(e)
    _set(tcPr, 'w:vAlign', val='center')
    p = cell.paragraphs[0]
    p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.first_line_indent = Cm(0)
    style_run(p.add_run('[ วางรูปที่ %s.%s ที่นี่ ]' % (chapter, num)),
              size=SZ_TABLE, bold=True, color='808080')
    p2 = cell.add_paragraph()
    p2.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p2.paragraph_format.first_line_indent = Cm(0)
    style_run(p2.add_run('ชื่อไฟล์ที่เตรียมไว้  fig-%s-%02d.png' %
                         (chapter, int(num))), size=12, color='A0A0A0')


def add_caption(doc, text, kind):
    style = 'CaptionTable' if kind == 'table' else 'CaptionFigure'
    p = doc.add_paragraph(style=style)
    emit_inline(p, text, size=SZ_BODY)
    if kind == 'figure':
        stats['figures'] += 1
    return p


# ------------------------------------------------------------ ตัวตัดสินชนิดกล่อง

DRAFT_MARKS = ('📸', '⚠️', '💡', 'ลบออกก่อนส่ง', 'ลบก่อนส่ง',
               'ต้องให้ผู้เขียน', 'ต้องแคป', 'ต้องเติม', 'สถานะเอกสาร',
               'สถานะร่าง', 'แหล่งข้อมูล:', 'หลักการที่ใช้:',
               'ยังเขียนไม่ได้', 'ยังเติมไม่ได้', 'ต้องตัดสินใจ',
               'ต้องตรวจ', 'ต้องยืนยัน')


def is_draft_note(raw):
    head = ' '.join(raw[:3])
    return any(m in head for m in DRAFT_MARKS)


# --------------------------------------------------------------- ประกอบแต่ละบท

def render_chapter(doc, blocks, chap_no):
    i = 0
    n = len(blocks)
    pending_fig = None        # (chapter, num) ที่รอคำบรรยาย
    while i < n:
        b = blocks[i]
        kind = b[0]

        if kind == 'hr':
            i += 1
            continue

        if kind == 'head':
            lvl, text = b[1], b[2]
            if lvl == 1:
                i += 1
                continue                      # ชื่อบทวางไว้แล้วจากภายนอก
            style = 'Heading 2' if lvl == 2 else 'Heading 3'
            p = doc.add_paragraph(style=style)
            emit_inline(p, text, size=SZ_H2 if lvl == 2 else SZ_H3, bold=True)
            i += 1
            continue

        if kind == 'quote':
            inner, raw = b[1], b[2]
            draft = is_draft_note(raw)
            add_note(doc, inner, draft)
            # กล่อง 📸 ที่ระบุคำบรรยายไว้ ให้วางช่องรูปกับคำบรรยายตามหลัง
            m = None
            for ln in raw:
                m = RE_CAPLINE.search(ln) or RE_FIG_BOLD.search(ln)
                if m:
                    break
            if m:
                cap = m.group(1).strip()
                add_figure_box(doc, m.group(2), m.group(3),
                               picture=fig_picture(m.group(2), m.group(3)))
                add_caption(doc, cap, 'figure')
            i += 1
            continue

        if kind == 'table':
            add_table(doc, b[1], b[2])
            i += 1
            continue

        if kind == 'code':
            add_code(doc, b[1], b[2])
            i += 1
            continue

        if kind == 'bullet':
            add_para(doc, '–  ' + b[2], indent_first=Cm(0),
                     indent_left=INDENT_FIRST + Cm(0.5 * b[1]),
                     align=WD_ALIGN_PARAGRAPH.JUSTIFY)
            i += 1
            continue

        if kind == 'num':
            add_para(doc, '%s. %s' % (b[2], b[3]), indent_first=Cm(0),
                     indent_left=INDENT_FIRST + Cm(0.5 * b[1]),
                     align=WD_ALIGN_PARAGRAPH.JUSTIFY)
            i += 1
            continue

        # ---- ย่อหน้า
        text = b[1]

        # ย่อหน้าที่ขึ้นต้นด้วยสัญลักษณ์เป็นหมายเหตุของร่าง ไม่ใช่เนื้อหารายงาน
        if text[:2] in ('📸', '⚠️', '💡', '❓') or text[0] in '📸⚠💡❓':
            add_note(doc, [('para', text)], True)
            mb = RE_FIG_BOLD.search(text)
            if mb:
                add_figure_box(doc, mb.group(2), mb.group(3),
                               picture=fig_picture(mb.group(2), mb.group(3)))
                add_caption(doc, mb.group(1).strip(), 'figure')
            i += 1
            continue

        m = RE_FIG_MARK.match(text)
        if m:
            add_figure_box(doc, m.group(2), m.group(3),
                           picture=fig_picture(m.group(2), m.group(3)))
            add_caption(doc, m.group(1), 'figure')
            i += 1
            continue

        if RE_FIG_HERE.match(text):
            nxt = blocks[i + 1] if i + 1 < n else None
            if nxt and nxt[0] == 'para':
                mf = RE_CAP_FIG.match(nxt[1])
                if mf:
                    pic = fig_picture(mf.group(1), mf.group(2))
                    add_figure_box(doc, mf.group(1), mf.group(2), picture=pic)
                    add_caption(doc, nxt[1], 'figure')
                    i += 2
                    continue
            add_figure_box(doc, str(chap_no), '0')
            i += 1
            continue

        if RE_CAP_TABLE.match(text):
            nxt = blocks[i + 1] if i + 1 < n else None
            if nxt and nxt[0] == 'para' and nxt[1].strip() == '[[GANTT]]':
                add_caption(doc, text, 'table')
                add_gantt(doc)
                i += 2
                continue
            if nxt and nxt[0] == 'table':
                add_caption(doc, text, 'table')
                add_table(doc, nxt[1], nxt[2])
                i += 2
                continue

        add_para(doc, text)
        i += 1
    return


def load_chapter(fname):
    path = os.path.join(MD_CH, fname)
    with open(path, encoding='utf-8') as f:
        lines = f.read().split('\n')

    body = []
    for ln in lines:
        if ln.startswith('# ภาคผนวกของร่าง'):
            break
        if ln.startswith('## เอกสารอ้างอิง'):
            break
        body.append(ln)

    blocks = parse_blocks(body)
    # ตัดกล่องสถานะร่างที่หัวไฟล์ออก
    out = []
    for idx, b in enumerate(blocks):
        if (idx <= 1 and b[0] == 'quote'
                and any('สถานะ' in ln for ln in b[2][:2])):
            continue
        out.append(b)
    return out


def load_references():
    fin = os.path.join(MD, 'final', 'เอกสารอ้างอิง.md')
    if FINAL and os.path.exists(fin):
        return [ln.strip() for ln in open(fin, encoding='utf-8') if re.match(r'^\[\d+\]', ln.strip())]
    path = os.path.join(MD, 'บทที่-2-ทฤษฎีที่เกี่ยวข้อง.md')
    with open(path, encoding='utf-8') as f:
        text = f.read()
    seg = text.split('## เอกสารอ้างอิง', 1)[1]
    seg = seg.split('# ภาคผนวกของร่าง', 1)[0]
    refs = []
    for ln in seg.split('\n'):
        if re.match(r'^\[\d+\]', ln.strip()):
            refs.append(ln.strip())
    return refs


# -------------------------------------------------------------- ส่วนหน้า/ท้ายเล่ม

def centered(doc, text, size=SZ_BODY, bold=False, space_before=0,
             space_after=0):
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.alignment = WD_ALIGN_PARAGRAPH.CENTER
    pf.first_line_indent = Cm(0)
    pf.space_before = Pt(space_before)
    pf.space_after = Pt(space_after)
    pf.line_spacing = 1.0
    if text:
        emit_inline(p, text, size=size, bold=bold)
    return p


def flat(doc, text, size=SZ_BODY, bold=False, left=Cm(0), align=None,
         space_after=0, space_before=0):
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.alignment = align if align is not None else WD_ALIGN_PARAGRAPH.LEFT
    pf.first_line_indent = Cm(0)
    pf.left_indent = left
    pf.space_after = Pt(space_after)
    pf.space_before = Pt(space_before)
    pf.line_spacing = 1.0
    if text:
        emit_inline(p, text, size=size, bold=bold)
    return p


def page_break(doc):
    p = doc.add_paragraph()
    p.paragraph_format.first_line_indent = Cm(0)
    p.add_run().add_break(WD_BREAK.PAGE)


def fix_page_breaks(doc):
    """ย่อหน้าที่มีแต่ page break กินที่หนึ่งบรรทัด ถ้าหน้าก่อนหน้าเต็มพอดี
    ย่อหน้านั้นจะไหลไปหน้าใหม่แล้วดันเนื้อหาต่อไปอีกหน้า เกิดหน้าว่างคั่น
    จึงย้ายไปเป็นคุณสมบัติ pageBreakBefore ของย่อหน้าถัดไปแทน แล้วลบย่อหน้าเปล่าทิ้ง"""
    moved = 0
    for p in list(doc.element.body.iter(qn('w:p'))):
        brs = [b for b in p.iter(qn('w:br'))
               if b.get(qn('w:type')) == 'page']
        if not brs:
            continue
        if ''.join(t.text or '' for t in p.iter(qn('w:t'))).strip():
            continue
        nxt = p.getnext()
        while nxt is not None and nxt.tag not in (qn('w:p'), qn('w:tbl')):
            nxt = nxt.getnext()
        if nxt is None or nxt.tag != qn('w:p'):
            continue
        _set(nxt.get_or_add_pPr(), 'w:pageBreakBefore', val='1')
        p.getparent().remove(p)
        moved += 1
    return moved


def build_legend(doc):
    centered(doc, 'หน้านี้ไม่ใช่ส่วนของรายงาน — ลบทิ้งก่อนส่ง',
             size=SZ_H2, bold=True, space_after=12)
    flat(doc, 'ไฟล์นี้สร้างอัตโนมัติจากไฟล์ร่าง markdown ในโฟลเดอร์ `md/` '
              'ด้วยสคริปต์ `tools/md2docx.py` '
              'รูปแบบหน้ากระดาษ ฟอนต์ ขนาดตัวอักษร ตำแหน่งเลขหน้า '
              'คำบรรยายรูปและตาราง ตั้งตามข้อกำหนดใน '
              '`report/exam_report/รูปเล่มสหกิจสำหรับคณะวิศวะ.pdf` แล้ว',
         align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_after=10)

    flat(doc, 'สิ่งที่ต้องทำใน Word ก่อนส่ง', size=SZ_H3, bold=True,
         space_after=6)
    items = [
        'กด Ctrl+A แล้วกด F9 หนึ่งครั้ง เพื่อให้ Word สร้างสารบัญ '
        'สารบัญตาราง สารบัญรูป และเลขหน้าทั้งเล่ม '
        '(ถ้ามีกล่องถาม ให้เลือก Update entire table)',
        'ลบ **กล่องพื้นสีเหลือง** ทุกกล่อง — เป็นหมายเหตุของร่าง '
        'บอกว่าตรงนั้นยังขาดข้อมูลอะไร หรือต้องถ่ายภาพอะไร ไม่ใช่เนื้อหารายงาน',
        'กล่อง **พื้นสีเทา** เป็นหมายเหตุที่อยู่ในเนื้อหาได้ '
        'แต่ควรอ่านแล้วตัดสินใจว่าจะกลืนเข้าไปในย่อหน้าปกติหรือคงไว้',
        'แทนที่ **กรอบเส้นประ** ทุกกรอบด้วยรูปจริง '
        'โดยวางรูปแบบ In Line with Text แล้วลบกรอบทิ้ง '
        'ชื่อไฟล์ที่ต้องถ่ายเขียนไว้ในกรอบแล้ว '
        'รายการถ่ายภาพอยู่ใน `md/ด่วน-ทำก่อนย้ายเครื่อง.md`',
        'เติมช่อง `[...]` ทุกช่องในปก หน้าอนุมัติ และประวัติผู้จัดทำ',
        'ตัดสินใจเรื่องชื่อโครงงาน แล้วแก้ให้เหมือนกันทุกตัวอักษรใน 5 ที่ '
        'คือ ปก · หน้าอนุมัติ · บทสรุป · Summary · สารบัญ',
        'พิมพ์หัว **(ต่อ)** ให้ทุกส่วนที่ยาวข้ามหน้า — รูปเล่มจริงเขียนติดกัน '
        'ไม่เว้นวรรค คือ **สารบัญ(ต่อ)** · **สารบัญตาราง(ต่อ)** · '
        '**สารบัญรูป(ต่อ)** และ **เอกสารอ้างอิง(ต่อ)** ด้วย '
        '— Word ใส่ให้เองไม่ได้ ต้องพิมพ์เองหลังอัปเดตเลขหน้าเสร็จ',
        'แทรก **รายงานปฏิบัติงานประจำสัปดาห์** ลงในภาคผนวก ก. '
        'ท้ายเล่มมีหน้าคั่นกับกล่องอธิบายวางไว้ให้แล้ว '
        'รูปเล่มจริงที่ใช้เทียบแนบไว้ 27 หน้า หน้าละสัปดาห์',
        'หน้าในภาคผนวกไม่แสดงเลขหน้า ซึ่งตั้งไว้แล้ว '
        'แต่ Word จะนับหน้าต่อไปเรื่อย ๆ ทำให้หน้าประวัติผู้จัดทำได้เลขที่เลยมา '
        'ถ้าต้องการให้ต่อจากเอกสารอ้างอิงเหมือนรูปเล่มจริง '
        'ให้คลิกที่เลขหน้าของหน้าประวัติ แล้วสั่ง Format Page Numbers > Start at '
        'ใส่เลขถัดจากหน้าสุดท้ายของเอกสารอ้างอิง',
        'ตรวจว่าไม่มีตารางหรือรูปใดถูกตัดคร่อมสองหน้า '
        'และไม่มีหัวข้อค้างอยู่บรรทัดสุดท้ายของหน้า',
    ]
    for k, it in enumerate(items, 1):
        flat(doc, '%d.  %s' % (k, it), left=Cm(0.8), space_after=4,
             align=WD_ALIGN_PARAGRAPH.JUSTIFY)

    flat(doc, 'ข้อที่ยังไม่ตรงตามข้อกำหนด และเป็นการตัดสินใจของผู้เขียน',
         size=SZ_H3, bold=True, space_before=12, space_after=6)
    for it in [
        'ข้อความในตารางใช้ขนาด 14 ไม่ใช่ 16 เพราะบางตารางมีห้าคอลัมน์ '
        'ซึ่งที่ขนาด 16 จะล้นหน้ากระดาษ ถ้าอาจารย์กำหนดให้ใช้ 16 ทั้งเล่ม '
        'ให้แก้ค่า `SZ_TABLE` ในสคริปต์แล้วสร้างใหม่',
        'ตัวอย่างโปรแกรมใช้ฟอนต์ Consolas ขนาด 11 '
        'เพราะโปรแกรมภาษา C# ต้องอาศัยความกว้างตัวอักษรเท่ากันจึงจะอ่านการย่อหน้าได้ '
        'ข้อกำหนดระบุว่าให้ใช้ TH Sarabun New ทั้งเล่ม — '
        'ถ้าต้องยึดตามนั้นเคร่งครัด ให้แก้ค่า `MONO` ในสคริปต์',
        'สารบัญตารางและสารบัญรูปที่ Word สร้างจะขึ้นต้นด้วยคำว่า '
        '"ตารางที่" และ "รูปที่" ตามคำบรรยายจริง '
        'ขณะที่ตัวอย่างในแบบฟอร์มพิมพ์เฉพาะเลข — แก้ได้หลังกด F9',
    ]:
        flat(doc, '–  ' + it, left=Cm(0.8), space_after=4,
             align=WD_ALIGN_PARAGRAPH.JUSTIFY)


def build_cover(doc):
    # วางตามแบบฟอร์มหน้า 5 และตามรูปเล่มจริงที่ตรวจแล้ว — ขึ้นต้นด้วยชื่อโครงงาน
    # ไม่มีบรรทัด "รายงานการปฏิบัติงานสหกิจศึกษา" นำหน้า
    for _ in range(2):
        centered(doc, '')
    centered(doc, TITLE_TH, size=SZ_H2, bold=True, space_after=6)
    centered(doc, TITLE_EN, size=SZ_H2, bold=True, space_after=24)
    for _ in range(4):
        centered(doc, '')
    centered(doc, '{{ชื่อ-นามสกุล พร้อมคำนำหน้า ไม่ต้องใส่รหัสนักศึกษา}}',
             space_after=24)
    for _ in range(5):
        centered(doc, '')
    centered(doc, 'โครงงานสหกิจศึกษานี้เป็นส่วนหนึ่งของการศึกษาตามหลักสูตร')
    centered(doc, 'ปริญญาวิศวกรรมศาสตรบัณฑิต สาขา{{ชื่อสาขาวิชา}}')
    centered(doc, 'คณะวิศวกรรมศาสตร์')
    centered(doc, 'สถาบันเทคโนโลยีไทย - ญี่ปุ่น')
    centered(doc, 'ปีการศึกษา 2569')


def build_approval(doc):
    rows = [
        ('ชื่อโครงงาน', TITLE_TH),
        ('', TITLE_EN),
        ('ผู้เขียน', '{{ชื่อ-นามสกุล}}'),
        ('สาขาวิชา', '{{สาขาวิชา}}'),
        ('อาจารย์ที่ปรึกษาสหกิจศึกษา', '{{ชื่ออาจารย์ที่ปรึกษาสหกิจศึกษา}}'),
    ]
    for label, val in rows:
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.left_indent = Cm(0)
        pf.space_after = Pt(2)
        pf.line_spacing = 1.0
        pf.tab_stops.add_tab_stop(Cm(5.2))
        style_run(p.add_run(label + '\t'))
        emit_inline(p, val)
    doc.add_paragraph()
    flat(doc, 'คณะวิศวกรรมศาสตร์ สถาบันเทคโนโลยีไทย-ญี่ปุ่น '
              'อนุมัติให้โครงงานสหกิจศึกษาฉบับนี้เป็นส่วนหนึ่งของการศึกษา'
              'ตามหลักสูตรปริญญาวิศวกรรมศาสตรบัณฑิต',
         align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_after=18)

    def sign(role):
        flat(doc, '......................................................  ' + role,
             left=Cm(1.2), space_before=14, space_after=2)
        flat(doc, '(....................................................)',
             left=Cm(1.2), space_after=2)

    sign('ประธานสหกิจศึกษาสาขา')
    flat(doc, 'วันที่ ......... เดือน .............................. พ.ศ. ............',
         left=Cm(1.2), space_after=10)
    flat(doc, 'คณะกรรมการสอบโครงงานสหกิจ', space_before=8, space_after=0)
    sign('ประธานกรรมการ')
    sign('กรรมการ')
    sign('อาจารย์ที่ปรึกษาสหกิจ')
    for _ in range(2):
        centered(doc, '')
    centered(doc, 'ลิขสิทธิ์ของสถาบันเทคโนโลยีไทย – ญี่ปุ่น', space_before=14)


def build_abstract(doc, md_text, english=False):
    head_rows = ([('Title of Project', TITLE_EN),
                  ('Writer / Submitted by', '{{ชื่อ-นามสกุล ภาษาอังกฤษ}}'),
                  ('Faculty', 'Engineering'),
                  ('Major', '{{สาขาวิชา ภาษาอังกฤษ}}'),
                  ("Advisor's", '{{ชื่ออาจารย์ที่ปรึกษา ภาษาอังกฤษ}}'),
                  ('Job Supervisor', '{{ชื่อพนักงานที่ปรึกษา ภาษาอังกฤษ}}'),
                  ("Company's name", 'Lumas Corp.')]
                 if english else
                 [('ชื่อโครงงาน', TITLE_TH),
                  ('ผู้เขียน', '{{ชื่อ-นามสกุล}}'),
                  ('คณะ', 'วิศวกรรมศาสตร์'),
                  ('สาขาวิชา', '{{สาขาวิชา}}'),
                  ('อาจารย์ที่ปรึกษาสหกิจ', '{{ชื่ออาจารย์ที่ปรึกษาสหกิจศึกษา}}'),
                  ('พนักงานที่ปรึกษา', '{{ชื่อพนักงานที่ปรึกษา}}'),
                  ('ชื่อบริษัท', 'บริษัท Lumas corp')])
    for label, val in head_rows:
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_after = Pt(1)
        pf.line_spacing = 1.0
        pf.tab_stops.add_tab_stop(Cm(5.6))
        style_run(p.add_run(label + '\t'))
        emit_inline(p, val)
    doc.add_paragraph()
    head = centered(doc, 'Summary' if english else 'บทสรุป', size=SZ_H2,
                    bold=True, space_after=10)
    bookmark(head, 'bmSummaryEn' if english else 'bmSummaryTh')
    for para in md_text:
        add_para(doc, para, align=WD_ALIGN_PARAGRAPH.JUSTIFY)


def extract_quoted(fname, start_marker, stop_marker):
    """ดึงย่อหน้าที่อยู่ในบล็อก > ... ระหว่างสองหัวข้อ"""
    with open(os.path.join(MD, fname), encoding='utf-8') as f:
        text = f.read()
    seg = text.split(start_marker, 1)[1].split(stop_marker, 1)[0]
    paras = []
    buf = []
    started = False
    for ln in seg.split('\n'):
        s = ln.strip()
        if started and s.startswith('#'):
            break            # จบที่หัวข้อย่อยถัดไป เช่น "### ร่างฉบับยาว"
        if s.startswith('>'):
            started = True
            s = s[1:].strip()
            if not s:
                if buf:
                    paras.append(' '.join(buf))
                    buf = []
                continue
            if s.startswith('💡') or s.startswith('⚠️'):
                if buf:
                    paras.append(' '.join(buf))
                    buf = []
                break
            buf.append(s)
        else:
            if buf:
                paras.append(' '.join(buf))
                buf = []
    if buf:
        paras.append(' '.join(buf))
    return paras


def build_ack(doc):
    bookmark(centered(doc, 'กิตติกรรมประกาศ', size=SZ_H2, bold=True,
                      space_after=12), 'bmAck')
    paras = extract_quoted('ส่วนหน้าเล่มและส่วนท้ายเล่ม.md',
                           '# 5. กิตติกรรมประกาศ', '# 6. สารบัญ')
    for para in paras:
        if '[ชื่อ-นามสกุล]' in para or '[เดือน พ.ศ.]' in para:
            continue
        add_para(doc, para, align=WD_ALIGN_PARAGRAPH.JUSTIFY)
    doc.add_paragraph()
    flat(doc, '{{ชื่อ-นามสกุล}}', left=Cm(9.5), space_before=12)
    flat(doc, '{{เดือน พ.ศ. ที่เขียนรายงาน}}', left=Cm(9.5))


def build_toc(doc):
    bookmark(centered(doc, 'สารบัญ', size=SZ_H2, bold=True, space_after=10),
             'bmToc')
    p = doc.add_paragraph()
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    style_run(p.add_run('หน้า'), bold=True)
    # เลขหน้าของส่วนหน้าเล่มใช้ field PAGEREF ชี้ไปที่ bookmark ของแต่ละหัวข้อ
    # Word จึงคำนวณเป็น ก ข ค ตามรูปแบบเลขหน้าของส่วนนั้นให้เอง
    for label, bm in (('บทสรุปภาษาไทย', 'bmSummaryTh'),
                      ('บทสรุปภาษาอังกฤษ', 'bmSummaryEn'),
                      ('กิตติกรรมประกาศ', 'bmAck'),
                      ('สารบัญ', 'bmToc'),
                      ('สารบัญตาราง', 'bmTocTables'),
                      ('สารบัญรูป', 'bmTocFigures')):
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_after = Pt(0)
        pf.tab_stops.add_tab_stop(TEXT_WIDTH, 2)   # 2 = RIGHT
        style_run(p.add_run(label + '\t'))
        add_field(p, 'PAGEREF %s \\h' % bm, '?')
    doc.add_paragraph()
    flat(doc, 'บทที่', bold=True, space_after=2)
    p = doc.add_paragraph()
    p.paragraph_format.first_line_indent = Cm(0)
    add_field(p, 'TOC \\o "1-2" \\h \\z \\u',
              'วางเคอร์เซอร์ในเล่ม กด Ctrl+A แล้ว F9 '
              'เพื่อให้ Word สร้างสารบัญจากหัวข้อจริงพร้อมเลขหน้า '
              '— รายการจะรวมเอกสารอ้างอิงและประวัติผู้จัดทำโครงงานมาด้วย '
              'เพราะสองหัวข้อนั้นใช้ระดับหัวข้อเดียวกับชื่อบท')


def build_toc_of(doc, title, label_col, style_name, bm=None):
    head = centered(doc, title, size=SZ_H2, bold=True, space_after=10)
    if bm:
        bookmark(head, bm)
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.first_line_indent = Cm(0)
    pf.tab_stops.add_tab_stop(TEXT_WIDTH, 2)
    style_run(p.add_run(label_col + '\tหน้า'), bold=True)
    p = doc.add_paragraph()
    p.paragraph_format.first_line_indent = Cm(0)
    add_field(p, 'TOC \\h \\z \\t "%s,1"' % style_name,
              'กด Ctrl+A แล้ว F9 เพื่อสร้างรายการนี้จากคำบรรยายจริงในเล่ม')


def build_references(doc, refs):
    p = doc.add_paragraph(style='Heading 1')
    style_run(p.add_run('เอกสารอ้างอิง'), size=SZ_CHAPTER, bold=True)
    for r in refs:
        m = re.match(r'^(\[\d+\])\s*(.*)$', r)
        para = doc.add_paragraph()
        pf = para.paragraph_format
        pf.first_line_indent = Cm(-1.2)
        pf.left_indent = Cm(1.2)
        pf.space_after = Pt(6)
        pf.line_spacing = 1.0
        pf.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        style_run(para.add_run(m.group(1) + '\t'))
        emit_inline(para, m.group(2))


def build_bio(doc):
    p = doc.add_paragraph(style='Heading 1')
    style_run(p.add_run('ประวัติผู้จัดทำโครงงาน'), size=SZ_CHAPTER, bold=True)
    rows = [
        ('ชื่อ - นามสกุล', '{{ชื่อ-นามสกุล พร้อมคำนำหน้า}}'),
        ('วัน เดือน ปี เกิด', '{{วัน เดือน ปี เกิด}}'),
    ]
    for label, val in rows:
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_after = Pt(6)
        pf.tab_stops.add_tab_stop(Cm(5.0))
        style_run(p.add_run(label + '\t'))
        emit_inline(p, val)
    # โครงของหน้านี้ตามรูปเล่มจริง — มีช่องทุนการศึกษาและประวัติการฝึกอบรมด้วย
    flat(doc, 'ประวัติการศึกษา', space_before=8, space_after=6)
    edu = [
        ('ระดับมัธยมศึกษา', 'มัธยมศึกษาตอนต้น พ.ศ. {{ปีที่จบ ม.ต้น}}',
         '{{ชื่อโรงเรียน}}'),
        ('', 'มัธยมศึกษาตอนปลาย พ.ศ. {{ปีที่จบ ม.ปลาย}}', '{{ชื่อโรงเรียน}}'),
        ('ระดับอุดมศึกษา',
         'วิศวกรรมศาสตรบัณฑิต สาขาวิศวกรรม{{สาขาวิชา}}',
         'สถาบันเทคโนโลยีไทย-ญี่ปุ่น  พ.ศ. {{ปีการศึกษา}}'),
    ]
    for lvl, a, b in edu:
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.left_indent = Cm(1.2)
        pf.space_after = Pt(0)
        pf.tab_stops.add_tab_stop(Cm(5.2))
        style_run(p.add_run(lvl + '\t'))
        emit_inline(p, a)
        flat(doc, b, left=Cm(6.4), space_after=8)
    for label, val in (('สถานที่ฝึกงานสหกิจศึกษา', 'บริษัท Lumas corp'),
                       ('ตำแหน่ง', 'นักพัฒนาเกม (Game Developer)'),
                       ('ทุนการศึกษา', '{{ถ้าไม่มี พิมพ์ว่า - ไม่มี -}}'),
                       ('ประวัติการฝึกอบรม', '{{ถ้าไม่มี พิมพ์ว่า - ไม่มี -}}')):
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_after = Pt(6)
        pf.tab_stops.add_tab_stop(Cm(5.0))
        style_run(p.add_run(label + '\t'))
        emit_inline(p, val)


def build_appendix(doc):
    """โครงภาคผนวก — รูปเล่มจริงมีรายงานปฏิบัติงานประจำสัปดาห์แนบไว้ 27 หน้า
    ไฟล์ของผู้เขียนเองจึงต้องแนบชุดเดียวกัน ส่วนนี้วางโครงไว้ให้แทรก"""
    for _ in range(9):
        centered(doc, '')
    centered(doc, 'ภาคผนวก', size=SZ_CHAPTER, bold=True)
    page_break(doc)
    for _ in range(9):
        centered(doc, '')
    centered(doc, 'ภาคผนวก ก.', size=SZ_H2, bold=True, space_after=6)
    centered(doc, 'รายงานปฏิบัติงานประจำสัปดาห์', size=SZ_H2, bold=True)
    page_break(doc)
    if FINAL:
        # ฉบับจริงใช้ช่องสีแดงแบบเดียวกับที่อื่นในเล่ม ไม่ใช้กล่องหมายเหตุ
        for _ in range(6):
            centered(doc, '')
        centered(doc, '{{แทรกรายงานปฏิบัติงานประจำสัปดาห์ที่ส่งศูนย์สหกิจศึกษา '
                      'หน้าละสัปดาห์ เรียงตามลำดับ แล้วลบหน้านี้}}')
        return
    add_note(doc, [('para',
        '**แทรกรายงานปฏิบัติงานประจำสัปดาห์ที่นี่** — '
        'รูปเล่มที่ผ่านการอนุมัติแล้วซึ่งใช้เทียบ แนบแบบฟอร์มของ '
        'ศูนย์สหกิจศึกษาและจัดหางาน สถาบันเทคโนโลยีไทย-ญี่ปุ่น ไว้ '
        '**27 หน้า หนึ่งหน้าต่อหนึ่งสัปดาห์** เรียงตามลำดับสัปดาห์ '
        'และหน้าในภาคผนวกเหล่านั้น **ไม่แสดงเลขหน้า**'),
        ('para',
        'เอกสารชุดนี้เป็นสิ่งที่ผู้เขียนส่งศูนย์สหกิจศึกษาอยู่แล้วทุกสัปดาห์ '
        'ให้รวบรวมไฟล์หรือสำเนาที่เซ็นแล้วมาแทรกแทนหน้านี้ '
        'ถ้าช่วงฝึกงานคือกรกฎาคมถึงตุลาคม จะได้ประมาณ 16 ถึง 18 สัปดาห์'),
        ('para',
        'ถ้าสาขาวิชากำหนดให้มีภาคผนวกอื่นด้วย เช่น '
        'ภาคผนวก ข. สำหรับตัวอย่างโปรแกรมหรือคู่มือการใช้งาน '
        'ให้เพิ่มหน้าคั่นแบบเดียวกันนี้ต่อท้าย')], True)


# ------------------------------------------------------------------------ main

def main():
    chapters = [c for c in CHAPTERS
                if os.path.exists(os.path.join(MD_CH, c[2]))]
    if not chapters:
        print('ไม่พบไฟล์บทใดใน', MD_CH)
        return 1
    has_ch2 = any(c[0] == 'บทที่ 2' for c in chapters)

    doc = Document()
    build_styles(doc)

    # ---- ส่วนที่ 1  คำชี้แจง / ปก / หน้าอนุมัติ — ไม่มีเลขหน้า
    setup_section(doc.sections[0], show_number=False)
    if not FINAL:
        build_legend(doc)
        page_break(doc)
    build_cover(doc)
    page_break(doc)
    build_approval(doc)

    # ---- ส่วนที่ 2  บทสรุป → สารบัญรูป — เลขหน้า ก ข ค
    sec = doc.add_section(WD_SECTION.NEW_PAGE)
    setup_section(sec, numfmt='thaiLetters', start=1)

    th = extract_quoted('ส่วนหน้าเล่มและส่วนท้ายเล่ม.md',
                        '# 3. บทสรุป (ภาษาไทย)', '# 4. Summary')
    en = extract_quoted('ส่วนหน้าเล่มและส่วนท้ายเล่ม.md',
                        '# 4. Summary (ภาษาอังกฤษ)', '# 5. กิตติกรรมประกาศ')
    build_abstract(doc, th, english=False)
    page_break(doc)
    build_abstract(doc, en, english=True)
    page_break(doc)
    build_ack(doc)
    page_break(doc)
    build_toc(doc)
    page_break(doc)
    build_toc_of(doc, 'สารบัญตาราง', 'ตารางที่', 'CaptionTable', 'bmTocTables')
    page_break(doc)
    build_toc_of(doc, 'สารบัญรูป', 'รูปที่', 'CaptionFigure', 'bmTocFigures')

    # ---- ส่วนที่ 3-7  บทที่ 1 ถึง 5 — เลขหน้า 1 2 3 หน้าแรกของบทไม่แสดง
    for idx, (num, name, fname) in enumerate(chapters):
        sec = doc.add_section(WD_SECTION.NEW_PAGE)
        setup_section(sec, numfmt='decimal',
                      start=1 if idx == 0 else None, title_page=True)
        p = doc.add_paragraph(style='Heading 1')
        style_run(p.add_run(num), size=SZ_CHAPTER, bold=True)
        p.add_run().add_break()
        style_run(p.add_run(name), size=SZ_CHAPTER, bold=True)
        render_chapter(doc, load_chapter(fname), idx + 1)

    # ---- ส่วนที่ 8  เอกสารอ้างอิง
    # รายการ [1]–[29] ถูกอ้างจากบทที่ 2 ถึง 5 ทั้งหมด บทที่ 1 ไม่มีเลขอ้างอิงเลย
    # จึงยังไม่ใส่รายการอ้างอิงถ้าเล่มยังมีแต่บทที่ 1
    if has_ch2 or not FINAL:
        sec = doc.add_section(WD_SECTION.NEW_PAGE)
        setup_section(sec, numfmt='decimal')
        build_references(doc, load_references())

    # ---- ส่วนที่ 9  ภาคผนวก — ไม่แสดงเลขหน้า ตามรูปเล่มจริง
    sec = doc.add_section(WD_SECTION.NEW_PAGE)
    setup_section(sec, show_number=False)
    build_appendix(doc)

    # ---- ส่วนที่ 10  ประวัติผู้จัดทำโครงงาน
    sec = doc.add_section(WD_SECTION.NEW_PAGE)
    setup_section(sec, numfmt='decimal')
    build_bio(doc)

    fix_page_breaks(doc)
    outdir = os.path.dirname(OUT)
    if outdir and not os.path.isdir(outdir):
        os.makedirs(outdir)
    doc.save(OUT)
    print('โหมด:', 'ฉบับจริง' if FINAL else 'ฉบับร่าง')
    print('บทที่สร้าง:', ' · '.join(c[0] for c in chapters))
    print('เขียนไฟล์แล้ว:', OUT)
    print('ตาราง %d · รูป %d · กล่องหมายเหตุของร่าง %d · กล่องหมายเหตุในเนื้อหา %d '
          '· บล็อกโปรแกรม %d · รูปที่ฝังได้จริง %d · ช่องที่ต้องเติม %d'
          % (stats['tables'], stats['figures'], stats['notes_draft'],
             stats['notes_body'], stats['code'], stats['images'], BLANKS[0]))
    if FINAL and (stats['notes_draft'] or stats['notes_body']):
        print('!! ฉบับจริงไม่ควรมีกล่องหมายเหตุ — ตรวจไฟล์ใน md/final/')
    return 0


if __name__ == '__main__':
    sys.exit(main() or 0)
