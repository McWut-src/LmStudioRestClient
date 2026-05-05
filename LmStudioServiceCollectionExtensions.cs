using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LmStudioRestClient;

/// <summary>
/// Provides extension methods for registering LM Studio client services.
/// </summary>
public static class LmStudioServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILmStudioClient"/> using values from appsettings.json
    /// under the "LmStudio" section. Call this overload when you want pure config-file
    /// driven setup.
    /// </summary>
    public static IServiceCollection AddLmStudioClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LmStudioClientOptions>(
            configuration.GetSection(LmStudioClientOptions.SectionName));

        return services.AddCoreClient();
    }

    /// <summary>
    /// Registers <see cref="ILmStudioClient"/> with inline configuration.
    /// Any values not explicitly set fall back to <see cref="LmStudioClientOptions"/> defaults.
    /// </summary>
    public static IServiceCollection AddLmStudioClient(
        this IServiceCollection services,
        Action<LmStudioClientOptions> configure)
    {
        services.Configure(configure);
        return services.AddCoreClient();
    }

    /// <summary>
    /// Registers <see cref="ILmStudioClient"/> allowing both a config section baseline
    /// and programmatic overrides (overrides win).
    /// </summary>
    public static IServiceCollection AddLmStudioClient(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LmStudioClientOptions> configure)
    {
        services
            .Configure<LmStudioClientOptions>(
                configuration.GetSection(LmStudioClientOptions.SectionName))
            .PostConfigure(configure);

        return services.AddCoreClient();
    }

    private static IServiceCollection AddCoreClient(this IServiceCollection services)
    {
        // Use IHttpClientFactory for proper socket/lifetime management
        services.AddHttpClient<ILmStudioClient, LmStudioClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<LmStudioClientOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = options.Timeout;

            if (!string.IsNullOrWhiteSpace(options.ApiToken))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiToken);
            }
        });

        return services;
    }
}