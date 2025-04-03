using FlightManager.Extensions.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Hosting;

namespace FlightManager.Test.ServiceTests
{
    /// <summary>
    /// Unit tests for the <see cref="EmailTemplateService"/> class.
    /// </summary>
    [TestFixture]
    public class EmailTemplateServiceTests : IDisposable
    {
        private Mock<IWebHostEnvironment> _envMock;
        private Mock<ILogger<EmailTemplateService>> _loggerMock;
        private string _tempTemplatesPath;
        private EmailTemplateService _service;

        /// <summary>
        /// Sets up the test environment before each test method is executed.
        /// Creates a temporary directory for template files and initializes mocks.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Create a temp directory for testing
            _tempTemplatesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempTemplatesPath);

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(x => x.ContentRootPath).Returns(Path.GetTempPath());

            _loggerMock = new Mock<ILogger<EmailTemplateService>>();

            _service = new EmailTemplateService(_envMock.Object, _loggerMock.Object);
        }

        /// <summary>
        /// Cleans up the test environment by deleting the temporary directory
        /// and its contents after each test method is executed.
        /// </summary>
        public void Dispose()
        {
            // Clean up temp directory
            if (Directory.Exists(_tempTemplatesPath))
            {
                Directory.Delete(_tempTemplatesPath, true);
            }
        }

        /// <summary>
        /// Tests that the constructor creates the Templates directory when it doesn't exist.
        /// </summary>
        [Test]
        public void Constructor_ShouldCreateTemplatesDirectory_WhenNotExists()
        {
            // Arrange
            var customPath = Path.Combine(Path.GetTempPath(), "NonExistentTemplateDir");
            if (Directory.Exists(customPath))
                Directory.Delete(customPath);

            _envMock.Setup(x => x.ContentRootPath).Returns(Path.GetTempPath());

            // Act
            var service = new EmailTemplateService(_envMock.Object, _loggerMock.Object);

            // Assert
            Assert.That(Directory.Exists(Path.Combine(Path.GetTempPath(), "Templates")), Is.True);

            // Cleanup
            Directory.Delete(Path.Combine(Path.GetTempPath(), "Templates"));
        }

        /// <summary>
        /// Tests that GetTemplate returns the processed template with all placeholders replaced
        /// when provided with valid template and replacement values.
        /// </summary>
        [Test]
        public void GetTemplate_ShouldReturnProcessedTemplate_WhenValid()
        {
            // Arrange
            var templateName = "test_template.html";
            var templateContent = "Hello {Name}, welcome to {Service}!";

            // Create the Templates subdirectory
            var templatesDir = Path.Combine(_tempTemplatesPath, "Templates");
            Directory.CreateDirectory(templatesDir);

            // Create the template file in the correct location
            var templatePath = Path.Combine(templatesDir, templateName);
            File.WriteAllText(templatePath, templateContent);

            // Configure the environment mock to use our test directory
            _envMock.Setup(x => x.ContentRootPath).Returns(_tempTemplatesPath);

            // Create a new service instance with the configured environment
            var service = new EmailTemplateService(_envMock.Object, _loggerMock.Object);

            var replacements = new Dictionary<string, string>
                {
                    {"Name", "John Doe"},
                    {"Service", "FlightManager"}
                };

            // Act
            var result = service.GetTemplate(templateName, replacements);

            // Assert
            Assert.That(result, Is.EqualTo("Hello John Doe, welcome to FlightManager!"));
        }

        /// <summary>
        /// Tests that GetTemplate throws a FileNotFoundException when the requested template doesn't exist.
        /// Verifies that the error is logged.
        /// </summary>
        [Test]
        public void GetTemplate_ShouldThrowFileNotFoundException_WhenTemplateMissing()
        {
            // Arrange
            var templateName = "non_existent_template.html";

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() =>
                _service.GetTemplate(templateName, new Dictionary<string, string>()));

            Assert.That(ex.Message, Does.Contain(templateName));
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(templateName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that GetTemplate handles templates with no replacement placeholders correctly.
        /// </summary>
        [Test]
        public void GetTemplate_ShouldHandleEmptyReplacements()
        {
            // Arrange
            var templateName = "empty_replacements.html";
            var templateContent = "Static content without placeholders";

            // Create the Templates directory if it doesn't exist
            var templatesDir = Path.Combine(_tempTemplatesPath, "Templates");
            Directory.CreateDirectory(templatesDir);

            // Create the template file in the Templates subdirectory
            var templatePath = Path.Combine(templatesDir, templateName);
            File.WriteAllText(templatePath, templateContent);

            // Configure the environment mock to return our test path
            _envMock.Setup(x => x.ContentRootPath).Returns(_tempTemplatesPath);

            // Create a new service instance with the configured environment
            var service = new EmailTemplateService(_envMock.Object, _loggerMock.Object);

            // Act
            var result = service.GetTemplate(templateName, new Dictionary<string, string>());

            // Assert
            Assert.That(result, Is.EqualTo(templateContent));
        }

        /// <summary>
        /// Tests that GetTemplate preserves unmatched placeholders in the template content.
        /// </summary>
        [Test]
        public void GetTemplate_ShouldPreserveUnmatchedPlaceholders()
        {
            // Arrange
            var templateName = "unmatched_placeholders.html";
            var templateContent = "Hello {Name}, your code is {Code}";

            // Create the Templates directory if it doesn't exist
            var templatesDir = Path.Combine(_tempTemplatesPath, "Templates");
            Directory.CreateDirectory(templatesDir);

            // Create the template file in the correct location
            var templatePath = Path.Combine(templatesDir, templateName);
            File.WriteAllText(templatePath, templateContent);

            // Configure the environment mock to use our test directory
            _envMock.Setup(x => x.ContentRootPath).Returns(_tempTemplatesPath);

            // Create service instance with the configured environment
            var service = new EmailTemplateService(_envMock.Object, _loggerMock.Object);

            var replacements = new Dictionary<string, string>
                {
                    {"Name", "Alice"}
                };

            // Act
            var result = service.GetTemplate(templateName, replacements);

            // Assert
            Assert.That(result, Is.EqualTo("Hello Alice, your code is {Code}"));
        }
    }
}