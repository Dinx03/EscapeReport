# -*- coding: utf-8 -*-
"""เปลี่ยนไฟล์ภาพที่ฝังใน docx ให้เป็นฉบับล่าสุดใน picture/ready/ (จับคู่ด้วยชื่อใน docPr) โดยไม่แตะข้อความ
วิธีใช้  py tools/swap_figure_blobs.py SRC DST"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import conform_form as cf
from docx import Document
READY = os.path.join(os.path.dirname(HERE), 'picture', 'ready')
doc = Document(sys.argv[1])
A = '{http://schemas.openxmlformats.org/drawingml/2006/main}'
n = 0
for dp in doc.element.body.iter('{http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing}docPr'):
    name = dp.get('name') or ''
    f = os.path.join(READY, name)
    if not os.path.exists(f):
        continue
    inline = dp.getparent()
    blip = next(inline.iter(A + 'blip'))
    rid = blip.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}embed')
    part = doc.part.related_parts[rid]
    part._blob = open(f, 'rb').read()
    n += 1
    print(name, part.content_type)
doc.save(sys.argv[2])
print('swapped', n)
