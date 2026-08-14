using System.Security.Cryptography;

namespace EventManagement.Api.Services;

public static class TicketCodes
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        Span<char> code = stackalloc char[12];
        "EMS-".AsSpan().CopyTo(code);
        for (var index = 0; index < bytes.Length; index++)
            code[index + 4] = Alphabet[bytes[index] % Alphabet.Length];
        return new string(code);
    }
}
