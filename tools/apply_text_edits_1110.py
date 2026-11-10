# -*- coding: utf-8 -*-
"""แก้ข้อความตามที่ผู้เขียนสั่งเมื่อ 10 พ.ย. 2569 (ช่องแดง แถวตาราง 4.18 ที่มา แหล่งโมเดล สะกด Summary ภาษาอังกฤษ)
ทำบนไฟล์ Word โดยตรง ใช้ครั้งเดียว  สำรองก่อนเสมอ  วิธีใช้  py tools/apply_text_edits_1110.py SRC --inplace"""
import copy
import datetime
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf                       # noqa: E402
from docx import Document                       # noqa: E402
from docx.oxml import OxmlElement               # noqa: E402

W = cf.W
XML_SPACE = '{http://www.w3.org/XML/1998/namespace}space'


def rtext(r):
    return ''.join(t.text or '' for t in r.findall(W('t')))


def set_run(r, text, plain=True):
    for t in r.findall(W('t')):
        r.remove(t)
    t = OxmlElement('w:t')
    t.text = text
    t.set(XML_SPACE, 'preserve')
    r.append(t)
    if plain:
        rPr = r.find(W('rPr'))
        if rPr is not None:
            for tag in ('b', 'bCs', 'color'):
                for e in rPr.findall(W(tag)):
                    rPr.remove(e)


def replace_red(doc, marker, new_text):
    n = 0
    for r in doc.element.body.iter(W('r')):
        if marker in rtext(r):
            set_run(r, new_text)
            n += 1
    return n


def drop_red(doc, marker):
    for r in list(doc.element.body.iter(W('r'))):
        if marker in rtext(r):
            prev = r.getprevious()
            if prev is not None and prev.tag == W('r'):
                for t in prev.findall(W('t')):
                    if t.text:
                        t.text = t.text.rstrip()
            r.getparent().remove(r)
            return 1
    return 0


def find_par(doc, pred):
    for p in doc.element.body.iter(W('p')):
        if pred(cf.ptext(p)):
            return p
    return None


def source_par(caption_par, text):
    p = OxmlElement('w:p')
    pPr = OxmlElement('w:pPr')
    ind = OxmlElement('w:ind')
    ind.set(W('left'), '0')
    ind.set(W('firstLine'), '0')
    pPr.append(ind)
    jc = OxmlElement('w:jc')
    jc.set(W('val'), 'left')
    pPr.append(jc)
    p.append(pPr)
    p.append(cf.make_run(text, None))
    caption_par.addnext(p)


