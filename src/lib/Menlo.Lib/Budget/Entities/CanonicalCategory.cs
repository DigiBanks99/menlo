using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// A lean bridge entity providing stable cross-year identity for logical budget categories.
/// Implicitly created when a new category is added to a budget.
/// </summary>
public sealed class CanonicalCategory : IEntity<CanonicalCategoryId>, IAuditable
{
    internal CanonicalCategory(CanonicalCategoryId id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Creates a new <see cref="CanonicalCategory"/> with the given ID and name.
    /// </summary>
    public static CanonicalCategory Create(CanonicalCategoryId id, string name) => new(id, name);

    // Required by EF Core for materialization
    private CanonicalCategory()
    {
        Name = null!;
    }

    /// <summary>Gets the unique identifier for this canonical category.</summary>
    public CanonicalCategoryId Id { get; }

    /// <summary>Gets the canonical name for this category.</summary>
    public string Name { get; private set; }

    // IAuditable
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc />
    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        AuditStamp stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }

        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }
}
