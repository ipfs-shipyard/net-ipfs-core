namespace Ipfs.CoreApi
{
    /// <summary>
    /// The result of a DAG resolve operation.
    /// </summary>
    /// <param name="Cid">The cid of the resolved dag path.</param>
    /// <param name="RemPath">Unknown usage. See <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/dag.go#L54"/></param>
    public record DagResolveOutput(DagCid Cid, string RemPath);
}
