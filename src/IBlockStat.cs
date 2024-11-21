namespace Ipfs
{
    /// <summary>
    /// Represents a block provided to or yielded from the <see cref="CoreApi.IBlockApi"/>.
    /// </summary>
    /// <remarks>
    /// Analogous to the BlockStat struct in Kubo. See <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/block.go#L21"/>.
    /// </remarks>
    public interface IBlockStat : IDataBlock
    {
        /// <summary>
        /// The size of the block.
        /// </summary>
        public int Size { get; set; }
    }
}
