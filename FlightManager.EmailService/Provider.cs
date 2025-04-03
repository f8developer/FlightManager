using System.ComponentModel.DataAnnotations;
using brevo_csharp.Api;
using brevo_csharp.Model;
using System.Text.Json;
using brevo_csharp.Client;
using Microsoft.Extensions.Logging;

namespace FlightManager.EmailService;

/// <summary>
/// Service for sending emails using the Brevo (formerly Sendinblue) API.
/// </summary>
public class BrevoEmailService
{
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;

    /// <summary>
    /// Initializes a new instance of the Brevo email service.
    /// </summary>
    /// <param name="logger">The logger instance for recording service events.</param>
    /// <param name="credentialsPath">Path to the JSON file containing API credentials. Defaults to "credentials.json".</param>
    /// <exception cref="FileNotFoundException">Thrown when the credentials file is not found.</exception>
    /// <exception cref="Exception">Thrown when credentials are missing or invalid.</exception>
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

    /// <summary>
    /// Sends an email using the Brevo API.
    /// </summary>
    /// <param name="subject">The email subject line.</param>
    /// <param name="htmlContent">The HTML content of the email.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="recipientName">Optional recipient name.</param>
    /// <param name="textContent">Optional plain text content. If not provided, will be generated from HTML.</param>
    /// <returns>The message ID from Brevo if successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured.</exception>
    /// <exception cref="ArgumentException">Thrown when recipient email is invalid or content is empty.</exception>
    /// <exception cref="Exception">Thrown when email sending fails.</exception>
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

    /// <summary>
    /// Converts HTML content to plain text by stripping HTML tags.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <returns>Plain text version of the HTML content.</returns>
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

    /// <summary>
    /// Internal class for deserializing email service credentials.
    /// </summary>
    private class AppCredentials
    {
        /// <summary>
        /// Gets or sets the Brevo API key.
        /// </summary>
        public string BrevoApiKey { get; set; }

        /// <summary>
        /// Gets or sets the sender email address.
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets the sender display name.
        /// </summary>
        public string SenderName { get; set; }
    }
}