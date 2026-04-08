namespace PMQ.Mediator;

/// <summary>
/// Represents a void type since <see cref="System.Void"/> is not a valid return type in C#.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>
    /// Default and only value of the <see cref="Unit"/> type.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// A pre-cached completed <see cref="Task{Unit}"/> with the default <see cref="Unit"/> value.
    /// </summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    int IComparable.CompareTo(object? obj) => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether two <see cref="Unit"/> instances are equal. Always returns <c>true</c>.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two <see cref="Unit"/> instances are not equal. Always returns <c>false</c>.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
