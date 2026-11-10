# -*- coding: utf-8 -*-
"""ร่างใน md/ -> ฉบับสะอาดใน md/final/  ตัดส่วนที่ไม่ใช่เนื้อรายงาน แล้วรายงานสิ่งที่ตัดออก
ตัด: กล่องสถานะหัวไฟล์ · ทุกบล็อก > (หมายเหตุผู้เขียน) · เอกสารอ้างอิงท้ายบท · ภาคผนวกของร่าง · ตัวคั่น ---
ไม่แตะ: ตาราง รูป(**[ต้องแคปใหม่]**) หัวข้อ ย่อหน้า
วิธีใช้  py tools/clean_draft.py 2   (หรือ 2 3 4 5)"""
import os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MD = os.path.join(ROOT, 'md')
FILES = {'2': 'บทที่-2-ทฤษฎีที่เกี่ยวข้อง.md', '3': 'บทที่-3-ขั้นตอนการดำเนินงาน.md',
         '4': 'บทที่-4-ผลการดำเนินงาน.md', '5': 'บทที่-5-บทสรุปและข้อเสนอแนะ.md'}
for ch in [a for a in sys.argv[1:] if a in FILES]:
    src = os.path.join(MD, FILES[ch])
    lines = open(src, encoding='utf-8').read().split('\n')
    out, removed, cur = [], [], []
    for ln in lines:
        if ln.startswith('# ภาคผนวกของร่าง') or ln.startswith('## เอกสารอ้างอิง'):
            break
        if ln.strip().startswith('>'):
            cur.append(ln.strip()); continue
        if cur:
            removed.append('\n'.join(cur)); cur = []
        if ln.strip() == '---':
            continue
        out.append(ln)
    if cur:
        removed.append('\n'.join(cur))
    text = re.sub(r'\n{3,}', '\n\n', '\n'.join(out)).strip() + '\n'
    dst = os.path.join(MD, 'final', FILES[ch])
    if os.path.exists(dst) and '--force' not in sys.argv:
        dst = os.path.join(MD, 'final', '_ร่างสะอาด-' + FILES[ch])
    open(dst, 'w', encoding='utf-8').write(text)
    log = os.path.join(MD, 'final', '_ตัดออก-บท%s.txt' % ch)
    open(log, 'w', encoding='utf-8').write('\n\n=====\n\n'.join(removed))
    print('บท', ch, '->', os.path.basename(dst), len(text), 'อักขระ  ตัดกล่อง', len(removed))
