# อัปเดตสารบัญสามชุดและเลขหน้าในไฟล์ Word ที่สร้างจาก md2docx.py
# แล้วบันทึกทับ พร้อมส่งออก PDF ไว้ตรวจหน้าตา
#
# วิธีใช้   powershell -File tools\finish-docx.ps1
#
# เปิด Word แบบไม่แสดงหน้าต่าง ทำงานแล้วปิดทุกครั้งใน finally
# ถ้าไม่ปิด Word จะค้างถือไฟล์ไว้ ทำให้สร้างไฟล์ใหม่ทับไม่ได้

param(
    [string]$Docx = (Join-Path (Split-Path -Parent $PSScriptRoot) 'รายงานสหกิจศึกษา-EscapeRoom-ร่าง.docx'),
    [string]$Pdf  = ''
)

$ErrorActionPreference = 'Stop'
if (-not $Pdf) { $Pdf = [IO.Path]::ChangeExtension($Docx, '.pdf') }


# ---- ตารางที่ข้ามหน้า  คู่มือข้อ 9: พิมพ์ตารางหน้าต่อไปเหมือนหน้าแรก (หัวตารางซ้ำ) และใส่ (ต่อ) ท้ายชื่อตาราง
$CONT = ' (ต่อ)'

function Get-CapBefore($t) {
    $p = $t.Range.Paragraphs.Item(1).Previous(1)
    if ($p -eq $null) { return $null }
    return $p
}

function Merge-Continuations($doc) {
    # ถ้าเคยแยกไว้แล้ว  รวมกลับเป็นตารางเดียวก่อน  (ลบย่อหน้า "ชื่อตาราง (ต่อ)" และแถวหัวที่ซ้ำ)
    $n = 0
    for ($i = $doc.Tables.Count; $i -ge 2; $i--) {
        $t = $doc.Tables.Item($i)
        $cap = Get-CapBefore $t
        if ($cap -eq $null) { continue }
        $txt = $cap.Range.Text.TrimEnd([char]13, [char]7)
        if ($txt.EndsWith($CONT) -and $txt.StartsWith('ตารางที่')) {
            $prevTbl = $doc.Tables.Item($i - 1)
            $t.Rows.Item(1).Delete()
            $cap.Range.Delete() | Out-Null
            $n++
        }
    }
    return $n
}

function Keep-Headers($doc) {
    # แถวหัวตารางติดกับแถวถัดไปเสมอ  ไม่ให้เหลือชื่อตารางกับหัวตารางโดดเดี่ยวท้ายหน้า
    foreach ($t in $doc.Tables) {
        if ($t.Rows.Count -lt 2) { continue }
        $cap = Get-CapBefore $t
        if ($cap -eq $null) { continue }
        if (-not $cap.Range.Text.StartsWith('ตารางที่')) { continue }
        try { $t.Rows.Item(1).Range.ParagraphFormat.KeepWithNext = $true } catch {}
    }
}

