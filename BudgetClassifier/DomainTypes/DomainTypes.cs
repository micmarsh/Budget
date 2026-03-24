using LanguageExt;

namespace BudgetClassifier;

// Are these really "DomainTypes"? There's barely any kind of validation baked-in at all

public readonly record struct Category(string Value);

public abstract record Classification(LineItem LineItem) : IComparable<Classification>
{
    public int CompareTo(Classification? other) => this == other ? 0 : 1;
};

public sealed record UnCategorized(LineItem LineItem) : Classification(LineItem);

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);

// totals have to be enforced by app logic? Technically this just shows need for "data storage object"
public sealed record SubClassifications(Seq<SubCategorized> Children, LineItem LineItem) : Classification(LineItem);

public readonly record struct SubCategorized(Category Category, decimal Amount);

public readonly record struct LineItem(string Description, decimal Amount, DateTime Date) : IComparable<LineItem>
{
    public int CompareTo(LineItem? other) => this == other ? 0 : -1;
    public int CompareTo(LineItem other) => CompareTo((LineItem?)other);
}