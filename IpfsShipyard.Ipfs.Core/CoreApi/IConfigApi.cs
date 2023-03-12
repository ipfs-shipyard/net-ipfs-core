using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Core.CoreApi;

/// <summary>
///   Manages the IPFS Configuration.
/// </summary>
/// <remarks>
///   <para>
///   Configuration values are JSON.  <see href="http://www.newtonsoft.com/json">Json.NET</see>
///   is used to represent JSON.
///   </para>
/// </remarks>
/// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/CONFIG.md">Config API spec</seealso>
public interface IConfigApi
{
    /// <summary>
    ///   Gets the entire configuration.
    /// </summary>
    /// <returns>
    ///   A <see cref="JObject"/> containing the configuration.
    /// </returns>
    Task<JObject> GetAsync(CancellationToken cancel = default);

    /// <summary>
    ///   Gets the value of a configuration key.
    /// </summary>
    /// <param name="key">
    ///   The key name, such as "Addresses.API".
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   The value of the <paramref name="key"/> as <see cref="JToken"/>.
    /// </returns>
    /// <exception cref="Exception">
    ///   When the <paramref name="key"/> does not exist.
    /// </exception>
    /// <remarks>
    ///   Keys are case sensitive.
    /// </remarks>
    Task<JToken> GetAsync(string key, CancellationToken cancel = default);

    /// <summary>
    ///   Adds or replaces a configuration value.
    /// </summary>
    /// <param name="key">
    ///   The key name, such as "Addresses.API".
    /// </param>
    /// <param name="value">
    ///   The new <see cref="string"/> value of the <paramref name="key"/>.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task SetAsync(string key, string value, CancellationToken cancel = default);

    /// <summary>
    ///   Adds or replaces a configuration value.
    /// </summary>
    /// <param name="key">
    ///   The key name, such as "Addresses.API".
    /// </param>
    /// <param name="value">
    ///   The new <see cref="JToken">JSON</see> value of the <paramref name="key"/>.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task SetAsync(string key, JToken value, CancellationToken cancel = default);

    /// <summary>
    ///   Replaces the entire configuration.
    /// </summary>
    /// <param name="config"></param>
    Task ReplaceAsync(JObject config);
}