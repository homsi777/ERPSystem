import { jsPDF } from 'jspdf';

export type DocumentExportSection = {
  heading?: string;
  rows: Array<{ label: string; value: string }>;
};

export type DocumentExportPayload = {
  title: string;
  subtitle?: string;
  fileName: string;
  sections: DocumentExportSection[];
  shareText: string;
};

function buildPdfBlob(payload: DocumentExportPayload): Blob {
  const doc = new jsPDF({ unit: 'pt', format: 'a4' });
  const margin = 40;
  let y = margin;

  doc.setFont('helvetica', 'bold');
  doc.setFontSize(16);
  doc.text(payload.title, margin, y);
  y += 22;

  if (payload.subtitle) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    doc.text(payload.subtitle, margin, y);
    y += 18;
  }

  doc.setDrawColor(180);
  doc.line(margin, y, 555, y);
  y += 18;

  for (const section of payload.sections) {
    if (y > 760) {
      doc.addPage();
      y = margin;
    }
    if (section.heading) {
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(12);
      doc.text(section.heading, margin, y);
      y += 16;
    }
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    for (const row of section.rows) {
      if (y > 780) {
        doc.addPage();
        y = margin;
      }
      const line = `${row.label}: ${row.value}`;
      const wrapped = doc.splitTextToSize(line, 515);
      doc.text(wrapped, margin, y);
      y += wrapped.length * 14 + 4;
    }
    y += 10;
  }

  doc.setFontSize(9);
  doc.setTextColor(120);
  doc.text('ERP الأمل.AB — تصدير أولي (التصميم النهائي لاحقاً)', margin, 820);

  return doc.output('blob');
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName.endsWith('.pdf') ? fileName : `${fileName}.pdf`;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export async function exportDocumentPdf(payload: DocumentExportPayload): Promise<void> {
  const blob = buildPdfBlob(payload);
  triggerDownload(blob, payload.fileName);
}

export async function shareDocumentWhatsApp(payload: DocumentExportPayload): Promise<'shared' | 'opened' | 'downloaded'> {
  const blob = buildPdfBlob(payload);
  const safeName = payload.fileName.endsWith('.pdf') ? payload.fileName : `${payload.fileName}.pdf`;
  const file = new File([blob], safeName, { type: 'application/pdf' });

  // Best path on iPhone/Android: native share sheet (user can pick WhatsApp).
  if (typeof navigator.canShare === 'function' && navigator.canShare({ files: [file] })) {
    try {
      await navigator.share({
        files: [file],
        title: payload.title
      });
      return 'shared';
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return 'shared';
      }
    }
  }

  if (typeof navigator.share === 'function') {
    try {
      await navigator.share({
        title: payload.title,
        text: payload.shareText
      });
      return 'shared';
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return 'shared';
      }
    }
  }

  // Fallback: download PDF then open WhatsApp with a text message.
  triggerDownload(blob, safeName);
  const waUrl = `https://wa.me/?text=${encodeURIComponent(payload.shareText)}`;
  window.open(waUrl, '_blank', 'noopener,noreferrer');
  return 'opened';
}
