using System.Threading.Tasks;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   A service is async and can be started and stopped.
/// </summary>
public interface IService
{
    /// <summary>
    ///   Start the service.
    /// </summary>
    Task StartAsync();

    /// <summary>
    ///   Stop the service.
    /// </summary>
    Task StopAsync();
}