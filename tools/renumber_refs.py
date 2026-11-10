# -*- coding: utf-8 -*-
"""เรียงเลขอ้างอิง IEEE ใหม่ตามลำดับที่ปรากฏครั้งแรกในเล่ม (บทที่ 2 -> 5)
อ่านรายการเต็มจาก md/archive/refs-all.md (ไฟล์ต้นทาง ไม่แก้)  เขียนผลลง
  md/final/บทที่-N-*.md  (เลข [n] ในเนื้อความ)   และ   md/final/เอกสารอ้างอิง.md
ไม่แตะวงเล็บเหลี่ยมในโค้ด (บล็อก ``` และ `...`)   รายการที่ไม่ถูกอ้างจะไม่อยู่ในรายการสุดท้าย
ระวัง  เรียกซ้ำจะอ่านสำเนาก่อนเรียง (md/archive/final-pre-renumber/) กลับมาเรียงใหม่ แก้ไขเนื้อความที่ทำหลังเรียกครั้งแรกจะหาย
วิธีใช้  py tools/renumber_refs.py"""
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL = os.path.join(ROOT, 'md', 'final')
ARCH = os.path.join(ROOT, 'md', 'archive')
FILES = ['บทที่-2-ทฤษฎีที่เกี่ยวข้อง.md', 'บทที่-3-ขั้นตอนการดำเนินงาน.md',
         'บทที่-4-ผลการดำเนินงาน.md', 'บทที่-5-บทสรุปและข้อเสนอแนะ.md']
BAK = os.path.join(ARCH, 'final-pre-renumber')
os.makedirs(BAK, exist_ok=True)
for f in FILES:                                    # ใช้สำเนาก่อนเรียงเป็นต้นทางเสมอ  เรียกซ้ำได้
    b = os.path.join(BAK, f)
    if not os.path.exists(b):
        shutil.copy2(os.path.join(FINAL, f), b)

refs = {}
for ln in open(os.path.join(ARCH, 'refs-all.md'), encoding='utf-8').read().split('\n'):
    m = re.match(r'^\[(\d+)\]\s*(.*)$', ln.strip())
    if m:
        refs[int(m.group(1))] = m.group(2)

RE_CITE = re.compile(r'\[(\d+(?:\]\s*,\s*\[\d+)*)\]')


def segments(text):
    """แบ่งเป็น (เป็นโค้ดหรือไม่, ข้อความ)"""
    out = []
    parts = re.split(r'(```.*?```)', text, flags=re.S)
    for p in parts:
        if p.startswith('```'):
            out.append((True, p))
            continue
        for q in re.split(r'(`[^`\n]*`)', p):
            out.append((q.startswith('`') and q.endswith('`') and len(q) > 1, q))
    return out


order = []
texts = {}
for f in FILES:
    t = open(os.path.join(BAK, f), encoding='utf-8').read()
    texts[f] = t
    for is_code, seg in segments(t):
        if is_code:
            continue
        for m in RE_CITE.finditer(seg):
            for n in re.findall(r'\d+', m.group(0)):
                n = int(n)
                if n in refs and n not in order:
                    order.append(n)
mapping = {old: i + 1 for i, old in enumerate(order)}
print('ลำดับการอ้างครั้งแรก (เลขเดิม):', order)
unused = sorted(set(refs) - set(order))
print('ไม่ถูกอ้าง ตัดออก:', unused)


def fix(seg):
    def sub(m):
        nums = re.findall(r'\d+', m.group(0))
        if not all(int(n) in mapping for n in nums):
            return m.group(0)
        return ', '.join('[%d]' % mapping[int(n)] for n in nums)
    return RE_CITE.sub(sub, seg)


for f in FILES:
    out = ''.join(seg if is_code else fix(seg) for is_code, seg in segments(texts[f]))
    open(os.path.join(FINAL, f), 'w', encoding='utf-8').write(out)

lines = ['[%d] %s' % (mapping[o], refs[o]) for o in order]
open(os.path.join(FINAL, 'เอกสารอ้างอิง.md'), 'w', encoding='utf-8').write('\n\n'.join(lines) + '\n')
print('เขียนแล้ว', len(lines), 'รายการ')
