using LanguageExt;

namespace BudgetClassifier;

// not in "Domain" b/c is likely getting moved to console-only project/namespace, not shared
public record CategorySelectOption(Category Category, bool IsIncome)
{
    public static Seq<CategorySelectOption> Create(Classification classification) =>
        classification switch
        {
            Categorized(var category, { Amount: var amount }) => [new CategorySelectOption(category, amount > 0)],
            SubClassifications subs => subs.Children.Map(c => new CategorySelectOption(c.Category, subs.LineItem.Amount > 0)),
            UnCategorized => [],
            _ => throw Utilities.patternMatchError(classification)
        };
};