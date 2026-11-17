using System.Net;
using System.Text.RegularExpressions;

namespace Sporty.Tests.Infrastructure;

//Helpers for posting MVC forms that are protected with an antiforgery token
public static partial class FormClient {
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client,
        string pageUrl,
        string postUrl,
        Dictionary<string, string> fields) {
        var html = await client.GetStringAsync(pageUrl);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"No antiforgery token on {pageUrl}.");

        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string name, string email, string password) =>
        client.PostFormAsync("/Identity/Account/Register", "/Identity/Account/Register", new() {
            ["Input.Name"] = name,
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password
        });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostFormAsync("/Identity/Account/Login", "/Identity/Account/Login", new() {
            ["Input.Email"] = email,
            ["Input.Password"] = password
        });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
