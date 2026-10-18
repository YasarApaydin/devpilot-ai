import { jsPDF } from 'jspdf';
import { formatDate } from '../utils/formatDate';
import type { ChatMessage } from '../types';

/**
 * Normalizes Turkish characters and sanitizes emoji/surrogate pairs
 * for standard WinAnsi jsPDF fonts to guarantee that broken glyphs
 * (such as 'Ø=Þ' or question marks) never appear in exported PDFs.
 */
function sanitizeForPdf(text: string): string {
  if (!text) return '';
  return text
    // Replace standard emojis with clean readable ASCII badges
    .replace(/⚠️/g, '[!]')
    .replace(/🤖/g, '[AI]')
    .replace(/💬/g, '[Mesaj]')
    .replace(/⚡/g, '[>]')
    .replace(/🚀/g, '[>]')
    .replace(/💡/g, '[Not]')
    .replace(/🛡️?/g, '[Guvenlik]')
    .replace(/✅/g, '[OK]')
    .replace(/❌/g, '[X]')
    .replace(/ℹ️?/g, '[i]')
    .replace(/📌/g, '[*]')
    .replace(/⭐/g, '[*]')
    .replace(/🔥/g, '[!]')
    .replace(/→|->/g, '->')
    .replace(/•/g, '*')
    // Normalize Turkish characters
    .replace(/ğ/g, 'g')
    .replace(/Ğ/g, 'G')
    .replace(/ş/g, 's')
    .replace(/Ş/g, 'S')
    .replace(/ı/g, 'i')
    .replace(/İ/g, 'I')
    .replace(/ç/g, 'c')
    .replace(/Ç/g, 'C')
    .replace(/ö/g, 'o')
    .replace(/Ö/g, 'O')
    .replace(/ü/g, 'u')
    .replace(/Ü/g, 'U')
    .replace(/â/g, 'a')
    .replace(/Â/g, 'A')
    .replace(/î/g, 'i')
    .replace(/Î/g, 'I')
    .replace(/û/g, 'u')
    .replace(/Û/g, 'U')
    // Punctuation and quotes
    .replace(/[“”]/g, '"')
    .replace(/[‘’]/g, "'")
    .replace(/[—–]/g, '-')
    // Strip surrogate pairs and emoji ranges
    .replace(/[\uD800-\uDBFF][\uDC00-\uDFFF]/g, '')
    .replace(/[\u2600-\u27BF]/g, '')
    .replace(/[\uE000-\uF8FF]/g, '')
    // Strip any remaining non-printable or non-ASCII characters
    .replace(/[^\x20-\x7E\n\r\t]/g, '');
}

/**
 * Splits continuous unbreakable words (e.g. long URLs, tokens)
 * so they wrap properly within PDF document borders without horizontal clipping.
 */
function wrapLongWords(text: string, maxLen = 45): string {
  if (!text) return '';
  return text
    .split('\n')
    .filter((line) => !/^\s*\|[\s:|-]+\|\s*$/.test(line))
    .map((line) => {
      const normalizedLine = /^\s*\|.*\|\s*$/.test(line)
        ? line
            .trim()
            .replace(/^\|/, '')
            .replace(/\|$/, '')
            .split('|')
            .map((c) => c.trim())
            .join('  |  ')
        : line;
      return normalizedLine
        .split(' ')
        .map((word) => {
          if (word.length <= maxLen) return word;
          const chunks: string[] = [];
          for (let i = 0; i < word.length; i += maxLen) {
            chunks.push(word.slice(i, i + maxLen));
          }
          return chunks.join(' ');
        })
        .join(' ');
    })
    .join('\n');
}

/**
 * Strips raw markdown artifacts while keeping clean readable text
 */
