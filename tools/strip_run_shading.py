# -*- coding: utf-8 -*-
"""ลบพื้นเทาอ่อน (EDEDED) ที่คลุมชื่อคลาส/โค้ดกลางประโยค  ไม่แตะบล็อกโค้ด (พื้นย่อหน้า F4F4F4) และพื้นเซลล์ตาราง
วิธีใช้  py tools/strip_run_shading.py SRC DST"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf
from docx import Document
W = cf.W
d = Document(sys.argv[1])
n = 0
for rp in list(d.element.body.iter(W('rPr'))):
    for s in rp.findall(W('shd')):
        if s.get(W('fill')) == 'EDEDED':
            rp.remove(s)
            n += 1
d.save(sys.argv[2])
print('ลบพื้นเทาระดับตัวอักษร', n, 'จุด')