def run(src, dst):
    doc = Document(src)
    log = []

    # ---- ช่องแดง
    log.append('3.3.4  %d' % replace_red(
        doc, 'ระบุวันที่ วิธีที่ให้เล่น',
        'ในช่วงเดือนกันยายนถึงตุลาคม พ.ศ. 2569 หลังพัฒนาเกมครบทั้งห้าองก์ โดยให้เพื่อนแต่ละคนเล่นคนเดียวบนเครื่องของนักศึกษา '
        'ไม่บอกคำตอบหรือคำใบ้ใด ๆ ล่วงหน้า นักศึกษานั่งสังเกตการเล่น จดว่าผู้เล่นติดที่องก์ใดและทำอะไรก่อนติด และสอบถามความเห็นหลังเล่นจบ'))
    log.append('4.2.8  %d' % replace_red(
        doc, 'ระบุว่าเกิดกับผู้ทดลองเล่นหรือไม่',
        'จากการทดลองเล่น มีผู้เล่น 2 ใน 5 คนที่พลาดเก็บปริซึม จึงเป็นหลักฐานว่าจุดเสี่ยงนี้เกิดขึ้นจริง'))
    log.append('4.2.9  %d' % replace_red(
        doc, 'ตรวจ Console ขณะกด Play อีกครั้ง',
        'การตรวจซ้ำเมื่อวันที่ 10 พฤศจิกายน 2569 พบว่าซอร์สโค้ดของ DigitalReadout และ ClockMovement ยังเป็นรูปแบบเดิม จึงยังมีความเสี่ยงที่ข้อผิดพลาดนี้จะเกิดขึ้นได้ '
        'ส่วน Console ขณะตรวจไม่พบข้อความ NullReferenceException ซึ่งสอดคล้องกับที่ลำดับการเรียก Awake บนเครื่องหนึ่งอาจคงที่และเปลี่ยนเมื่อย้ายเครื่อง '
        'ข้อบกพร่องนี้จึงยังไม่ถือว่าแก้ไขแล้ว และเสนอวิธีแก้ไว้ในหัวข้อ 5.2.3'))
    log.append('5.1   %d' % drop_red(doc, 'ตรวจย่อหน้านี้และแก้ให้ตรงกับสิ่งที่ได้เรียนรู้จริง'))

    # ---- 3.1  ระยะเวลา 1 สัปดาห์ = ช่วงออกแบบธีมและปริศนาเพื่อนำเสนอแนวทาง
    n = 0
    for t in doc.element.body.iter(W('t')):
        if t.text and 'และกำหนดระยะเวลาไว้ 1 สัปดาห์' in t.text:
            t.text = t.text.replace('และกำหนดระยะเวลาไว้ 1 สัปดาห์',
                                    'และกำหนดระยะเวลา 1 สัปดาห์สำหรับออกแบบธีมและปริศนาเพื่อนำเสนอแนวทางของเกม')
            n += 1
    log.append('3.1 ระยะเวลา 1 สัปดาห์  %d' % n)

    # ---- ตารางที่ 4.18  แถวระยะเวลา
    for t in doc.element.body.iter(W('tbl')):
        trs = t.findall(W('tr'))
        last = trs[-1]
        tcs = last.findall(W('tc'))
        if tcs and cf.ptext(tcs[0]).startswith('ระยะเวลาหนึ่งสัปดาห์'):
            vals = ['ระยะเวลาหนึ่งสัปดาห์สำหรับออกแบบธีมและปริศนาเพื่อนำเสนอแนวทางของเกม', 'ทำได้',
                    'ใช้เวลา 1 สัปดาห์ออกแบบและนำเสนอแนวทางของเกม ส่วนการพัฒนาตัวเกมทั้งหมดใช้เวลา 4 เดือนของการฝึกสหกิจศึกษา (หัวข้อ 3.1)']
            for tc, v in zip(tcs, vals):
                cf.set_text(tc.find(W('p')), v)
            log.append('ตาราง 4.18 แถวระยะเวลา แก้แล้ว')
            break

    # ---- 3.2.3  แหล่งที่มาของโมเดลตัวละคร
    nxt = find_par(doc, lambda s: s.startswith('ทรัพยากรอื่นทั้งหมดในเกม'))
    if nxt is not None:
        new = copy.deepcopy(nxt)
        cf.set_text(new, 'โมเดลตัวละครมาจากชุดทรัพยากรของ Zenva Academy (academy.zenva.com) ซึ่งนักศึกษาได้รับจากการซื้อคอร์สเรียน Unity '
                         'และนำมาใช้ต่อในโครงงานนี้ ')
        red = cf.make_run('[ ระบุแหล่งที่มาของไฟล์เสียงทั้งสี่ไฟล์ แล้วลบข้อความนี้ ]', None, bold=True)
        c = OxmlElement('w:color')
        c.set(W('val'), 'C00000')
        rp = red.find(W('rPr'))
        rp.insert(2, c)
        new.append(red)
        nxt.addprevious(new)
        log.append('3.2.3 แหล่งที่มาโมเดล เพิ่มแล้ว')

    # ---- ที่มา :
    for prefix, text in (('ภาพที่ 3.1', 'ที่มา : เอกสารโจทย์ Project 3 : Escape Room Simulator จากพนักงานที่ปรึกษา บริษัท Lumas corp co., LTD.'),
                         ('ภาพที่ 1.1', 'ที่มา : Google Maps')):
        # ต้องเป็นคำบรรยายจริง (สไตล์ CaptionFigure) ไม่ใช่บรรทัดในสารบัญภาพ ซึ่ง Word จะเขียนทับตอนอัปเดตฟิลด์
        fig_ids = {st.style_id for st in doc.styles if st.name == 'CaptionFigure'}
        cap = None
        for p_ in doc.element.body.iter(W('p')):
            if cf.style_of(p_) in fig_ids and cf.ptext(p_).startswith(prefix):
                cap = p_
                break
        if cap is not None:
            source_par(cap, text)
            log.append('ที่มา ' + prefix)

    # ---- สะกด / ชื่อ / Summary
    for old, new in (('คอมพิวเตอฺและปัญญหาประดิษฐ์', 'วิศวกรรมคอมพิวเตอร์และปัญญาประดิษฐ์'),
                     ('คอมพิวเตอฺและปัญญาประดิษฐ์', 'คอมพิวเตอร์และปัญญาประดิษฐ์'),
                     ('บริษัท  Lumas', 'บริษัท Lumas'),
                     ('dr. adna sento', 'Dr. Adna Sento'),
                     ('Development of a First-Person Escape Room Simulator Using a Modular Software Architecture in Unity',
                      'Development of an Escape Room Game Using Modular Software Design')):
        log.append('%s → %d' % (new[:30], cf.replace_in_w_t(doc, old, new)))
    # สาขา "CE" ในหน้าอนุมัติ  (ป้ายกับค่าคั่นด้วยแท็บ จึงดูทีละ run)
    for p in doc.element.body.iter(W('p')):
        ts = [t for t in p.iter(W('t'))]
        if len(ts) == 2 and ts[0].text == 'สาขาวิชา' and ts[1].text == 'CE':
            ts[1].text = 'วิศวกรรมคอมพิวเตอร์และปัญญาประดิษฐ์'
            log.append('สาขา CE แก้แล้ว')

    summ = [
        'This project aims to gain practical experience in the 3D game development process by developing an Escape Room game '
        'whose control scheme and gameplay conditions follow the assigned requirements. The student independently designed and '
        'developed the puzzles, covering concept and content design, game system design, programming, and testing.',
        'The student adopted Modular Software Design from the beginning. Each object is built by combining components that each '
        'have a specific responsibility, rather than creating a separate class for each type of object, so existing components can '
        'be reused and new objects added without modifying existing code. Clocks were chosen as the central element of all five '
        'acts because they share a clock face and a mechanism yet behave differently: keeping time, running slowly, stopping, and '
        'chiming. They therefore demonstrate the flexibility of modular design clearly.',
        'The result is a fully playable game of five rooms in a single route, each with its own puzzle, and 46 clocks built from '
        'shared clock face and mechanism components, varied by additional components such as mainsprings, pendulums, and chiming '
        'mechanisms. New clocks and puzzle objects can be created by selecting and combining existing components, without '
        'additional programming.']
    starts = ['This project aims to gain practical', 'In developing this project, the student adopted', 'The development resulted in a fully playable']
    for st, txt in zip(starts, summ):
        p = find_par(doc, lambda s, st=st: s.startswith(st))
        if p is not None:
            cf.set_text(p, txt)
    log.append('Summary ภาษาอังกฤษ ตัดเหลือ %d คำ' % sum(len(x.split()) for x in summ))

    doc.save(dst)
    return log


if __name__ == '__main__':
    src = sys.argv[1]
    bdir = os.path.join(os.path.dirname(os.path.abspath(src)), 'สำรอง')
    os.makedirs(bdir, exist_ok=True)
    bak = os.path.join(bdir, 'รายงานสหกิจศึกษา-EscapeRoom-v2-ก่อนแก้ช่องแดง-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S') + '.docx')
    shutil.copy2(src, bak)
    print('สำรองไว้ที่:', bak)
    tmp = src + '.tmp'
    for l in run(src, tmp):
        print(' ', l)
    os.replace(tmp, src)
    print('เสร็จ')
