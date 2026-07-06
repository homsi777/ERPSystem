using System.Net.Http.Headers;
using System.Net.Http.Json;
using ERPSystem.Web.Client.Models.Auth;

namespace ERPSystem.Web.Client.Services;

public sealed class ApiClient(HttpClient http, AuthTokenStore tokenStore)
{
    public async Task<ApiCallResult<T>> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, relativeUrl);
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return ApiCallResult<T>.Fail(MapHttpError(response.StatusCode));

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            if (value is null)
                return ApiCallResult<T>.Fail("تعذّر قراءة الاستجابة من الخادم.");

            return ApiCallResult<T>.Ok(value);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Fail("تعذّر الاتصال بالخادم. تحقق من تشغيل النظام وحاول مرة أخرى.");
        }
        catch (TaskCanceledException)
        {
            return ApiCallResult<T>.Fail("انتهت مهلة الطلب. حاول مرة أخرى.");
        }
    }

    public async Task<ApiCallResult<LoginResponse>> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                "api/v1/auth/login",
                new LoginRequest { Username = username, Password = password },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return ApiCallResult<LoginResponse>.Fail(MapHttpError(response.StatusCode));

            var value = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
            if (value is null || string.IsNullOrWhiteSpace(value.AccessToken))
                return ApiCallResult<LoginResponse>.Fail("تعذّر الحصول على رمز الدخول.");

            tokenStore.SetAccessToken(value.AccessToken);
            return ApiCallResult<LoginResponse>.Ok(value);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<LoginResponse>.Fail("تعذّر الاتصال بالخادم. تحقق من تشغيل النظام وحاول مرة أخرى.");
        }
        catch (TaskCanceledException)
        {
            return ApiCallResult<LoginResponse>.Fail("انتهت مهلة الطلب. حاول مرة أخرى.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, relativeUrl);

        if (!string.IsNullOrWhiteSpace(tokenStore.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStore.AccessToken);

        return request;
    }

    private static string MapHttpError(System.Net.HttpStatusCode statusCode) => statusCode switch
    {
        System.Net.HttpStatusCode.Unauthorized => "انتهت الجلسة أو رمز الدخول غير صالح.",
        System.Net.HttpStatusCode.Forbidden => "ليس لديك صلاحية لتنفيذ هذا الطلب.",
        System.Net.HttpStatusCode.NotFound => "المورد المطلوب غير موجود.",
        _ => "حدث خطأ أثناء جلب البيانات. حاول مرة أخرى."
    };
}

public sealed class ApiCallResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiCallResult<T> Ok(T value) => new() { IsSuccess = true, Value = value };

    public static ApiCallResult<T> Fail(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
