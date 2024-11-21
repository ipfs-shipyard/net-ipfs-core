using Newtonsoft.Json;

namespace Ipfs
{
    /// <summary>
    /// A wrapper for a <see cref="Cid"/> that is used in an IPLD Directed Acyclic Graph (DAG).
    /// </summary>
    /// <remarks>
    /// See also <see href="https://ipld.io/specs/codecs/dag-json/spec/#links"/>.
    /// </remarks>
    public record DagCid
    {
        /// <summary>
        /// The <see cref="Cid"/> value of this DAG link.
        /// </summary>
        [JsonProperty("/")]
        public required Cid Value { get; set; }

        /// <summary>
        /// Implicit casting of a <see cref="DagCid"/> to a <see cref="Cid"/>.
        /// </summary>
        /// <param name="dagLink">The <see cref="DagCid"/> to cast.</param>
        public static implicit operator Cid(DagCid dagLink) => dagLink.Value;

        /// <summary>
        /// Explicit casting of a <see cref="Cid"/> to a <see cref="DagCid"/>.
        /// </summary>
        /// <param name="cid">The <see cref="Cid"/> to cast.</param>"
        public static explicit operator DagCid(Cid cid) => new DagCid { Value = cid, };

        /// <summary>
        /// Returns the string representation of the <see cref="DagCid"/>.
        /// </summary>
        /// <returns>
        ///  e.g. "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V"
        /// </returns>
        public override string ToString() => $"{Value}";
    }
}
