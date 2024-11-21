namespace Ipfs.CoreApi
{
    /// <summary>
    /// The response object for the <see cref="IFilestoreApi.DupsAsync"/> method.
    /// </summary>
    public record FilestoreDuplicate
    {
        /// <summary>
        /// Any error in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public required string Err { get; set; }

        /// <summary>
        /// The cid in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public required Cid Ref { get; set; }
    }
}
