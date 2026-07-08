import { useState } from 'react';
import {
  exportDocumentPdf,
  shareDocumentWhatsApp,
  type DocumentExportPayload
} from '../lib/documentExport.ts';

type DocumentActionsProps = {
  payload: DocumentExportPayload | null;
  onToast?: (message: string, tone?: 'success' | 'error') => void;
};

export function DocumentActions({ payload, onToast }: DocumentActionsProps) {
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
      await exportDocumentPdf(payload);
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
      const result = await shareDocumentWhatsApp(payload);
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
