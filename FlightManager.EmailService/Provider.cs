using System.ComponentModel.DataAnnotations;
using brevo_csharp.Api;
using brevo_csharp.Model;
using System.Text.Json;
using brevo_csharp.Client;
using Microsoft.Extensions.Logging;

namespace FlightManager.EmailService;

public class BrevoEmailService
{
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public BrevoEmailService(ILogger<BrevoEmailService> logger, string credentialsPath = "credentials.json")
    {
        _logger = logger;
        
        try
        {
            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    $"Email credentials file not found at: {Path.GetFullPath(credentialsPath)}");
            }

            var credentials = JsonSerializer.Deserialize<AppCredentials>(
                File.ReadAllText(credentialsPath));

            if (credentials == null || 
                string.IsNullOrEmpty(credentials.BrevoApiKey) ||
                string.IsNullOrEmpty(credentials.SenderEmail))
            {
                throw new Exception("Invalid credentials format in JSON file");
            }

            _apiKey = credentials.BrevoApiKey;
            _senderEmail = credentials.SenderEmail;
            _senderName = credentials.SenderName ?? "FlightManager";

            _logger.LogInformation("Brevo email service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize Brevo email service");
            throw;
        }
    }

        public string SendEmail(
        string subject,
        string htmlContent,
        string recipientEmail,
        string recipientName = null,
        string textContent = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("API key not configured");
        
        if (!new EmailAddressAttribute().IsValid(recipientEmail))
            throw new ArgumentException($"Invalid recipient email: {recipientEmail}");

        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("Email content cannot be empty");

        try
        {
            // Sanitize inputs
            var cleanSubject = subject.Replace("\"", "\\\"");
            var cleanRecipient = recipientEmail.Trim();

            // Configure API
            var config = new Configuration()
            {
                ApiKey = new Dictionary<string, string> { { "api-key", _apiKey } }
            };

            // Create email
            var email = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_senderName, _senderEmail),
                to: new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(cleanRecipient, recipientName?.Trim())
                },
                subject: cleanSubject,
                htmlContent: htmlContent,
                textContent: textContent ?? ConvertHtmlToPlainText(htmlContent)
            );

            // Send email
            var apiInstance = new TransactionalEmailsApi(config);
            var result = apiInstance.SendTransacEmail(email);
            
            return result.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sending failed to {Recipient}", recipientEmail);
            throw new Exception($"Failed to send email: {ex.Message}", ex);
        }
    }
        
    private string ConvertHtmlToPlainText(string html)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        }
        catch
        {
            return "Please view this email in HTML format";
        }
    }

    private class AppCredentials
    {
        public string BrevoApiKey { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
    }
}