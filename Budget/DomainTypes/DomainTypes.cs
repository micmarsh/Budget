using LanguageExt;

namespace Budget;

public sealed record Category(string Value); // NonEmpty string, use those "domain NewType substitutes", for this?

public abstract record Classification(LineItem LineItem);

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);
public sealed record Income(Category Category, LineItem LineItem) : Classification(LineItem);
public sealed record Annual(Category Category, LineItem LineItem) : Classification(LineItem);

public sealed record SubClassifications : Classification
{
    // prevent recursive craziness, "Single" and "Multiple" subtypes are now clearly distinct
    public Seq<Categorized> Children { get; }

    private SubClassifications(Seq<Categorized> children, LineItem lineItem) : base(lineItem)
    {
        Children = children;
    }

    public static Option<SubClassifications> New(Seq<Categorized> children, LineItem lineItem)
        => lineItem.Amount == children.Map(x => x.LineItem.Amount).Sum(x => x) ?
            new SubClassifications(children, lineItem) :
            Option<SubClassifications>.None;
}

public sealed record LineItem(string Description, decimal Amount, DateTime Date);