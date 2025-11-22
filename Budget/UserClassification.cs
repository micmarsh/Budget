using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static Budget.Utilities;
using static LanguageExt.Prelude;

namespace Budget;

public static class UserClassification
{
    public static Eff<Runtime, Unit> classifyAll(Seq<CategorySelectOption> categories, Seq<LineItem> lineItems) =>
        // Need "FoldBack" in order to stack actions in correct order? Is this a bug?
        lineItems.FoldBackM(categories, (cats, lineItem) =>
                from @class in classify.CoMap(getClassifyRuntime(cats, lineItem))
                from store in asks((Runtime rt) => rt.Storage)
                from _ in store.Save(@class)
                select addNewCategories(@class, cats))
            .IgnoreF()
            .As();

    private static Func<Runtime, ClassifyRT> getClassifyRuntime(Seq<CategorySelectOption> cats, LineItem lineItem)
    {
        //todo filter categories! It's so simple!
        return rt => new ClassifyRT(rt.Console, cats, lineItem);
    }

    /// <summary>
    /// Public for testing only
    /// </summary>
    public sealed record ClassifyRT(IConsole Console, Seq<CategorySelectOption> Categories, LineItem LineItem);

    /// <summary>
    /// Public for testing only
    /// </summary>
    public static readonly Eff<ClassifyRT, Classification> classify =
        from rt in askE<ClassifyRT>()
        from _1 in log(getMainPrompt(rt.Categories, rt.LineItem))
        from input in readLine
        from result in classifyFromInput(input)
        select result;
    
    static Eff<ClassifyRT, Classification> classifyFromInput(string input) =>
        cond([
                (string.IsNullOrWhiteSpace(input), retry("Please enter a valid (non-empty) value")),
                (parseInt(input).IsSome, selectCategory(input)),
                (input.StartsWith('*'), applySubClassifications(input)),
                (input.Equals("cancel"), retry("Nothing to cancel"))
            ], askE<ClassifyRT>().Map(rt => (Classification) new Categorized(new Category(input.Trim()), rt.LineItem)))
            .Catch(StateCancelledCode, _ => retry("Previous in-progress classification cancelled"))
            .As();
    
    static Eff<ClassifyRT, Classification> retry(string message) => log(message).Bind(_ => classify);
    

    static string getMainPrompt(Seq<CategorySelectOption> categories, LineItem lineItem) =>
        string.Join(Environment.NewLine, $"{lineItem.Description}: {lineItem.Amount:C} on {lineItem.Date:D}"
            .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Category.Value}${(c.IsIncome ? " (Income)" : "")}")));

    private static Seq<CategorySelectOption> addNewCategories(Classification @class, Seq<CategorySelectOption> cats) =>
        CategorySelectOption.Create(@class).Concat(cats).Distinct();

    static Eff<ClassifyRT, Classification> selectCategory(string input) =>
        parseInt(input)
            .Match(selectCategory, () => reselectCategory)
            .Map(c => (Classification)c)
            .As();
    
    static Eff<ClassifyRT, Categorized> selectCategory(int index) =>
        from rt in askE<ClassifyRT>()
        let cats = rt.Categories
        from result in index< 1 || index > cats.Count ? 
            reselectCategory : 
            Pure(new Categorized(cats[index - 1].Category, rt.LineItem))
        select result;
    
    
    static readonly Eff<ClassifyRT, Categorized> reselectCategory =
        from rt in askE<ClassifyRT>()
        from _1 in log($"Please select a number between 1 and {rt.Categories.Count}")
        from selection in readLine
        from _2 in guardNotCancelled(selection)
        from result in int.TryParse(selection, out var index)
            ? selectCategory(index)
            : reselectCategory
        select result;
    
    static Eff<ClassifyRT, Classification> applySubClassifications(string s) =>
        applySubClassifications(s, Empty).Map(c => (Classification) c);
    
    static Eff<ClassifyRT, SubClassifications> applySubClassifications(string s, Seq<SubCategorized> soFar) =>
        from _1 in guardNotCancelled(s)
        from categorized in getSubCategorized(s)
        let all = soFar.Add(categorized)
        from result in subclassifyBasedOnTotals(all)
        select result;

    private static Eff<ClassifyRT, SubClassifications> subclassifyBasedOnTotals(Seq<SubCategorized> all)
        => askE<ClassifyRT>().Map(c => c.LineItem).Bind(lineItem => 
        {
            var total = all.Sum(c => c.Amount);
            if (total > lineItem.Amount)
            {
                var previousItems = toSeq(all.SkipLast());
                var previousTotal = previousItems.Sum(c => c.Amount);
                return
                    from _1 in log($"Last entry exceeded total by {total - lineItem.Amount:C} (only {lineItem.Amount - previousTotal:C} left), please try again")
                    from input in readLine
                    from result in applySubClassifications(input, previousItems)
                    select result;
            }

            if (total < lineItem.Amount)
            {
                return from _1 in log($"{lineItem.Amount - total:C} remaining to classify")
                    from input in readLine
                    from result in applySubClassifications(input, all)
                    select result;
            }

            return Pure(SubClassifications.New(all, lineItem).IfNone(() => throw new Exception("This should never happen")));
        });

    private static Eff<ClassifyRT, SubCategorized> getSubCategorized(string s)
    {
        var parts = toSeq(s.Replace("*", "").Trim().Split(" ").Select(s1=> s1.Trim()));
        var partsTuple =
            from cat in parts.At(0)
            from amount in parts.At(1).Bind(parseDecimal)
            select (CategoryString: cat, Amount: amount);
        return partsTuple.Match(
            tuple => parseInt(tuple.CategoryString)
                .Match(selection => selectCategory(selection).Map(c => new SubCategorized(c.Category, tuple.Amount)),
                    () => Pure(new SubCategorized(new Category(tuple.CategoryString), tuple.Amount))),
            () => 
                from _1 in log($"Incorrectly formatted subcategory '{s}', please try again")
                from input in readLine
                from _2 in guardNotCancelled(input) // didn't anticipate this!
                from result in getSubCategorized(input)
                select result
            );
    }
    
    static Eff<ClassifyRT, Unit> log(string message) => askE<ClassifyRT>().Bind(c => c.Console.WriteLine(message));

    static readonly Eff<ClassifyRT, string> readLine = askE<ClassifyRT>().Bind(c => c.Console.ReadLine());

    private const int StateCancelledCode = 345;

    static Eff<ClassifyRT, Unit> guardNotCancelled(string input) =>
        input.StartsWith("cancel") ? Fail(Error.New(StateCancelledCode, "state cancelled")) : Pure(unit);
}