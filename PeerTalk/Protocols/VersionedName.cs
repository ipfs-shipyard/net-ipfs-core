using System;
using System.Linq;
using Semver;

namespace IpfsShipyard.PeerTalk.Protocols;

/// <summary>
///   A name with a semantic version.
/// </summary>
/// <remarks>
///   Implements value type equality.
/// </remarks>
public class VersionedName : IEquatable<VersionedName>, IComparable<VersionedName>
{
    /// <summary>
    ///   The name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///   The semantic version.
    /// </summary>
    public SemVersion Version { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"/{Name}/{Version}";
    }

    /// <summary>
    ///   Parse
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static VersionedName Parse(string s)
    {
        var parts = s.Split('/').Where(p => p.Length > 0).ToArray();
        return new()
        {
            Name = string.Join("/", parts, 0, parts.Length - 1),
            Version = SemVersion.Parse(parts[^1], SemVersionStyles.Strict)
        };
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        var that = obj as VersionedName;
        return that != null
               && Name == that.Name && Version == that.Version;
    }

    /// <inheritdoc />
    public bool Equals(VersionedName that)
    {
        return Name == that.Name && Version == that.Version;
    }

    /// <summary>
    ///   Value equality.
    /// </summary>
    public static bool operator ==(VersionedName a, VersionedName b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null) return false;
        if (b is null) return false;

        return a.Equals(b);
    }

    /// <summary>
    ///   Value inequality.
    /// </summary>
    public static bool operator !=(VersionedName a, VersionedName b)
    {
        if (ReferenceEquals(a, b)) return false;
        if (a is null) return true;
        if (b is null) return true;

        return !a.Equals(b);
    }

    /// <inheritdoc />
    public int CompareTo(VersionedName that) =>
        that == null ? 1 :
        Name == that.Name ? Version.ComparePrecedenceTo(that.Version) :
        string.Compare(Name, that.Name, StringComparison.Ordinal);
}