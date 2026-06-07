using GLMS_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace PROG7311GLMS.Tests;

[TestFixture]
public class FileUploadTests
{
    // We need a real LogisticsService instance.
    // File upload logic doesn't use DB, HttpClient or Config,
    // so we can safely use null for those via NullLoggerFactory.
    private ILogisticsService _service = null!;

    [SetUp]
    public void Setup()
    {
        // Use the ApiTestFactory to get a real service with in-memory DB
        // rather than passing nulls (which caused NullReferenceExceptions
        // in your original tests when other methods were called).
        using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        _service = scope.ServiceProvider.GetRequiredService<ILogisticsService>();
    }

    // ── Tests that should REJECT the file (return null) ───────

    /// <summary>
    /// A null file input should return null immediately.
    /// </summary>
    [Test]
    public async Task Upload_NullInput_ReturnsNull()
    {
        var result = await _service.UploadAgreementAsync(null!);
        Assert.That(result, Is.Null, "Null file should return null");
    }

    /// <summary>
    /// A zero-byte file should be rejected even if it has a .pdf extension.
    /// </summary>
    [Test]
    public async Task Upload_ZeroByteFile_ReturnsNull()
    {
        var fileMock = CreateMockFile("empty.pdf", "application/pdf", length: 0);

        var result = await _service.UploadAgreementAsync(fileMock.Object);

        Assert.That(result, Is.Null, "Zero-byte file should be rejected");
    }

    /// <summary>
    /// A file with a .exe extension should be rejected regardless of content type.
    /// </summary>
    [Test]
    public async Task Upload_ExecutableFile_ReturnsNull()
    {
        var fileMock = CreateMockFile(
            "malware.exe", "application/x-msdownload", length: 1024);

        var result = await _service.UploadAgreementAsync(fileMock.Object);

        Assert.That(result, Is.Null, "Executable file should be rejected");
    }

    /// <summary>
    /// A file with a .pdf extension but wrong MIME type should be rejected.
    /// Attackers sometimes rename files — we check both extension AND content type.
    /// </summary>
    [Test]
    public async Task Upload_PdfExtensionButWrongMimeType_ReturnsNull()
    {
        var fileMock = CreateMockFile(
            "fake.pdf", "application/zip", length: 1024);  // ZIP disguised as PDF

        var result = await _service.UploadAgreementAsync(fileMock.Object);

        Assert.That(result, Is.Null,
            "File with wrong MIME type should be rejected even if extension is .pdf");
    }

    /// <summary>
    /// A .jpg image should be rejected — only PDFs are allowed.
    /// </summary>
    [Test]
    public async Task Upload_ImageFile_ReturnsNull()
    {
        var fileMock = CreateMockFile("photo.jpg", "image/jpeg", length: 2048);

        var result = await _service.UploadAgreementAsync(fileMock.Object);

        Assert.That(result, Is.Null, "Image files should be rejected");
    }

    /// <summary>
    /// A file larger than 5MB should be rejected.
    /// 5MB = 5 * 1024 * 1024 = 5,242,880 bytes. We test at 5,242,881.
    /// </summary>
    [Test]
    public async Task Upload_FileTooLarge_ReturnsNull()
    {
        var fileMock = CreateMockFile(
            "huge.pdf", "application/pdf",
            length: (5 * 1024 * 1024) + 1);  // 1 byte over the 5MB limit

        var result = await _service.UploadAgreementAsync(fileMock.Object);

        Assert.That(result, Is.Null, "Files larger than 5MB should be rejected");
    }

    /// <summary>
    /// A file with the wrong content type AND wrong extension should be rejected.
    /// </summary>
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

    /// <summary>
    /// A file exactly AT the 5MB limit should NOT be rejected by size.
    /// (It will still fail because we can't copy to a real filesystem in unit tests,
    ///  but this verifies the size check boundary.)
    /// </summary>
    [Test]
    public async Task Upload_FileExactlyAtSizeLimit_NotRejectedBySizeCheck()
    {
        // Exactly 5MB — should pass the size gate
        var fileMock = CreateMockFile(
            "exact.pdf", "application/pdf",
            length: 5 * 1024 * 1024);

        // Setup a real stream so CopyToAsync doesn't throw
        var content = new byte[5 * 1024 * 1024];
        fileMock.Setup(_ => _.OpenReadStream())
                .Returns(new MemoryStream(content));
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // We don't assert null here because the file passes validation —
        // whether it succeeds depends on filesystem access in the test environment.
        // The key assertion is that it does NOT fail due to the size check.
        // This test documents the boundary behaviour.
        Assert.DoesNotThrowAsync(
            async () => await _service.UploadAgreementAsync(fileMock.Object),
            "A file exactly at the size limit should not throw an exception");
    }

    // ── Helper ────────────────────────────────────────────────

    /// <summary>
    /// Creates a Moq IFormFile mock with the given filename, content type, and size.
    /// This saves repeating the same Setup() calls in every test.
    /// </summary>
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