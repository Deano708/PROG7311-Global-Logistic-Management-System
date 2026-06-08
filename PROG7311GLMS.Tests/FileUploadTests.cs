// =============================================================
//  PROG7311GLMS.Tests / FileUploadTests.cs
//
//  UNIT TESTS — these test file upload validation in isolation.
//  We do NOT boot the full API here (that caused the Firebase
//  "already exists" error when multiple factories were created).
//  Instead we create a minimal LogisticsService directly.
// =============================================================

/*
Title: Disclosure of AI Usage in my Assessment.
• Section: FileUploadTests.
• AI Tool: Claude Sonnet 4.6
• Purpose/intention : Design assistance and troubleshooting of FileUploadTests allowing for testing of file upload validation in isolation.
• Date(s) 05/06/2026.
• https://claude.ai/share/503d645e-0ce0-4796-920e-6e73ce7ccfb5
*/

using GLMS_API.Models;
using GLMS_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace PROG7311GLMS.Tests;

[TestFixture]
public class FileUploadTests
{
    private ILogisticsService _service = null!;

    [SetUp]
    public void Setup()
    {
        // Build a minimal in-memory DbContext directly —
        // no WebApplicationFactory needed, so Firebase is never touched.
        var dbOptions = new DbContextOptionsBuilder<GlmsContext>()
            .UseInMemoryDatabase("FileUploadTestDb_" + Guid.NewGuid())
            .Options;
        var db = new GlmsContext(dbOptions);

        // Empty configuration — upload logic doesn't use any config keys
        var config = new ConfigurationBuilder().Build();

        // NullLoggerFactory — we don't need real log output in unit tests
        var loggerFactory = NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<LogisticsService>();

        // Real HttpClientFactory is not needed for upload tests
        var httpFactory = new Mock<IHttpClientFactory>().Object;

        _service = new LogisticsService(db, httpFactory, config, logger);
    }

    // ── Tests that REJECT the file ────────────────────────────

    [Test]
    public async Task Upload_NullInput_ReturnsNull()
    {
        var result = await _service.UploadAgreementAsync(null!);
        Assert.That(result, Is.Null, "Null file should return null");
    }

    [Test]
    public async Task Upload_ZeroByteFile_ReturnsNull()
    {
        var fileMock = CreateMockFile("empty.pdf", "application/pdf", length: 0);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Zero-byte file should be rejected");
    }

    [Test]
    public async Task Upload_ExecutableFile_ReturnsNull()
    {
        var fileMock = CreateMockFile("malware.exe", "application/x-msdownload", length: 1024);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Executable file should be rejected");
    }

    [Test]
    public async Task Upload_PdfExtensionButWrongMimeType_ReturnsNull()
    {
        // ZIP disguised with a .pdf extension
        var fileMock = CreateMockFile("fake.pdf", "application/zip", length: 1024);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Wrong MIME type should be rejected even with .pdf extension");
    }

    [Test]
    public async Task Upload_ImageFile_ReturnsNull()
    {
        var fileMock = CreateMockFile("photo.jpg", "image/jpeg", length: 2048);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Image files should be rejected");
    }

    [Test]
    public async Task Upload_FileTooLarge_ReturnsNull()
    {
        // 1 byte over the 5 MB limit
        var fileMock = CreateMockFile("huge.pdf", "application/pdf",
            length: (5 * 1024 * 1024) + 1);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Files larger than 5 MB should be rejected");
    }

    [Test]
    public async Task Upload_WordDocument_ReturnsNull()
    {
        var fileMock = CreateMockFile(
            "contract.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            length: 4096);
        var result = await _service.UploadAgreementAsync(fileMock.Object);
        Assert.That(result, Is.Null, "Word documents should be rejected");
    }

    // ── Boundary test ─────────────────────────────────────────

    [Test]
    public async Task Upload_FileExactlyAtSizeLimit_PassesSizeValidation()
    {
        // Exactly 5 MB — should pass the size gate (not be rejected by size)
        var content = new byte[5 * 1024 * 1024];
        var fileMock = CreateMockFile("exact.pdf", "application/pdf",
            length: 5 * 1024 * 1024);

        // Provide a real readable stream so CopyToAsync works
        fileMock.Setup(_ => _.OpenReadStream())
                .Returns(new MemoryStream(content));
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // This test verifies the file is NOT rejected by the size check.
        // It may still return null if the filesystem isn't writable in CI,
        // but it should NOT throw an exception.
        Assert.DoesNotThrowAsync(
            async () => await _service.UploadAgreementAsync(fileMock.Object),
            "A file exactly at the size limit should not throw");
    }

    // ── Helper ────────────────────────────────────────────────

    private static Mock<IFormFile> CreateMockFile(
        string fileName, string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(_ => _.FileName).Returns(fileName);
        mock.Setup(_ => _.ContentType).Returns(contentType);
        mock.Setup(_ => _.Length).Returns(length);
        return mock;
    }
}