using Microsoft.AspNetCore.Mvc.RazorPages;

namespace barberShop.Pages;

public class OneSignalSdkModel : PageModel
{
    private static string? _cachedScript;
    private static readonly HttpClient _http = new();

    public string ScriptContent { get; set; } = "";

    public async Task OnGetAsync()
    {
        if (_cachedScript != null)
        {
            ScriptContent = _cachedScript;
            return;
        }
        try
        {
            var js = await _http.GetStringAsync("https://cdn.onesignal.com/sdks/web/v16/OneSignalSDK.page.js");
            _cachedScript = js;
            ScriptContent = js;
        }
        catch (Exception)
        {
            ScriptContent = "console.error('OneSignal SDK betöltés sikertelen');";
        }
    }
}
