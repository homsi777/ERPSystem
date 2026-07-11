import { useState } from 'react';
import {
  downloadPdfBlob,
  exportDocumentPdf,
  sharePdfBlobWhatsApp,
  shareDocumentWhatsApp,
  type DocumentExportPayload
} from '../lib/documentExport.ts';

type DocumentActionsProps = {
  payload: DocumentExportPayload | null;
  pdfSource?: { fileName: string; load: () => Promise<Blob> };
  onToast?: (message: string, tone?: 'success' | 'error') => void;
};

export function DocumentActions({ payload, pdfSource, onToast }: DocumentActionsProps) {
  const [busy, setBusy] = useState<'pdf' | 'wa' | null>(null);

  if (!payload) {
    return null;
  }

  async function handlePdf() {
    if (!payload || busy) {
      return;
    }
    setBusy('pdf');
    try {
      if (pdfSource) {
        downloadPdfBlob(await pdfSource.load(), pdfSource.fileName);
      } else {
        await exportDocumentPdf(payload);
      }
      onToast?.('تم تجهيز ملف PDF للتنزيل.', 'success');
    } catch {
      onToast?.('تعذّر تصدير PDF.', 'error');
    } finally {
      setBusy(null);
    }
  }

  async function handleWhatsApp() {
    if (!payload || busy) {
      return;
    }
    setBusy('wa');
    try {
      const result = pdfSource
        ? await sharePdfBlobWhatsApp(
            await pdfSource.load(),
            pdfSource.fileName,
            payload.title,
            payload.shareText
          )
        : await shareDocumentWhatsApp(payload);
      if (result === 'shared') {
        onToast?.('تم فتح المشاركة — اختر واتساب إن ظهر.', 'success');
      } else if (result === 'opened') {
        onToast?.('تم تنزيل PDF وفتح واتساب. أرفق الملف يدوياً إن لزم.', 'success');
      } else {
        onToast?.('تم تنزيل PDF للمشاركة.', 'success');
      }
    } catch {
      onToast?.('تعذّرت المشاركة عبر واتساب.', 'error');
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="compact-action-row document-actions" aria-label="تصدير ومشاركة">
      <button className="chip-button" type="button" onClick={() => void handlePdf()} disabled={busy !== null}>
        {busy === 'pdf' ? 'جار التصدير...' : 'تصدير PDF'}
      </button>
      <button className="chip-button" type="button" onClick={() => void handleWhatsApp()} disabled={busy !== null}>
        {busy === 'wa' ? 'جار المشاركة...' : 'مشاركة واتساب'}
      </button>
    </section>
  );
}
