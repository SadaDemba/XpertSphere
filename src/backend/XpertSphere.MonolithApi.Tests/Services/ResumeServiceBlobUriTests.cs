using Azure.Storage.Blobs;
using FluentAssertions;

namespace XpertSphere.MonolithApi.Tests.Services;

/// <summary>
/// Verifies that <see cref="BlobUriBuilder"/> correctly extracts the blob name from both
/// Azure virtual-hosted-style URLs (real Azure Storage) and Azurite path-style URLs
/// (local emulator), regression-guarding the fix applied to
/// <see cref="XpertSphere.MonolithApi.Services.ResumeService"/> (see
/// azurite-blob-storage-local.md, critère d'acceptation n°7).
/// </summary>
public class ResumeServiceBlobUriTests
{
    [Fact]
    public void BlobUriBuilder_WithAzureVirtualHostedStyleUrl_ShouldExtractCorrectBlobName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uri = new Uri($"https://xpertspheredev.blob.core.windows.net/resumes/{userId}/f.pdf");

        // Act
        var blobName = new BlobUriBuilder(uri).BlobName;

        // Assert
        blobName.Should().Be($"{userId}/f.pdf");
    }

    [Fact]
    public void BlobUriBuilder_WithAzuritePathStyleUrl_ShouldExtractCorrectBlobName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uri = new Uri($"http://127.0.0.1:10000/devstoreaccount1/resumes/{userId}/f.pdf");

        // Act
        var blobName = new BlobUriBuilder(uri).BlobName;

        // Assert
        blobName.Should().Be($"{userId}/f.pdf");
    }
}
