using ERPSystem.Application.Results;
using ERPSystem.Dialogs;

namespace ERPSystem.Services;

public static class ApplicationResultPresenter
{
    public static bool Present(ApplicationResult result, string? successTitle = null)
    {
        if (result.IsSuccess)
            return true;

        var title = result.Status switch
        {
            ApplicationResultStatus.ValidationFailed => "تحقق من البيانات",
            ApplicationResultStatus.NotFound => "غير موجود",
            ApplicationResultStatus.PermissionDenied => "صلاحية غير كافية",
            ApplicationResultStatus.Conflict => "تعارض",
            _ => "خطأ"
        };

        var message = result.Status == ApplicationResultStatus.ValidationFailed && result.ValidationErrors.Count > 0
            ? string.Join("\n", result.ValidationErrors.Select(e => e.Message))
            : result.ErrorMessage ?? "حدث خطأ غير متوقع.";

        var kind = result.Status switch
        {
            ApplicationResultStatus.ValidationFailed => MockFeedbackKind.Warning,
            ApplicationResultStatus.PermissionDenied => MockFeedbackKind.Warning,
            _ => MockFeedbackKind.Warning
        };

        MockFeedbackDialog.Show(kind, message, title);
        return false;
    }

    public static bool Present<T>(ApplicationResult<T> result) => Present((ApplicationResult)result);
}
