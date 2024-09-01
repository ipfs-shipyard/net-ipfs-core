using Newtonsoft.Json;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// Interface for <see cref="IFilestoreApi"/> repsonse.
    /// </summary>
    public interface IFilesStoreApiObjectResponse
    {
        /// <summary>
        /// Holds any error message.
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// Path to the file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The key to the Filestore.
        /// </summary>
        public IFilesStoreKey Key { get; set; }

        /// <summary>
        /// The response offset.
        /// </summary>
        public string Offset { get; set; }

        /// <summary>
        /// The size of the object.
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// The object status.k
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Model for the hold filestore key
    /// </summary>
    public interface IFilesStoreKey
    {
        /// <summary>
        /// Key value.
        /// </summary>
        [JsonProperty("/")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Interface for <see cref="IDupsResponse"/> response.
    /// </summary>
    public interface IDupsResponse
    {
        /// <summary>
        /// Any error in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public string Err { get; set; }

        /// <summary>
        /// The error in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public string Ref { get; set; }
    }
}
