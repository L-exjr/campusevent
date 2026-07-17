using System.Globalization;
using System.Security.Cryptography;

namespace EventManagement.Api.Services;

public enum PasswordVerificationResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerificationResult Verify(string password, string? storedHash);
}

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string CurrentVersion = "v2";
    private const string CurrentAlgorithm = "pbkdf2-sha256";
    private const int CurrentIterations = 600_000;
    private const int LegacyIterations = 210_000;
    private const int MinimumAcceptedIterations = 100_000;
    private const int MaximumAcceptedIterations = 1_200_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int MaximumEncodedHashLength = 500;

    // A valid, fixed-cost hash used only to make unknown-user verification do
    // the same PBKDF2 work as verification for an existing current-format user.
    private const string DummyHash =
        "v2$pbkdf2-sha256$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            CurrentIterations,
            HashAlgorithmName.SHA256,
            HashLength);

        return string.Join(
            '$',
            CurrentVersion,
            CurrentAlgorithm,
            CurrentIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string password, string? storedHash)
    {
        if (password is null) return PasswordVerificationResult.Failed;

        var isDummyVerification = string.IsNullOrWhiteSpace(storedHash);
        var hashToVerify = isDummyVerification ? DummyHash : storedHash!;
        if (!TryParse(hashToVerify, out var parsed))
            return PasswordVerificationResult.Failed;

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            parsed.Salt,
            parsed.Iterations,
            HashAlgorithmName.SHA256,
            parsed.ExpectedHash.Length);
        var verified = CryptographicOperations.FixedTimeEquals(actual, parsed.ExpectedHash);
        CryptographicOperations.ZeroMemory(actual);

        if (isDummyVerification || !verified) return PasswordVerificationResult.Failed;
        return parsed.NeedsRehash
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    private static bool TryParse(string storedHash, out ParsedPasswordHash parsed)
    {
        parsed = default;
        if (storedHash.Length > MaximumEncodedHashLength) return false;

        var parts = storedHash.Split('$');
        string version;
        string iterationsText;
        string saltText;
        string hashText;

        if (parts is ["v1", _, _, _])
        {
            version = parts[0];
            iterationsText = parts[1];
            saltText = parts[2];
            hashText = parts[3];
        }
        else if (parts is [CurrentVersion, CurrentAlgorithm, _, _, _])
        {
            version = parts[0];
            iterationsText = parts[2];
            saltText = parts[3];
            hashText = parts[4];
        }
        else
        {
            return false;
        }

        if (!int.TryParse(
                iterationsText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var iterations))
            return false;
        if (version == "v1" && iterations != LegacyIterations) return false;
        if (iterations is < MinimumAcceptedIterations or > MaximumAcceptedIterations) return false;

        try
        {
            var salt = Convert.FromBase64String(saltText);
            var expectedHash = Convert.FromBase64String(hashText);
            if (salt.Length != SaltLength || expectedHash.Length != HashLength) return false;

            parsed = new ParsedPasswordHash(
                iterations,
                salt,
                expectedHash,
                version != CurrentVersion || iterations < CurrentIterations);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private readonly record struct ParsedPasswordHash(
        int Iterations,
        byte[] Salt,
        byte[] ExpectedHash,
        bool NeedsRehash);
}
