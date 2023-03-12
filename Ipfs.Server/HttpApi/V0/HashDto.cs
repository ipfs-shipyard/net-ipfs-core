using Newtonsoft.Json;

namespace Ipfs.Server.HttpApi.V0;

/// <summary>
///     A hash to some data.
/// </summary>
public class HashDto
{
    /// <summary>
    ///     An error message.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Error;

    /// <summary>
    ///     Typically a CID.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Hash;
}