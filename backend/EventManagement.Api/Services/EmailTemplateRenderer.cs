using System.Text.Encodings.Web;

namespace EventManagement.Api.Services;

public sealed class EmailTemplateRenderer(IWebHostEnvironment environment)
{
    public async Task<string> RenderAsync(
        string templateName,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(templateName);
        if (!string.Equals(safeName, templateName, StringComparison.Ordinal))
            throw new InvalidOperationException("The email template name is invalid.");

        var path = Path.Combine(environment.ContentRootPath, "EmailTemplates", safeName);
        var html = await File.ReadAllTextAsync(path, cancellationToken);
        foreach (var (key, value) in values)
        {
            html = html.Replace(
                $"{{{{{key}}}}}",
                HtmlEncoder.Default.Encode(value ?? string.Empty),
                StringComparison.Ordinal);
        }
        return html;
    }
}
