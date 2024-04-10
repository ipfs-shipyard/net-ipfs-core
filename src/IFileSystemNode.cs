namespace Ipfs
{
    /// <summary>
    ///   A Directed Acyclic Graph (DAG) for IPFS file system node.
    /// </summary>
    public interface IFileSystemNode : IMerkleNode<IFileSystemLink>
    {
        /// <summary>
        ///   Determines if the node is a directory (folder).
        /// </summary>
        /// <value>
        ///   <b>true</b> if the node is a directory; Otherwise <b>false</b>,
        ///   it is some type of a file.
        /// </value>
        bool IsDirectory { get; }

        /// <summary>
        ///   The file name of the IPFS node.
        /// </summary>
        public string Name { get; }
    }
}
