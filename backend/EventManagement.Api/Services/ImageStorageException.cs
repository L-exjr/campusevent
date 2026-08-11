namespace EventManagement.Api.Services;

public enum ImageStorageFailureKind
{
    Configuration,
    ProviderRejected,
    ProviderUnavailable
}

public sealed class ImageStorageException : Exception
{
    public ImageStorageException(ImageStorageFailureKind kind, string message) : base(message)
    {
        Kind = kind;
    }

    public ImageStorageException(
        ImageStorageFailureKind kind,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public ImageStorageFailureKind Kind { get; }
}
