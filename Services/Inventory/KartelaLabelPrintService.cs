using ERPSystem.ViewModels.Inventory;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ERPSystem.Services.Inventory;

public interface IKartelaLabelPrintService
{
    KartelaPrintResult Print(
        IReadOnlyList<KartelaLabelRowSnapshot> rows,
        int copies,
        string documentName);
}

public enum KartelaPrintStatus
{
    Success,
    Cancelled,
    Failure
}

public sealed record KartelaPrintResult(KartelaPrintStatus Status, string Message);

public sealed class KartelaLabelPrintService(IKartelaLabelRenderer renderer)
    : IKartelaLabelPrintService
{
    private const double SizeTolerance = 2;

    public KartelaPrintResult Print(
        IReadOnlyList<KartelaLabelRowSnapshot> rows,
        int copies,
        string documentName)
    {
        try
        {
            var dialog = new PrintDialog
            {
                UserPageRangeEnabled = false
            };

            if (dialog.ShowDialog() != true)
                return new(KartelaPrintStatus.Cancelled, "تم إلغاء الطباعة.");

            var queue = dialog.PrintQueue;
            if (queue is null)
                return Failure("لم يتم اختيار طابعة صالحة.");

            var requestedTicket = queue.DefaultPrintTicket.Clone();
            requestedTicket.PageMediaSize = new PageMediaSize(
                KartelaLabelRenderer.LabelWidth,
                KartelaLabelRenderer.LabelHeight);
            requestedTicket.PageOrientation = PageOrientation.Landscape;
            requestedTicket.CopyCount = 1;

            var validation = queue.MergeAndValidatePrintTicket(
                queue.DefaultPrintTicket,
                requestedTicket);
            var validatedTicket = validation.ValidatedPrintTicket;
            var validatedSize = validatedTicket.PageMediaSize;
            if (validatedSize?.Width is not double width
                || validatedSize.Height is not double height
                || Math.Abs(width - KartelaLabelRenderer.LabelWidth) > SizeTolerance
                || Math.Abs(height - KartelaLabelRenderer.LabelHeight) > SizeTolerance)
            {
                return Failure(
                    "تعريف الطابعة المحددة لا يدعم ورق 100 × 80 مم. " +
                    "أضف هذا المقاس من خصائص طابعة الملصقات ثم أعد المحاولة.");
            }

            var capabilities = queue.GetPrintCapabilities(validatedTicket);
            var imageable = capabilities.PageImageableArea;
            if (imageable is null
                || imageable.ExtentWidth + SizeTolerance < KartelaLabelRenderer.LabelWidth
                || imageable.ExtentHeight + SizeTolerance < KartelaLabelRenderer.LabelHeight)
            {
                return Failure(
                    "المساحة القابلة للطباعة في تعريف الطابعة أصغر من 100 × 80 مم. " +
                    "اختر إعداد الطباعة بدون هوامش أو عرّف مقاس الملصق الصحيح.");
            }

            var fixedDocument = new FixedDocument();
            fixedDocument.DocumentPaginator.PageSize = new Size(
                KartelaLabelRenderer.LabelWidth,
                KartelaLabelRenderer.LabelHeight);

            for (var copy = 0; copy < copies; copy++)
            {
                var page = new FixedPage
                {
                    Width = KartelaLabelRenderer.LabelWidth,
                    Height = KartelaLabelRenderer.LabelHeight
                };
                page.Children.Add(renderer.CreateLabelVisual(rows));

                var content = new PageContent();
                ((IAddChild)content).AddChild(page);
                fixedDocument.Pages.Add(content);
            }

            dialog.PrintTicket = validatedTicket;
            dialog.PrintDocument(fixedDocument.DocumentPaginator, documentName);
            return new(
                KartelaPrintStatus.Success,
                $"تم إرسال {copies} نسخة إلى الطابعة «{queue.FullName}» بنجاح.");
        }
        catch (PrintSystemException)
        {
            return Failure("تعذر الاتصال بالطابعة. تحقق من تشغيلها وتعريفها ثم أعد المحاولة.");
        }
        catch
        {
            return Failure("تعذرت طباعة الملصق. تحقق من إعدادات الطابعة ثم أعد المحاولة.");
        }
    }

    private static KartelaPrintResult Failure(string message) =>
        new(KartelaPrintStatus.Failure, message);
}
