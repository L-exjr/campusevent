namespace EventManagement.Api.Services;

public sealed class ImageStorageException : Exception
{
    public ImageStorageException(string message) : base(message)
    {
    }

    public ImageStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
