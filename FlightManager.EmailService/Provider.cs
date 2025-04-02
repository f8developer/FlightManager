using brevo_csharp.Api;
using brevo_csharp.Model;
using System.Text.Json;

namespace FlightManager.EmailService;

public class BrevoEmailService
{
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;

    // Credentials class for JSON deserialization
    private class AppCredentials
    {
        public string BrevoApiKey { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
    }

    // Constructor loads credentials from JSON file
    public BrevoEmailService(string credentialsPath = "credentials.json")
    {
        try
        {
            var credentials = JsonSerializer.Deserialize<AppCredentials>(
                File.ReadAllText(credentialsPath));

            if (credentials == null)
            {
                throw new Exception("Failed to load credentials from JSON");
            }

            _apiKey = credentials.BrevoApiKey;
            _senderEmail = credentials.SenderEmail;
            _senderName = credentials.SenderName;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading credentials: {ex.Message}");
        }
    }

    // Main email sending function
    public string SendEmail(
        string subject,
        string htmlContent,
        string recipientEmail,
        string recipientName = null,
        string textContent = null)
    {
        try
        {
            // Configure API
            brevo_csharp.Client.Configuration.Default.ApiKey.Clear();
            brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", _apiKey);

            // Create email
            var email = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_senderName, _senderEmail),
                to: new System.Collections.Generic.List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(recipientEmail, recipientName)
                },
                subject: subject,
                htmlContent: htmlContent,
                textContent: textContent ?? ConvertHtmlToPlainText(htmlContent)
            );

            // Send email
            var apiInstance = new TransactionalEmailsApi();
            var result = apiInstance.SendTransacEmail(email);
            return result.MessageId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending email: {ex.Message}");
        }
    }

    // Helper method to convert HTML to plain text (simplistic version)
    private string ConvertHtmlToPlainText(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
    }
}