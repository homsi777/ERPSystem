namespace ERPSystem.Views.Auth;

internal static class LoginSecuritySteps
{
    internal sealed record Step(string Id, string Text, string Icon);

    internal static readonly Step[] All =
    [
        new("welcome", "مرحباً بك في الأمل.AB — جاري تهيئة جلسة آمنة", "👋"),
        new("secure", "يتم تأمين اتصال", "🔒"),
        new("encrypt", "يتم تشفير البيانات", "🔐"),
        new("odocore", "يتم أتصال OdoCore", "◉"),
        new("dlp", "يتم أتصال Google Workspace DLP", "🛡"),
        new("ready", "تم تجهيز أتصالات", "✓"),
        new("enter", "تفضل", "→")
    ];

    internal const int StepDelayMs = 720;
    internal const int FinalStepExtraMs = 420;
}
