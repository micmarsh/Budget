using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static Budget.Utilities;
using static LanguageExt.Prelude;

namespace Budget;

public static class UserClassification
{
    
static Eff<IConsole, Unit> log(string message) => askE<IConsole>().Bind(c => c.WriteLine(message));

static Eff<IConsole, string> readLine() => askE<IConsole>().Bind(c => c.ReadLine());

private const int StateCancelledCode = 345;

static Eff<IConsole, Unit> guardNotCancelled(string input) =>
    input.StartsWith("cancel") ? Fail(Error.New(StateCancelledCode, "state cancelled")) : Pure(unit);

static Eff<IConsole, Categorized> reselectCategory(Seq<Category> categories, LineItem lineItem1) =>
    from _1 in log($"Please select a number between 1 and {categories.Count}")
    from selection in readLine()
    from _2 in guardNotCancelled(selection)
    from result in int.TryParse(selection, out var index)
        ? selectCategory(index, categories, lineItem1)
        : reselectCategory(categories, lineItem1)
    select result;

static Eff<IConsole, Categorized> selectCategory(int result, Seq<Category> seq, LineItem lineItem1) =>
    result < 1 || result > seq.Count ? 
        reselectCategory(seq, lineItem1) : 
        Pure(new Categorized(seq[result - 1], lineItem1));


static Eff<IConsole, SubClassifications> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem,
    Seq<Categorized> soFar) =>
    from _1 in guardNotCancelled(s)
    from categorized in getSubCategorized(s, seq, lineItem)
    let all = soFar.Add(categorized)
    from result in subclassifyBasedOnTotals(seq, lineItem, all)
    select result;

private static Eff<IConsole, SubClassifications> subclassifyBasedOnTotals(Seq<Category> seq, LineItem lineItem, Seq<Categorized> all)
{
    var total = all.Sum(c => c.LineItem.Amount);
    if (total > lineItem.Amount)
    {
        var previousItems = toSeq(all.SkipLast());
        var previousTotal = previousItems.Sum(c => c.LineItem.Amount);
        return
            from _1 in log($"Last entry exceeded total by {total - lineItem.Amount:C} (only {lineItem.Amount - previousTotal:C} left), please try again")
            from input in readLine()
            from result in applySubClassifications(input, seq, lineItem, previousItems)
            select result;
    }

    if (total < lineItem.Amount)
    {
        return from _1 in log($"{lineItem.Amount - total:C} remaining to classify")
                from input in readLine()
                from result in applySubClassifications(input, seq, lineItem, all)
                select result;
    }

    return Pure(SubClassifications.New(all, lineItem).IfNone(() => throw new Exception("This should never happen")));

}

static Eff<IConsole, Classification> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem) =>
    applySubClassifications(s, seq, lineItem, Empty).Map(c => (Classification) c);

private static Eff<IConsole, Categorized> getSubCategorized(string s, Seq<Category> seq, LineItem lineItem)
{
    var parts = toSeq(s.Replace("*", "").Trim().Split(" ").Select(s1=> s1.Trim()));
    var partsTuple =
        from cat in parts.At(0)
        from amount in parts.At(1).Bind(parseDecimal)
        select (CategoryString: cat, Amount: amount);
    return partsTuple.Match(
        tuple => parseInt(tuple.CategoryString)
            .Match(selection => selectCategory(selection, seq, lineItem),
                () => Pure(new Categorized(new Category(tuple.CategoryString), lineItem with { Amount = tuple.Amount }))),// needing a whole LineItem here might be excesive, need custom "SubCategorized"!
        () => 
            from _1 in log($"Incorrectly formatted subcategory '{s}', please try again")
            from input in readLine()
            from _2 in guardNotCancelled(input) // didn't anticipate this!
            from result in getSubCategorized(input, seq, lineItem)
            select result
        );
}


static Eff<IConsole, Classification> classifyIncome(string s, Seq<Category> seq, LineItem lineItem1)
{
    var category = s.Replace("income", "").Trim();
    if (int.TryParse(category, out var index))
    {
        return selectCategory(index, seq, lineItem1)
            .Map(cat => (Classification) new Income(cat.Category, cat.LineItem));
    }
    return Pure((Classification) new Income(new Category(category), lineItem1));
}


//todo just an overload once is in proper class (maybe soon)
static Eff<IConsole, Classification> selectCategoryStr(string input, Seq<Category> categories, LineItem lineItem) =>
    parseInt(input)
        .Match(index => selectCategory(index, categories, lineItem), 
            () =>  reselectCategory(categories, lineItem))
        .Map(c => (Classification)c)
        .As();

static Eff<IConsole, Classification> retry(string message, Seq<Category> categories, LineItem lineItem) =>
    log(message).Bind(_ => classify(categories, lineItem));

static Eff<IConsole, Classification> classifyFromInput(string input, Seq<Category> categories, LineItem lineItem) =>
    cond([
            (string.IsNullOrWhiteSpace(input), retry("Please enter a valid (non-empty) value", categories, lineItem)),
            (parseInt(input).IsSome, selectCategoryStr(input, categories, lineItem)),
            (input.StartsWith('*'), applySubClassifications(input, categories, lineItem)),
            (input.ToLower().StartsWith("income"), classifyIncome(input, categories, lineItem))
            // "cancel" is also noteworthy syntax, but not/shouldn't be checked for here
        ], new Categorized(new Category(input.Trim()), lineItem))
        .Catch(StateCancelledCode, _ => retry("Previous in-progress classification cancelled", categories, lineItem))
        .As();

static string getMainPrompt(Seq<Category> categories, LineItem lineItem) =>
    string.Join(Environment.NewLine, $"{lineItem.Description}: {lineItem.Amount:C}"
        .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Value}")));

public static Eff<IConsole, Classification> classify(Seq<Category> categories, LineItem lineItem) =>
    from _1 in log(getMainPrompt(categories, lineItem))
    from input in readLine()
    from result in classifyFromInput(input, categories, lineItem)
    select result;

public static Eff<IConsole, Unit> classifyAll(Func<Classification, IO<Unit>> store, 
    Seq<Category> categories,
    Seq<LineItem> lineItems) =>
    lineItems.FoldM(categories, (cats, lineItem) =>
        from @class in classify(cats, lineItem)
        from _ in store(@class)
        select addNewCategories(@class, cats))
        .IgnoreF()
        .As();

private static Seq<Category> addNewCategories(Classification @class, Seq<Category> cats) => cats;

}