using CloudinaryDotNet;
using DocuMind.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Unit;

public class CloudinaryDirectUploadServiceTests
{
    private static readonly CloudinaryOptions CloudinaryOptions = new()
    {
        CloudName = "test-cloud",
        ApiKey = "test-key",
        ApiSecret = "test-secret",
        Folder = "documind"
    };

    private static CloudinaryDirectUploadService Service() => new(
        Options.Create(new StorageOptions { Cloudinary = CloudinaryOptions }),
        NullLogger<CloudinaryDirectUploadService>.Instance);

    [Theory]
    [InlineData("handbook.pdf")]
    [InlineData("ANNUAL REPORT.PDF")]
    [InlineData("policy.v2.pdf")]
    public void Raw_upload_tickets_sign_the_complete_id_including_the_extension(string fileName)
    {
        var userId = Guid.NewGuid();
        var service = Service();

        var ticket = service.CreateTicket(userId, fileName);

        // Cloudinary adds .pdf when it is absent. The ticket and stored asset must have the same id.
        Assert.EndsWith(".pdf", ticket.PublicId);
        Assert.StartsWith($"documind/{userId:N}/", ticket.PublicId);
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(ticket.PublicId), "N", out _));
        Assert.Equal("raw", ticket.ResourceType);
        Assert.EndsWith("/raw/upload", ticket.UploadUrl);
        Assert.True(service.BelongsToUser(ticket.PublicId, userId));

        var cloudinary = new Cloudinary(new Account(
            CloudinaryOptions.CloudName, CloudinaryOptions.ApiKey, CloudinaryOptions.ApiSecret));
        var expectedSignature = cloudinary.Api.SignParameters(new SortedDictionary<string, object>
        {
            ["public_id"] = ticket.PublicId,
            ["timestamp"] = ticket.Timestamp
        });

        Assert.Equal(expectedSignature, ticket.Signature);
    }

    [Fact]
    public void An_extension_does_not_allow_confirming_another_users_upload()
    {
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var service = Service();
        var ticket = service.CreateTicket(owner, "handbook.pdf");

        Assert.False(service.BelongsToUser(ticket.PublicId, otherUser));
        Assert.False(service.BelongsToUser($"documind/{owner:N}extra/file.pdf", owner));
        Assert.False(service.BelongsToUser($"documind/{owner:N}/nested/file.pdf", owner));
    }
}