function cleanMarkdownLine(line: string): string {
  return line
    .replace(/\*\*(.*?)\*\*/g, '$1') // Bold **text**
    .replace(/\*(.*?)\*/g, '$1')     // Italic *text*
    .replace(/`([^`]+)`/g, '$1')     // Inline code `code`
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1'); // Markdown links [text](url)
}

function escapeHtml(text: string): string {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function formatMarkdownToHtml(markdown: string): string {
  if (!markdown) return '';
  const lines = markdown.split('\n');
  let html = '';
  let inCodeBlock = false;
  let codeBuffer = '';

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (line.trim().startsWith('```')) {
      if (inCodeBlock) {
        html += `<pre class="code-block"><code>${escapeHtml(codeBuffer.trim())}</code></pre>`;
        codeBuffer = '';
        inCodeBlock = false;
      } else {
        inCodeBlock = true;
        codeBuffer = '';
      }
      continue;
    }

    if (inCodeBlock) {
      codeBuffer += line + '\n';
      continue;
    }

    const trimmed = line.trim();
    if (!trimmed) {
      html += '<div style="height: 6px;"></div>';
      continue;
    }

    if (trimmed.startsWith('### ')) {
      html += `<h3>${escapeHtml(trimmed.replace(/^###\s+/, ''))}</h3>`;
    } else if (trimmed.startsWith('## ')) {
      html += `<h2>${escapeHtml(trimmed.replace(/^##\s+/, ''))}</h2>`;
    } else if (trimmed.startsWith('# ')) {
      html += `<h1>${escapeHtml(trimmed.replace(/^#\s+/, ''))}</h1>`;
    } else if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
      const itemText = trimmed.replace(/^[-*]\s+/, '');
      const parsedItem = escapeHtml(itemText)
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
        .replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');
      html += `<div class="list-item"><span class="bullet">&bull;</span><span>${parsedItem}</span></div>`;
    } else if (/^\d+\.\s+/.test(trimmed)) {
      const num = trimmed.match(/^(\d+)\.\s+/)?.[1] || '1';
      const itemText = trimmed.replace(/^\d+\.\s+/, '');
      const parsedItem = escapeHtml(itemText)
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
        .replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');
      html += `<div class="list-item"><span class="bullet">${num}.</span><span>${parsedItem}</span></div>`;
    } else {
      const parsedText = escapeHtml(trimmed)
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
        .replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');
      html += `<p>${parsedText}</p>`;
    }
  }

  if (inCodeBlock && codeBuffer) {
    html += `<pre class="code-block"><code>${escapeHtml(codeBuffer.trim())}</code></pre>`;
  }

  return html;
}

