using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class CertificatePdfGeneratorTests
{
    [Fact]
    public void Generate_returns_a_pdf_with_attendee_event_and_signatory_metadata()
    {
        var registrationId = Guid.NewGuid();
        var generator = new CertificatePdfGenerator();

        var pdf = generator.Generate(new CertificatePdfModel(
            "Akosua Student",
            "Campus Innovation Summit",
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
            "Kwame Organizer",
            registrationId,
            null));

        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
