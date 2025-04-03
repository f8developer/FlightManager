using System.Text;

namespace FlightManager.Extensions.Services;

public class EmailTemplateService
{
    private readonly string _templatesPath;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(IWebHostEnvironment env, ILogger<EmailTemplateService> logger)
    {
        _templatesPath = Path.Combine(env.ContentRootPath, "Templates");
        _logger = logger;

        if (!Directory.Exists(_templatesPath))
        {
            Directory.CreateDirectory(_templatesPath);
            _logger.LogInformation("Created Templates directory at {Path}", _templatesPath);
        }
    }

    public string GetTemplate(string templateName, Dictionary<string, string> replacements)
    {
        try
        {
            var templatePath = Path.Combine(_templatesPath, templateName);
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                throw new FileNotFoundException($"Email template not found: {templateName}");
            }

            var templateContent = File.ReadAllText(templatePath);

            // Perform replacements
            var result = new StringBuilder(templateContent);
            foreach (var replacement in replacements)
            {
                result.Replace($"{{{replacement.Key}}}", replacement.Value);
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email template {TemplateName}", templateName);
            throw;
        }
    }
}