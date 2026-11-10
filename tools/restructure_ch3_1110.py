# -*- coding: utf-8 -*-
"""บท 3 ให้ตรงคู่มือ: เลื่อนเลขตาราง 3.3-3.10 เป็น 3.2-3.9 และจัดหัวข้อ 3.2.1 / 3.2.3 ตามคู่มือ
  3.2.1 ประชากรและกลุ่มตัวอย่าง   (เนื้อหา 'ขอบเขตของโครงงาน' ย้ายไปท้ายหัวข้อ 3.1)
  3.2.3 การเก็บรวบรวมข้อมูล        (เนื้อหา 'ทรัพยากรภายนอก' ย้ายไปท้ายหัวข้อ 3.2.2)
ใช้ครั้งเดียว ปิด Word ก่อน สำรองก่อนเสมอ   py tools/restructure_ch3_1110.py SRC DST"""
import copy
import re
import sys
import os

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf                       # noqa: E402
from docx import Document                       # noqa: E402
from docx.oxml import OxmlElement               # noqa: E402

W = cf.W

NEW_321 = ('โครงงานนี้เป็นการพัฒนาเกมเพื่อฝึกกระบวนการพัฒนาซอฟต์แวร์ ไม่ใช่การสำรวจความคิดเห็น จึงไม่มีประชากรเชิงสถิติ '
           'กลุ่มตัวอย่างมีเพียงผู้ทดลองเล่นเกม คือเพื่อนของนักศึกษาจำนวน 5 คน ซึ่งเล่นเกมจนจบคนละหนึ่งรอบ '
           'วิธีทดลองเล่นอยู่ในหัวข้อ 3.3.4 และผลการสังเกตอยู่ในหัวข้อ 4.2.8')
NEW_323 = ('ข้อมูลที่เก็บในโครงงานนี้มีสามอย่าง อย่างแรกคือผลการรันชุดทดสอบอัตโนมัติ 15 กรณี '
           'อย่างที่สองคือข้อความใน Console ของ Unity ว่ามีข้อผิดพลาดหรือไม่ '
           'และอย่างที่สามคือข้อสังเกตจากการนั่งดูเพื่อน 5 คนเล่นเกมและความเห็นที่สอบถามหลังเล่นจบ '
           'นักศึกษาจดข้อมูลทั้งสามอย่างด้วยตนเอง วิธีเก็บข้อมูลอยู่ในหัวข้อ 3.3.4 และผลอยู่ในหัวข้อ 4.2.7 ถึง 4.2.9')


def body_children(doc):
    return list(doc.element.body)


def find_heading(doc, text):
    for el in doc.element.body:
        if el.tag == W('p') and cf.ptext(el) == text:
            sid = cf.style_of(el)
            if sid and sid not in ('2a',):          # ไม่ใช่บรรทัดในสารบัญ
                return el
    raise SystemExit('ไม่พบหัวข้อ: ' + text)


def move_block(first, stop, before):
    """ย้ายองค์ประกอบตั้งแต่ first จนถึงก่อน stop ไปไว้หน้า before"""
    block = []
    el = first
    while el is not stop:
        block.append(el)
        el = el.getnext()
    for e in block:
        before.addprevious(e)
    return len(block)


def renumber_tables(doc):
    mapping = {n: n - 1 for n in range(3, 11)}
    n_changed = 0
    for t in doc.element.body.iter(W('t')):
        if not t.text:
            continue

        def sub(m):
            return m.group(1) + '3.' + str(mapping.get(int(m.group(2)), int(m.group(2))))
        new = re.sub(r'(ตารางที่ ?)3\.(\d+)(?!\d)', sub, t.text)
        if new != t.text:
            t.text = new
            n_changed += 1
    return n_changed


def run(src, dst):
    doc = Document(src)
    log = []
    h32 = find_heading(doc, '3.2 รายละเอียดขั้นตอนการทำโครงงาน')
    h321 = find_heading(doc, '3.2.1 ขอบเขตของโครงงาน')
    h322 = find_heading(doc, '3.2.2 เครื่องมือและอุปกรณ์ที่ใช้ในการทำโครงงาน')
    h323 = find_heading(doc, '3.2.3 ทรัพยากรภายนอกที่นำมาใช้')
    h33 = find_heading(doc, '3.3 ขั้นตอนการดำเนินงานที่นักศึกษาปฏิบัติงานหรือโครงงาน')

    body_tpl = h321.getnext()
    assert cf.style_of(body_tpl) is None, 'ย่อหน้าต้นแบบไม่ใช่ย่อหน้าเนื้อหา'

    # 1) ขอบเขต -> ท้าย 3.1 (ก่อนหัวข้อ 3.2)
    n1 = move_block(h321.getnext(), h322, h32)
    # 2) ทรัพยากรภายนอก -> ท้าย 3.2.2 (ก่อนหัวข้อ 3.2.3)
    n2 = move_block(h323.getnext(), h33, h323)
    log.append('ย้ายขอบเขต %d องค์ประกอบ ไปท้าย 3.1 · ย้ายทรัพยากรภายนอก %d องค์ประกอบ ไปท้าย 3.2.2' % (n1, n2))

    # 3) หัวข้อใหม่ + เนื้อหาใหม่
    for h, title, text in ((h321, '3.2.1 ประชากรและกลุ่มตัวอย่าง', NEW_321),
                           (h323, '3.2.3 การเก็บรวบรวมข้อมูล', NEW_323)):
        assert cf.set_text(h, title)
        para = copy.deepcopy(body_tpl)
        assert cf.set_text(para, text)
        h.addnext(para)
    log.append('หัวข้อ 3.2.1 / 3.2.3 เปลี่ยนชื่อแล้ว')

    # 4) เลขตาราง
    log.append('เลื่อนเลขตารางบท 3: %d จุด' % renumber_tables(doc))

    # 5) ข้อความอื่นที่อ้างขอบเขต (ตอนนี้อยู่ในหัวข้อ 3.1)
    n = 0
    for t in doc.element.body.iter(W('t')):
        if t.text and 'หัวข้อ 3.2.1' in t.text:
            t.text = t.text.replace('หัวข้อ 3.2.1', 'หัวข้อ 3.1')
            n += 1
    log.append('อ้างหัวข้อ 3.2.1 -> 3.1: %d จุด' % n)
    # 6) ชื่อตารางที่ไม่ได้ใช้สไตล์ CaptionTable (ทำให้ไม่เข้าสารบัญตาราง)
    cap_id = sorted(st.style_id for st in doc.styles if st.name == 'CaptionTable')[0]
    for p in doc.element.body.iter(W('p')):
        t = cf.ptext(p)
        nxt = p.getnext()
        if (re.match(r'^ตารางที่ \d+\.\d+ +\S', t) and cf.style_of(p) is None and len(t) < 120
                and t.count('ตารางที่') == 1 and '(ต่อ)' not in t
                and nxt is not None and nxt.tag == W('tbl')):
            pPr = p.find(W('pPr'))
            if pPr is None:
                pPr = OxmlElement('w:pPr')
                p.insert(0, pPr)
            for e in pPr.findall(W('pStyle')):
                pPr.remove(e)
            ps = OxmlElement('w:pStyle')
            ps.set(W('val'), cap_id)
            pPr.insert(0, ps)
            log.append('ใช้สไตล์ชื่อตารางกับ: ' + t[:60])
    doc.save(dst)
    return log


if __name__ == '__main__':
    for l in run(sys.argv[1], sys.argv[2]):
        print(' ', l)