function Split-OnePass($doc) {
    for ($ti = 1; $ti -le $doc.Tables.Count; $ti++) {
        $t = $doc.Tables.Item($ti)
        if ($t.Rows.Count -lt 3) { continue }
        if (-not $t.Uniform) { continue }
        $cap = Get-CapBefore $t
        if ($cap -eq $null) { continue }
        $ctext = $cap.Range.Text.TrimEnd([char]13, [char]7)
        if (-not $ctext.StartsWith('ตารางที่')) { continue }
        $p1 = $t.Rows.Item(1).Range.Information(3)
        $brk = 0
        for ($r = 2; $r -le $t.Rows.Count; $r++) {
            if ($t.Rows.Item($r).Range.Information(3) -gt $p1) { $brk = $r; break }
        }
        if ($brk -lt 3) { continue }
        $capRange = $cap.Range
        $t.Split($brk)
        $nt = $doc.Tables.Item($ti + 1)
        $mid = $nt.Range.Paragraphs.Item(1).Previous(1)
        # ชื่อตารางซ้ำ + (ต่อ)  จัดรูปแบบเหมือนชื่อตารางเดิม แต่ไม่ใช้สไตล์ชื่อตาราง เพื่อไม่ให้ซ้ำในสารบัญตาราง
        $src = $capRange.Duplicate
        $src.MoveEnd(1, -1) | Out-Null
        $dst = $mid.Range
        $dst.MoveEnd(1, -1) | Out-Null
        $dst.FormattedText = $src.FormattedText
        $mid = $nt.Range.Paragraphs.Item(1).Previous(1)
        foreach ($b in @($mid.Range.Bookmarks)) { try { $b.Delete() } catch {} }
        $mid.Style = $doc.Styles.Item(-1)
        $mid.Alignment = $cap.Alignment
        $mid.LeftIndent = $cap.LeftIndent
        $mid.FirstLineIndent = $cap.FirstLineIndent
        $mid.SpaceBefore = $cap.SpaceBefore
        $mid.SpaceAfter = $cap.SpaceAfter
        $mid.KeepWithNext = $true
        $e = $mid.Range
        $e.MoveEnd(1, -1) | Out-Null
        $e.Collapse(0)
        $e.InsertAfter($CONT)
        $e.Font.Bold = $false
        # หัวตารางซ้ำ
        $hdr = $t.Rows.Item(1)
        $new = $nt.Rows.Add($nt.Rows.Item(1))
        for ($c = 1; $c -le $hdr.Cells.Count; $c++) {
            $ct = $hdr.Cells.Item($c).Range.Text.TrimEnd([char]13, [char]7)
            $new.Cells.Item($c).Range.Text = $ct
            $new.Cells.Item($c).Shading.BackgroundPatternColor = $hdr.Cells.Item($c).Shading.BackgroundPatternColor
            $new.Cells.Item($c).Range.Font.Bold = $hdr.Cells.Item($c).Range.Font.Bold
            $new.Cells.Item($c).Range.ParagraphFormat.Alignment = $hdr.Cells.Item($c).Range.ParagraphFormat.Alignment
        }
        $new.HeadingFormat = $true
        $new.Range.ParagraphFormat.KeepWithNext = $true
        return $true
    }
    return $false
}

$word = $null
$doc = $null
try {
    $word = [Activator]::CreateInstance([Type]::GetTypeFromProgID('Word.Application'))
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $doc = $word.Documents.Open($Docx)

    $doc.Fields.Update() | Out-Null
    # รูปเล่มจริงและแบบฟอร์มของคณะไม่ใช้จุดนำในสารบัญ เลขหน้าชิดขวาเฉย ๆ
    # wdTabLeaderSpaces = 0   (ถ้าต้องการจุดนำกลับมา ใช้ 1 = wdTabLeaderDots)
    foreach ($toc in $doc.TablesOfContents) { $toc.TabLeader = 0; $toc.Update() }
    $doc.Repaginate()

    $m = Merge-Continuations $doc
    Keep-Headers $doc
    $doc.Repaginate()
    $splits = 0
    while ($splits -lt 60) {
        if (-not (Split-OnePass $doc)) { break }
        $splits++
        $doc.Repaginate()
    }
    'tables merged back : ' + $m + '   split with (ต่อ) : ' + $splits

    # คู่มือ: เลขหน้านับต่อเนื่องตลอดเล่ม (เอกสารอ้างอิงและภาคผนวกนับแต่ไม่แสดง) ไม่ต้องเริ่มเลขหน้าประวัติใหม่
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    $doc.Repaginate()

    'pages : ' + $doc.ComputeStatistics(2)
    'TOCs  : ' + $doc.TablesOfContents.Count

    $doc.Save()
    $doc.ExportAsFixedFormat($Pdf, 17)
    'saved + pdf ok'
}
finally {
    if ($doc -ne $null) { try { $doc.Close(0) } catch {} }
    if ($word -ne $null) { try { $word.Quit(0) } catch {} }
    [GC]::Collect()
}
