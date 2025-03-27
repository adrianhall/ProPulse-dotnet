using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProPulse.IdentityService.Utils;
using System.Text.Json;

namespace ProPulse.IdentityService.Extensions;

public static class InternalExtensions
{
    /// <summary>
    /// Adds a facebook authentication provider to the authentication builder if the provided
    /// configuration section contains a ClientId and ClientSecret.
    /// </summary>
    /// <param name="builder">The source authentication builder.</param>
    /// <param name="configuration">The configuration section potentially holding the ClientId and ClientSecret.</param>
    /// <returns>The authentication builder (for chaining)</returns>
    internal static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a google authentication provider to the authentication builder if the provided
    /// configuration section contains a ClientId and ClientSecret.
    /// </summary>
    /// <param name="builder">The source authentication builder.</param>
    /// <param name="configuration">The configuration section potentially holding the ClientId and ClientSecret.</param>
    /// <returns>The authentication builder (for chaining)</returns>
    internal static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a google authentication provider to the authentication builder if the provided
    /// configuration section contains a ClientId and ClientSecret.
    /// </summary>
    /// <param name="builder">The source authentication builder.</param>
    /// <param name="configuration">The configuration section potentially holding the ClientId and ClientSecret.</param>
    /// <returns>The authentication builder (for chaining)</returns>
    internal static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the list of error messages contained within the model state dictionary.
    /// </summary>
    internal static IEnumerable<string> GetErrorMessages(this ModelStateDictionary modelState)
        => modelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

    /// <summary>
    /// Gets the (first found) error message from a model state dictionary, or an empty string if no error.
    /// </summary>
    internal static string GetErrorMessage(this ModelStateDictionary modelState)
        => modelState.GetErrorMessages().FirstOrDefault() ?? string.Empty;
        
    /// <summary>
    /// Converts the object to a JSON string - normally for logging.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="serializerOptions">The serializer options; use defaults if not specified.</param>
    /// <returns>The JSON serialization of the object.</returns>
    internal static string ToJsonString(this object obj, JsonSerializerOptions? serializerOptions = null)
    {
        serializerOptions ??= Defaults.SerializerOptions;
        return JsonSerializer.Serialize(obj, serializerOptions);
    }

    /// <summary>
    /// Returns a required configuration string from the configuration store.
    /// </summary>
    /// <param name="configuration">The configuration store (root or section).</param>
    /// <param name="key">The key of the required value.</param>
    /// <returns>The value of the key as a string.</returns>
    /// <exception cref="KeyNotFoundException">If the key does not exist in the configuration store.</exception>
    public static string GetRequiredString(this IConfiguration configuration, string key)
        => configuration.HasKey(key) ? configuration[key]! : throw new KeyNotFoundException($"Configuration key '{key}' not found");

    /// <summary>
    /// Determines if the provided key has a value.
    /// </summary>
    /// <param name="configuration">The configuration (root or section).</param>
    /// <param name="key">The key to look for, relative to the root of the provided configuration section.</param>
    /// <returns><c>true</c> if the key exists; false otherwise.</returns>
    public static bool HasKey(this IConfiguration configuration, string key)
        => !string.IsNullOrWhiteSpace(configuration[key]);
}
