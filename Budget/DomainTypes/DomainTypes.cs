using LanguageExt;

namespace Budget;

public sealed record Category(string Value);

public abstract record Classification(LineItem LineItem) : IComparable<Classification>
{
    public int CompareTo(Classification? other) => this == other ? 0 : 1;
};

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);

public sealed record SubClassifications : Classification
{
    // prevent recursive craziness, "Single" and "Multiple" subtypes are now clearly distinct
    public Seq<SubCategorized> Children { get; }

    private SubClassifications(Seq<SubCategorized> children, LineItem lineItem) : base(lineItem)
    {
        Children = children;
    }

    public static Option<SubClassifications> New(Seq<SubCategorized> children, LineItem lineItem)
        => Math.Abs(lineItem.Amount) == children.Map(x => x.Amount).Sum(Math.Abs) ?
            new SubClassifications(children, lineItem) :
            Option<SubClassifications>.None;
}
public sealed record SubCategorized(Category Category, decimal Amount);


public sealed record LineItem(string Description, decimal Amount, DateTime Date) : IComparable<LineItem>
{
    public int CompareTo(LineItem? other) => this == other ? 0 : -1;
}