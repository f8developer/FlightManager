using System.Text;

namespace FlightManager.Extensions.Services;

/// <summary>
/// Service for loading and processing email templates with dynamic content replacement.
/// </summary>
/// 
public class EmailTemplateService
{
    private readonly string _templatesPath;
    private readonly ILogger<EmailTemplateService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailTemplateService"/> class.
    /// </summary>
    /// <param name="env">The web hosting environment to locate template files.</param>
    /// <param name="logger">The logger for recording service events.</param>
    public EmailTemplateService(IWebHostEnvironment env, ILogger<EmailTemplateService> logger)
    {
        _templatesPath = Path.Combine(env.ContentRootPath, "Templates");
        _logger = logger;

        try
        {
            if (!Directory.Exists(_templatesPath))
            {
                Directory.CreateDirectory(_templatesPath);
                _logger.LogInformation("Created Templates directory at {Path}", _templatesPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Templates directory");
            throw;
        }
    }

    /// <summary>
    /// Retrieves and processes an email template with the specified replacements.
    /// </summary>
    /// <param name="templateName">The name of the template file to load.</param>
    /// <param name="replacements">Dictionary of placeholder-value pairs for template substitution.</param>
    /// <returns>The processed template content with all replacements applied.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified template file doesn't exist.</exception>
    /// <remarks>
    /// Template files should be located in the application's "Templates" directory.
    /// Placeholders in templates should be in the format {Key} where Key matches a key in the replacements dictionary.
    /// </remarks>
    public string GetTemplate(string templateName, Dictionary<string, string> replacements)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be empty", nameof(templateName));

        var templatePath = Path.Combine(_templatesPath, templateName);

        if (!File.Exists(templatePath))
        {
            var exception = new FileNotFoundException($"Email template not found: {templateName}", templatePath);
            _logger.LogError(exception, "Template file not found: {TemplatePath}", templatePath);
            throw exception;
        }

        try
        {
            var templateContent = File.ReadAllText(templatePath);

            if (replacements != null && replacements.Count > 0)
            {
                var result = new StringBuilder(templateContent);
                foreach (var replacement in replacements)
                {
                    result.Replace($"{{{replacement.Key}}}", replacement.Value);
                }
                return result.ToString();
            }

            return templateContent;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Error processing email template {TemplateName}", templateName);
            throw;
        }
    }
}