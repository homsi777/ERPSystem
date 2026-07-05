using ERPSystem.Controls.China;
using ERPSystem.Dialogs;

namespace ERPSystem.Services.China;

public static class ContainerDocumentationPopupService
{
    public static void Show(Guid containerId, string containerNumber)
    {
        var control = new ContainerDocumentationControl(containerId, containerNumber);
        ErpModalWindow.Show(
            "توثيق الحاوية",
            $"ملفات التخليص الجمركي — {containerNumber}",
            control,
            "\uE8B7",
            760,
            560);
    }

    public static void Show(ContainerListRow row) =>
        Show(row.Id, row.ContainerNumber);
}