export const pdfApi = {
  /**
   * Generates and downloads a high-resolution, beautifully styled PDF document
   * using jsPDF with proper hierarchy, no text overflow, clean typography,
   * and prominent AI model & fallback/quota details.
   */
  async downloadMessagePdf(
    messageId: string,
    customTitle?: string,
    messageContent?: string,
    userPrompt?: string,
    modelUsed?: string,
    fallbackReason?: string,
    originalModel?: string
  ): Promise<void> {
    try {
      const doc = new jsPDF({
        orientation: 'portrait',
        unit: 'mm',
        format: 'a4',
      });

      const pageWidth = doc.internal.pageSize.getWidth();
      const pageHeight = doc.internal.pageSize.getHeight();
      const margin = 16;
      const contentWidth = pageWidth - margin * 2;
      let y = margin;

      // 1. Header Banner
      doc.setFillColor(248, 250, 252); // slate-50
      doc.roundedRect(margin, y, contentWidth, 23, 3, 3, 'F');
      doc.setDrawColor(226, 232, 240); // slate-200
      doc.roundedRect(margin, y, contentWidth, 23, 3, 3, 'S');

      // Top brand accent line
      doc.setFillColor(37, 99, 235); // blue-600
      doc.rect(margin + 2.5, y, contentWidth - 5, 2, 'F');

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(13);
      doc.setTextColor(15, 23, 42); // slate-900
      doc.text('DevPilot AI - Yapay Zeka Analiz ve Istemi Raporu', margin + 6, y + 10);

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(8.5);
      doc.setTextColor(100, 116, 139); // slate-500
      const dateStr = `Export: ${new Date().toLocaleDateString('tr-TR')} ${new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}  |  Rapor No: ${messageId.slice(0, 12)}`;
      doc.text(sanitizeForPdf(dateStr), margin + 6, y + 18);

      y += 29;

      // 2. User Prompt Box (if available)
      if (userPrompt && userPrompt.trim()) {
        const sanitizedPrompt = sanitizeForPdf(wrapLongWords(cleanMarkdownLine(userPrompt.trim())));
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(9);
        doc.setTextColor(37, 99, 235); // blue-600
        doc.text('KULLANICI ISTEMI (PROMPT):', margin, y);
        y += 4.5;

        doc.setFont('helvetica', 'normal');
        doc.setFontSize(9);
        doc.setTextColor(51, 65, 85); // slate-700

        const promptLines = doc.splitTextToSize(sanitizedPrompt, contentWidth - 10);
        const promptBoxHeight = promptLines.length * 4.8 + 6;

        if (y + promptBoxHeight > pageHeight - margin - 15) {
          doc.addPage();
          y = margin;
        }

        doc.setFillColor(248, 250, 252);
        doc.roundedRect(margin, y - 1, contentWidth, promptBoxHeight, 2, 2, 'F');
        doc.setDrawColor(203, 213, 225);
        doc.roundedRect(margin, y - 1, contentWidth, promptBoxHeight, 2, 2, 'S');

        // Blue accent bar on the left of prompt box
        doc.setFillColor(37, 99, 235);
        doc.rect(margin, y - 1, 2.5, promptBoxHeight, 'F');

        doc.text(promptLines, margin + 5, y + 4);
        y += promptBoxHeight + 6;
      }

      // 3. AI Model & Status / Fallback Box (Under Prompt)
      const effectiveModel = modelUsed || 'DevPilot Default (Qwen 3.8 / Groq)';
      const sanitizedModel = sanitizeForPdf(effectiveModel);

      // Check if page break is needed for metadata card
      const metaCardHeight = fallbackReason ? 20 : 11;
      if (y + metaCardHeight > pageHeight - margin - 15) {
        doc.addPage();
        y = margin;
      }

      // Model info bar
      doc.setFillColor(241, 245, 249); // slate-100
      doc.roundedRect(margin, y - 1, contentWidth, 8, 1.5, 1.5, 'F');
      doc.setDrawColor(226, 232, 240);
      doc.roundedRect(margin, y - 1, contentWidth, 8, 1.5, 1.5, 'S');

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(8);
      doc.setTextColor(71, 85, 105); // slate-600
      doc.text('Yapay Zeka Modeli:', margin + 4, y + 4.2);

      doc.setFont('helvetica', 'normal');
      doc.setTextColor(15, 23, 42); // slate-900
      doc.text(sanitizedModel, margin + 35, y + 4.2);

      y += 11;

      // If Fallback happened (Quota exceeded / error switch), render highlighted warning box
      if (fallbackReason) {
        const warningRaw = `${originalModel ? 'Secilen ' + originalModel : 'Secilen model'} yanit veremedi (${fallbackReason}) -> DevPilot Varsayilan Modeli ile yanitlandi.`;
        const sanitizedWarning = sanitizeForPdf(wrapLongWords(warningRaw));
        const warningLines = doc.splitTextToSize(`[!] UYARI / KOTA GECISI: ${sanitizedWarning}`, contentWidth - 8);
        const warningH = warningLines.length * 4.2 + 5;

        if (y + warningH > pageHeight - margin - 15) {
          doc.addPage();
          y = margin;
        }

        doc.setFillColor(254, 243, 199); // amber-100
        doc.roundedRect(margin, y - 1, contentWidth, warningH, 1.5, 1.5, 'F');
        doc.setDrawColor(245, 158, 11); // amber-500
        doc.roundedRect(margin, y - 1, contentWidth, warningH, 1.5, 1.5, 'S');

        doc.setFont('helvetica', 'bold');
        doc.setFontSize(8);
        doc.setTextColor(146, 64, 14); // amber-900
        doc.text(warningLines, margin + 4, y + 3.2);

        y += warningH + 6;
      }

      // 4. Response Header
      if (y + 15 > pageHeight - margin - 15) {
        doc.addPage();
        y = margin;
      }

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(11);
      doc.setTextColor(15, 23, 42);
      const reportTitle = sanitizeForPdf(customTitle || 'Yapay Zeka Asistan Yaniti');
      doc.text(reportTitle, margin, y);
      y += 4.5;

      // Divider line
      doc.setDrawColor(226, 232, 240);
      doc.line(margin, y - 1, pageWidth - margin, y - 1);
      y += 5;

      // 5. Response Body with Robust Pagination & Text Wrapping
      const textToRender = messageContent && messageContent.trim()
        ? messageContent.trim()
        : 'Yanit icerigi bulunamadi.';

      const rawLines = textToRender.split('\n');
      let inCode = false;
      let codeLines: string[] = [];

      const renderCollectedCode = () => {
        if (codeLines.length === 0) return;
        const codeText = sanitizeForPdf(wrapLongWords(codeLines.join('\n'), 55));
        doc.setFont('courier', 'normal');
        doc.setFontSize(8);
        doc.setTextColor(30, 41, 59);

        const splitCodeLines = doc.splitTextToSize(codeText, contentWidth - 10);

        // Render code block line-by-line across pages to strictly avoid page clipping
        for (let idx = 0; idx < splitCodeLines.length; idx++) {
          if (y + 6 > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }

          // Background band for code line
          doc.setFillColor(241, 245, 249);
          doc.rect(margin, y - 1, contentWidth, 4.2, 'F');
          // Left dark border
          doc.setFillColor(30, 41, 59);
          doc.rect(margin, y - 1, 1.5, 4.2, 'F');

          doc.text(splitCodeLines[idx], margin + 4, y + 2.4);
          y += 4.2;
        }

        y += 4;
        codeLines = [];
      };

      for (let i = 0; i < rawLines.length; i++) {
        const line = rawLines[i];

        // Code block fence toggle
        if (line.trim().startsWith('```')) {
          if (inCode) {
            renderCollectedCode();
            inCode = false;
          } else {
            inCode = true;
            codeLines = [];
          }
          continue;
        }

        if (inCode) {
          codeLines.push(line);
          continue;
        }

        const trimmed = line.trim();
        if (!trimmed) {
          y += 3.5;
          continue;
        }

        // Headings (#, ##, ###)
        if (trimmed.startsWith('# ') || trimmed.startsWith('## ') || trimmed.startsWith('### ')) {
          const cleanHeading = sanitizeForPdf(wrapLongWords(cleanMarkdownLine(trimmed.replace(/^#+\s*/, ''))));
          doc.setFont('helvetica', 'bold');
          doc.setFontSize(10.5);
          doc.setTextColor(15, 23, 42);

          const headingLines = doc.splitTextToSize(cleanHeading, contentWidth);
          const reqH = headingLines.length * 5 + 4;

          if (y + reqH > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }

          y += 2;
          doc.text(headingLines, margin, y);
          y += headingLines.length * 5 + 2;
          continue;
        }

        // Bullet point or numbered list
        if (trimmed.startsWith('- ') || trimmed.startsWith('* ') || /^\d+\.\s+/.test(trimmed)) {
          const isNum = /^\d+\.\s+/.test(trimmed);
          const prefix = isNum ? trimmed.match(/^(\d+\.)\s+/)?.[1] || '1.' : '-';
          const itemText = sanitizeForPdf(wrapLongWords(cleanMarkdownLine(trimmed.replace(/^([-*]|\d+\.)\s+/, ''))));

          doc.setFont('helvetica', 'normal');
          doc.setFontSize(9.5);
          doc.setTextColor(30, 41, 59);

          const listLines = doc.splitTextToSize(`${prefix}  ${itemText}`, contentWidth - 4);
          const reqH = listLines.length * 4.8;

          if (y + reqH > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }

          doc.text(listLines, margin + 2, y);
          y += reqH + 1.5;
          continue;
        }

        // Regular paragraph
        const cleanPara = sanitizeForPdf(wrapLongWords(cleanMarkdownLine(trimmed)));
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(9.5);
        doc.setTextColor(30, 41, 59);

        const paraLines = doc.splitTextToSize(cleanPara, contentWidth);

        // Render paragraph with line-by-line page break check
        for (let pIdx = 0; pIdx < paraLines.length; pIdx++) {
          if (y + 5 > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }
          doc.text(paraLines[pIdx], margin, y);
          y += 4.8;
        }
        y += 2;
      }

      // Handle unclosed code block at end of message
      if (inCode && codeLines.length > 0) {
        renderCollectedCode();
      }

      // 6. Footer with page numbering
      const totalPages = doc.getNumberOfPages();
      for (let p = 1; p <= totalPages; p++) {
        doc.setPage(p);
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(8);
        doc.setTextColor(148, 163, 184); // slate-400
        doc.text(
          `Sayfa ${p} / ${totalPages}  |  DevPilot AI Guvenli ve Dogrulanmis Rapor`,
          pageWidth / 2,
          pageHeight - 7,
          { align: 'center' }
        );
      }

      const filename = `${(customTitle || 'DevPilot_Rapor').replace(/[^a-zA-Z0-9_-]/g, '_')}_${messageId.slice(0, 6)}.pdf`;
      doc.save(filename);
    } catch (err) {
      console.error('Client PDF generation error:', err);
      throw err;
    }
  },

  /**
   * Opens a native, pixel-perfect print layout styled in Arial with Turkish Unicode
   * and triggers the browser's native Print / 'Save as PDF' dialog.
   * Includes complete Prompt, Model identification, and Fallback/Quota notice.
   */
  printMessageReport(
    customTitle?: string,
    messageContent?: string,
    userPrompt?: string,
    modelUsed?: string,
    fallbackReason?: string,
    originalModel?: string
  ): void {
    const printWindow = window.open('', '_blank');
    if (!printWindow) {
      alert('Lutfen tarayicinizin acilir pencere (popup) engelleyicisini kapatiniz.');
      return;
    }

    const title = customTitle || 'DevPilot AI - Istemi ve Yanit Raporu';
    const effectiveModel = modelUsed || 'DevPilot Default (Qwen 3.8 / Groq)';

    const html = `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="UTF-8">
  <title>${escapeHtml(title)}</title>
  <style>
    @page {
      size: A4 portrait;
      margin: 16mm 14mm;
    }
    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
    }
    body {
      font-family: Arial, 'Segoe UI', -apple-system, sans-serif;
      font-size: 10pt;
      line-height: 1.6;
      color: #1e293b;
      background: #ffffff;
      padding: 10px 15px;
    }
    .header {
      border-bottom: 2.5px solid #2563eb;
      padding-bottom: 12px;
      margin-bottom: 18px;
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .logo-title {
      font-size: 16pt;
      font-weight: 700;
      color: #0f172a;
      letter-spacing: -0.3px;
    }
    .badge {
      font-size: 8pt;
      font-weight: 700;
      color: #2563eb;
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      padding: 3px 8px;
      border-radius: 999px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .meta {
      font-size: 8.5pt;
      color: #64748b;
      text-align: right;
      line-height: 1.4;
    }
    .prompt-card {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-left: 4px solid #2563eb;
      border-radius: 8px;
      padding: 12px 16px;
      margin-bottom: 14px;
      page-break-inside: avoid;
    }
    .section-label {
      font-size: 8pt;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      color: #2563eb;
      margin-bottom: 6px;
    }
    .prompt-text {
      font-size: 10pt;
      color: #334155;
      white-space: pre-wrap;
      word-break: break-word;
    }
    .model-card {
      background: #f1f5f9;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 8px 14px;
      margin-bottom: 14px;
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 8.5pt;
      color: #475569;
      page-break-inside: avoid;
    }
    .model-card strong {
      color: #0f172a;
    }
    .fallback-warning {
      background: #fef3c7;
      border: 1px solid #f59e0b;
      border-left: 4px solid #d97706;
      border-radius: 6px;
      padding: 10px 14px;
      margin-bottom: 14px;
      font-size: 8.5pt;
      color: #78350f;
      page-break-inside: avoid;
    }
    .fallback-warning strong {
      color: #92400e;
    }
    .response-title {
      font-size: 12.5pt;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 10px;
      padding-bottom: 6px;
      border-bottom: 1px solid #f1f5f9;
    }
    .response-body {
      font-size: 10pt;
      color: #1e293b;
    }
    .response-body h1 {
      font-size: 13pt;
      color: #0f172a;
      margin-top: 14px;
      margin-bottom: 8px;
      font-weight: 700;
      page-break-after: avoid;
    }
    .response-body h2 {
      font-size: 11.5pt;
      color: #0f172a;
      margin-top: 12px;
      margin-bottom: 6px;
      font-weight: 700;
      page-break-after: avoid;
    }
    .response-body h3 {
      font-size: 10.5pt;
      color: #0f172a;
      margin-top: 10px;
      margin-bottom: 5px;
      font-weight: 700;
      page-break-after: avoid;
    }
    .response-body p {
      margin-bottom: 8px;
      text-align: justify;
    }
    .list-item {
      display: flex;
      gap: 8px;
      margin-bottom: 5px;
      margin-left: 10px;
    }
    .bullet {
      color: #2563eb;
      font-weight: 700;
      flex-shrink: 0;
    }
    pre.code-block {
      background: #0f172a;
      color: #f8fafc;
      padding: 12px 14px;
      border-radius: 6px;
      font-family: 'Consolas', 'Courier New', monospace;
      font-size: 8.5pt;
      overflow-x: auto;
      margin: 12px 0;
      white-space: pre-wrap;
      word-break: break-all;
      border: 1px solid #1e293b;
      page-break-inside: avoid;
    }
    code.inline-code {
      background: #f1f5f9;
      color: #0f172a;
      padding: 2px 5px;
      border-radius: 4px;
      font-family: 'Consolas', 'Courier New', monospace;
      font-size: 8.5pt;
      border: 1px solid #e2e8f0;
    }
    .footer {
      margin-top: 30px;
      padding-top: 10px;
      border-top: 1px solid #e2e8f0;
      font-size: 8pt;
      color: #94a3b8;
      display: flex;
      justify-content: space-between;
      page-break-inside: avoid;
    }
  </style>
</head>
<body>
  <div class="header">
    <div class="brand">
      <span class="logo-title">DevPilot AI</span>
      <span class="badge">Rapor & Analiz</span>
    </div>
    <div class="meta">
      <div>Tarih: ${new Date().toLocaleDateString('tr-TR')} ${new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}</div>
      <div>Platform: DevPilot AI Clean Architecture</div>
    </div>
  </div>

  ${userPrompt ? `
  <div class="prompt-card">
    <div class="section-label">Kullanici Istemi (Prompt)</div>
    <div class="prompt-text">${escapeHtml(userPrompt)}</div>
  </div>
  ` : ''}

  <div class="model-card">
    <span>Kullanılan Yapay Zeka Modeli:</span>
    <strong>${escapeHtml(effectiveModel)}</strong>
  </div>

  ${fallbackReason ? `
  <div class="fallback-warning">
    <strong>⚠️ Model Geçiş / Kota Bildirimi:</strong>
    <span>Seçilen ${escapeHtml(originalModel || 'model')} yanıt veremedi (${escapeHtml(fallbackReason)}) → DevPilot Varsayılan Modeli ile yanıtlandı.</span>
  </div>
  ` : ''}

  <div class="response-title">${escapeHtml(customTitle || 'Yapay Zeka Asistan Yaniti')}</div>
  <div class="response-body">
    ${formatMarkdownToHtml(messageContent || '')}
  </div>

  <div class="footer">
    <span>DevPilot AI Assistant Platform</span>
    <span>Güvenli ve Doğrulanmış Çıktı</span>
  </div>

  <script>
    window.onload = function() {
      setTimeout(function() {
        window.print();
      }, 250);
    };
  </script>
</body>
</html>`;

    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
  },

  /**
   * Generates and downloads a complete multi-page PDF transcript of the conversation,
   * with models and fallback details clearly listed under each response.
   */
  async downloadConversationPdf(
    conversationId: string,
    conversationTitle?: string,
    messages: ChatMessage[] = []
  ): Promise<void> {
    try {
      const doc = new jsPDF({
        orientation: 'portrait',
        unit: 'mm',
        format: 'a4',
      });

      const pageWidth = doc.internal.pageSize.getWidth();
      const pageHeight = doc.internal.pageSize.getHeight();
      const margin = 16;
      const contentWidth = pageWidth - margin * 2;
      let y = margin;

      // Header Banner
      doc.setFillColor(248, 250, 252);
      doc.roundedRect(margin, y, contentWidth, 20, 2, 2, 'F');
      doc.setDrawColor(226, 232, 240);
      doc.roundedRect(margin, y, contentWidth, 20, 2, 2, 'S');

      doc.setFillColor(37, 99, 235);
      doc.rect(margin + 2, y, contentWidth - 4, 2, 'F');

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(12);
      doc.setTextColor(15, 23, 42);
      const titleStr = sanitizeForPdf(`Sohbet Kaydi: ${conversationTitle || 'DevPilot AI Oturumu'}`);
      doc.text(titleStr, margin + 6, y + 9);

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(8.5);
      doc.setTextColor(100, 116, 139);
      doc.text(
        sanitizeForPdf(`Tarih: ${new Date().toLocaleDateString('tr-TR')}  |  Toplam Mesaj: ${messages.length}`),
        margin + 6,
        y + 15
      );

      y += 26;

      if (messages.length === 0) {
        doc.setFontSize(9.5);
        doc.setTextColor(100, 116, 139);
        doc.text('Bu sohbette kayitli mesaj bulunmamaktadir.', margin, y);
      } else {
        for (const msg of messages) {
          const isUser = msg.role === 'user';
          const senderName = isUser ? 'Kullanici' : 'DevPilot Asistan';
          const time = formatDate(msg.createdAt);

          // Sender header
          if (y + 14 > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }

          doc.setFont('helvetica', 'bold');
          doc.setFontSize(9);
          if (isUser) {
            doc.setTextColor(37, 99, 235);
          } else {
            doc.setTextColor(15, 23, 42);
          }

          doc.text(`[${senderName}] - ${time}`, margin, y);
          y += 4;

          // If assistant message, render model and fallback warnings
          if (!isUser) {
            if (msg.fallbackReason) {
              const warningRaw = `[!] UYARI / KOTA GECISI: ${msg.originalModel ? 'Secilen ' + msg.originalModel : 'Secilen model'} yanit veremedi (${msg.fallbackReason}) -> DevPilot Varsayilan Modeli ile yanitlandi.`;
              const sanitizedWarn = sanitizeForPdf(wrapLongWords(warningRaw));
              const warnLines = doc.splitTextToSize(sanitizedWarn, contentWidth - 6);
              const warnBoxH = warnLines.length * 4 + 4;

              if (y + warnBoxH > pageHeight - margin - 10) {
                doc.addPage();
                y = margin;
              }

              doc.setFillColor(254, 243, 199);
              doc.roundedRect(margin, y - 1, contentWidth, warnBoxH, 1, 1, 'F');
              doc.setDrawColor(245, 158, 11);
              doc.roundedRect(margin, y - 1, contentWidth, warnBoxH, 1, 1, 'S');

              doc.setFont('helvetica', 'bold');
              doc.setFontSize(7.5);
              doc.setTextColor(146, 64, 14);
              doc.text(warnLines, margin + 3, y + 2.8);
              y += warnBoxH + 2;
            }

            if (msg.modelUsed) {
              doc.setFont('helvetica', 'normal');
              doc.setFontSize(7.5);
              doc.setTextColor(100, 116, 139);
              doc.text(`Model: ${sanitizeForPdf(msg.modelUsed)}`, margin + 2, y + 2.5);
              y += 5;
            }
          }

          // Message body
          doc.setFont('helvetica', 'normal');
          doc.setFontSize(9);
          doc.setTextColor(51, 65, 85);

          const cleanContent = sanitizeForPdf(wrapLongWords(cleanMarkdownLine(msg.content)));
          const lines = doc.splitTextToSize(cleanContent, contentWidth - 8);
          const blockHeight = lines.length * 4.5 + 5;

          if (y + blockHeight > pageHeight - margin - 10) {
            doc.addPage();
            y = margin;
          }

          doc.setFillColor(isUser ? 240 : 248, isUser ? 249 : 250, isUser ? 255 : 252);
          doc.roundedRect(margin, y - 1, contentWidth, blockHeight, 1.5, 1.5, 'F');
          doc.setDrawColor(isUser ? 191 : 226, isUser ? 219 : 232, isUser ? 254 : 240);
          doc.roundedRect(margin, y - 1, contentWidth, blockHeight, 1.5, 1.5, 'S');

          doc.text(lines, margin + 4, y + 3.5);
          y += blockHeight + 5;
        }
      }

      // Add page numbering footer
      const totalPages = doc.getNumberOfPages();
      for (let i = 1; i <= totalPages; i++) {
        doc.setPage(i);
        doc.setFontSize(8);
        doc.setTextColor(148, 163, 184);
        doc.text(
          `Sayfa ${i} / ${totalPages}  |  DevPilot AI Workspace`,
          pageWidth / 2,
          pageHeight - 7,
          { align: 'center' }
        );
      }

      const filename = `${sanitizeForPdf((conversationTitle || 'Sohbet').replace(/[^a-zA-Z0-9_-]/g, '_'))}.pdf`;
      doc.save(filename);
    } catch (err) {
      console.error('Conversation PDF export error:', err);
      throw err;
    }
  },
};
