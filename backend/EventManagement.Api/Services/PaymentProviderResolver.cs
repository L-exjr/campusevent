namespace EventManagement.Api.Services;

public interface IPaymentProviderResolver
{
    IPaymentProvider Active { get; }
    IPaymentProvider Get(string name);
}

public sealed class PaymentProviderResolver(
    IEnumerable<IPaymentProvider> providers,
    IConfiguration configuration) : IPaymentProviderResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers = providers
        .ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);

    public IPaymentProvider Active => Get(
        configuration["PAYMENTS_PROVIDER"] ?? configuration["Payments:Provider"] ?? "Paystack");

    public IPaymentProvider Get(string name) => _providers.TryGetValue(name, out var provider)
        ? provider
        : throw new PaymentProviderException($"Payment provider '{name}' is not configured.");
}
