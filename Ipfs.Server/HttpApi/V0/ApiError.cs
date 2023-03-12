using Newtonsoft.Json;

namespace Ipfs.Server.HttpApi.V0;

/// <summary>
///     The standard error response for failing API calls.
/// </summary>
public class ApiError
{
    /// <summary>
    ///     Human readable description of the error.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     Developer readable description of the error.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string[] Details { get; set; }

    /// <summary>
    ///     A standard ??? error code.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Code { get; set; }
}