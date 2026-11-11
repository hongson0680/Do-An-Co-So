"""Convert README-DACN.md to README-DACN.docx"""
import re
from docx import Document
from docx.shared import Pt, RGBColor, Inches
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

INPUT = r"c:\Users\re-pl\OneDrive\Documents\DEMO_DACS4 (2)\DEMO_DACS4\README-DACN.md"
OUTPUT = r"c:\Users\re-pl\OneDrive\Documents\DEMO_DACS4 (2)\DEMO_DACS4\README-DACN-v2.docx"

def add_horizontal_line(doc):
    p = doc.add_paragraph()
    pPr = p._p.get_or_add_pPr()
    pBdr = OxmlElement('w:pBdr')
    pPr.insert_element_before(pBdr,
        'w:shd', 'w:tabs', 'w:suppressAutoHyphens', 'w:kinsoku', 'w:wordWrap',
        'w:overflowPunct', 'w:topLinePunct', 'w:autoSpaceDE', 'w:autoSpaceDN',
        'w:bidi', 'w:adjustRightInd', 'w:snapToGrid', 'w:spacing', 'w:ind',
        'w:contextualSpacing', 'w:mirrorIndents', 'w:suppressOverlap', 'w:jc',
        'w:textDirection', 'w:textAlignment', 'w:textboxTightWrap',
        'w:outlineLvl', 'w:divId', 'w:cnfStyle', 'w:rPr', 'w:sectPr',
        'w:pPrChange'
    )
    bottom = OxmlElement('w:bottom')
    bottom.set(qn('w:val'), 'single')
    bottom.set(qn('w:sz'), '6')
    bottom.set(qn('w:space'), '1')
    bottom.set(qn('w:color'), 'CCCCCC')
    pBdr.append(bottom)

def parse_inline(paragraph, text):
    """Parse markdown inline: **bold**, `code`"""
    # Split on ** and ` patterns
    parts = re.split(r'(\*\*[^*]+\*\*|`[^`]+`)', text)
    for part in parts:
        if not part:
            continue
        if part.startswith('**') and part.endswith('**'):
            run = paragraph.add_run(part[2:-2])
            run.bold = True
        elif part.startswith('`') and part.endswith('`'):
            run = paragraph.add_run(part[1:-1])
            run.font.name = 'Consolas'
            run.font.size = Pt(10)
        else:
            run = paragraph.add_run(part)

def add_qa(doc, question, answer):
    """Add a Q&A block with formatted styling"""
    # Question
    p_q = doc.add_paragraph()
    p_q.paragraph_format.space_before = Pt(6)
    p_q.paragraph_format.space_after = Pt(3)
    p_q.paragraph_format.left_indent = Inches(0)
    q_run = p_q.add_run(question)
    q_run.bold = True
    q_run.font.color.rgb = RGBColor(0x1F, 0x4E, 0x79)
    q_run.font.size = Pt(11)

    # Answer
    p_a = doc.add_paragraph()
    p_a.paragraph_format.left_indent = Inches(0.25)
    p_a.paragraph_format.space_after = Pt(6)
    parse_inline(p_a, answer)

def add_heading_section(doc, text):
    """Add a section heading like PHẦN 1: ..."""
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(18)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run(text)
    run.bold = True
    run.font.size = Pt(14)
    run.font.color.rgb = RGBColor(0xC0, 0x50, 0x4D)

def add_main_title(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(12)
    run = p.add_run(text)
    run.bold = True
    run.font.size = Pt(18)
    run.font.color.rgb = RGBColor(0x1F, 0x4E, 0x79)

def add_subtitle(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(16)
    run = p.add_run(text)
    run.italic = True
    run.font.size = Pt(10)
    run.font.color.rgb = RGBColor(0x66, 0x66, 0x66)

def add_code_block(doc, text):
    """Add a code block with monospace font and gray background"""
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.25)
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(4)
    run = p.add_run(text)
    run.font.name = 'Consolas'
    run.font.size = Pt(10)
    pPr = p._p.get_or_add_pPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), 'F5F5F5')
    pPr.append(shd)

def add_note(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.25)
    p.paragraph_format.space_before = Pt(8)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run(text)
    run.italic = True
    run.font.size = Pt(10)
    run.font.color.rgb = RGBColor(0x66, 0x66, 0x66)

def main():
    with open(INPUT, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')
    doc = Document()

    # Set default font
    style = doc.styles['Normal']
    style.font.name = 'Calibri'
    style.font.size = Pt(11)

    i = 0
    in_code_block = False
    code_buffer = []

    while i < len(lines):
        line = lines[i].rstrip()

        # Code block
        if line.strip().startswith('```'):
            if in_code_block:
                add_code_block(doc, '\n'.join(code_buffer))
                code_buffer = []
                in_code_block = False
            else:
                in_code_block = True
            i += 1
            continue

        if in_code_block:
            code_buffer.append(line)
            i += 1
            continue

        # Main title (# ...)
        if line.startswith('# '):
            add_main_title(doc, line[2:].strip())
            i += 1
            continue

        # Section heading (## ...)
        if line.startswith('## '):
            text = line[3:].strip()
            # Check if it's a PHẦN heading
            if text.startswith('📌') or text.startswith('🚀') or 'PHẦN' in text or 'HƯỚNG' in text.upper():
                add_heading_section(doc, text)
            else:
                # Sub heading
                p = doc.add_paragraph()
                p.paragraph_format.space_before = Pt(12)
                p.paragraph_format.space_after = Pt(6)
                run = p.add_run(text)
                run.bold = True
                run.font.size = Pt(12)
                run.font.color.rgb = RGBColor(0x2E, 0x74, 0xB5)
            i += 1
            continue

        # Separator (---)
        if line.strip() == '---':
            add_horizontal_line(doc)
            i += 1
            continue

        # Blockquote (> ...)
        if line.startswith('> '):
            add_note(doc, line[2:].strip())
            i += 1
            continue

        # Q&A line
        if line.startswith('### Q') and ':' in line:
            # Format: "### Q1: question text"
            # Find the colon after the Q number
            after_hash = line[4:]  # "Q1: ..."
            colon_idx = after_hash.find(':')
            q_label = after_hash[:colon_idx + 1]  # "Q1:"
            a_part = after_hash[colon_idx + 1:].strip()
            add_qa(doc, q_label, a_part)
            i += 1
            continue

        # Empty line
        if not line.strip():
            i += 1
            continue

        # Regular paragraph
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(6)
        parse_inline(p, line)
        i += 1

    doc.save(OUTPUT)
    print(f'OK: Đã tạo file {OUTPUT}')

if __name__ == '__main__':
    main()
