namespace IpfsShipyard.PeerTalk;

/// <summary>
///   A base for defining a policy.
/// </summary>
/// <typeparam name="T">
///   The type of object that the rule applies to.
/// </typeparam>
public abstract class Policy<T> : IPolicy<T>
{
    /// <inheritdoc />
    public abstract bool IsAllowed(T target);

    /// <inheritdoc />
    public bool IsNotAllowed(T target)
    {
        return !IsAllowed(target);
    }
}

/// <summary>
///   A rule that always passes.
/// </summary>
/// <typeparam name="T">
///   The type of object that the rule applies to.
/// </typeparam>
public class PolicyAlways<T> : Policy<T>
{
    /// <inheritdoc />
    public override bool IsAllowed(T target)
    {
        return true;
    }
}

/// <summary>
///   A rule that always fails.
/// </summary>
/// <typeparam name="T">
///   The type of object that the rule applies to.
/// </typeparam>
public class PolicyNever<T> : Policy<T>
{
    /// <inheritdoc />
    public override bool IsAllowed(T target)
    {
        return false;
    }
}