namespace Ipfs.CoreApi
{
    /// <summary>
    /// Represents a folder to be added to IPFS.
    /// </summary>
    public record FolderPart
    {
        /// <summary>
        /// The name of the folder or the relative path to the folder in another folder.
        /// </summary>
        public required string Name { get; set; }
    }
}
