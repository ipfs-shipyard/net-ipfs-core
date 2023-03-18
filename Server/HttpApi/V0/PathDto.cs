namespace IpfsShipyard.Ipfs.Server.HttpApi.V0;

/// <summary>
///     A path to some data.
/// </summary>
public class PathDto
{
    /// <summary>
    ///     Something like "/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao".
    /// </summary>
    public string Path;

    /// <summary>
    ///     Create a new path.
    /// </summary>
    /// <param name="path"></param>
    public PathDto(string path)
    {
        Path = path;
    }
}