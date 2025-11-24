using LanguageExt;

namespace Budget;

public sealed record Category(string Value);

public abstract record Classification(LineItem LineItem) : IComparable<Classification>
{
    public int CompareTo(Classification? other) => this == other ? 0 : 1;
};

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);

// totals have to be enforced by app logic? Technically this just shows need for "data storage object"
public sealed record SubClassifications(Seq<SubCategorized> Children, LineItem LineItem) : Classification(LineItem);

public sealed record SubCategorized(Category Category, decimal Amount);


public sealed record LineItem(string Description, decimal Amount, DateTime Date) : IComparable<LineItem>
{
    public int CompareTo(LineItem? other) => this == other ? 0 : -1;
}