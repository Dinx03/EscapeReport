# -*- coding: utf-8 -*-
"""กรอบ "[ วางภาพ ... ]" เดิมเป็นตาราง 1 ช่องเส้นประ  ภาพที่ใส่แล้วจึงมีเส้นประล้อมรอบ
สคริปต์นี้เอาตารางห่อออก เหลือย่อหน้าภาพเปล่า ๆ  วิธีใช้  py tools/unwrap_figure_boxes.py SRC DST"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf
from docx import Document
W = cf.W
doc = Document(sys.argv[1])
n = 0
for tbl in list(doc.element.body.iter(W('tbl'))):
    rows = tbl.findall(W('tr'))
    if len(rows) != 1:
        continue
    cells = rows[0].findall(W('tc'))
    if len(cells) != 1:
        continue
    ps = cells[0].findall(W('p'))
    if len(ps) != 1 or ps[0].find('.//' + W('drawing')) is None:
        continue
    if tbl.getparent() is not doc.element.body:
        continue
    p = ps[0]
    tbl.addprevious(p)
    tbl.getparent().remove(tbl)
    n += 1
print('unwrapped', n)
doc.save(sys.argv[2])
