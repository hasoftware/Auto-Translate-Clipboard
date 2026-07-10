using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoTranslate.Services;

public sealed class TranslationResult
{
    public string Text { get; init; } = "";
    public string DetectedLanguage { get; init; } = "";
}

/// <summary>Gọi Google Translate (endpoint công khai translate_a/single, trả JSON sạch).</summary>
public static class TranslationService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        return c;
    }

    /// <summary>Gửi 1 request nhỏ lúc khởi động để mở sẵn DNS/TLS, giảm độ trễ lần dịch đầu.</summary>
    public static async Task WarmUpAsync()
    {
        try { await TranslateAsync("en", "en", "a").ConfigureAwait(false); } catch { }
    }

    public static async Task<TranslationResult> TranslateAsync(
        string sourceCode, string targetCode, string text)
    {
        var url = "https://translate.googleapis.com/translate_a/single"
                + "?client=gtx"
                + $"&sl={Uri.EscapeDataString(sourceCode)}"
                + $"&tl={Uri.EscapeDataString(targetCode)}"
                + "&dt=t"
                + $"&q={Uri.EscapeDataString(text)}";

        var json = await Http.GetStringAsync(url).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var sb = new StringBuilder();
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            foreach (var seg in root[0].EnumerateArray())
            {
                if (seg.GetArrayLength() > 0 && seg[0].ValueKind == JsonValueKind.String)
                    sb.Append(seg[0].GetString());
            }
        }

        string detected = "";
        if (root.GetArrayLength() > 2 && root[2].ValueKind == JsonValueKind.String)
            detected = root[2].GetString() ?? "";

        return new TranslationResult { Text = sb.ToString(), DetectedLanguage = detected };
    }
}
