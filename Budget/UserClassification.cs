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

static Eff<IConsole, Classification> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem1) =>
    new Fail<Error>(new NotImplementedException(nameof(applySubClassifications)));

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

static Eff<IConsole, Classification> classifyFromInput(string input, Seq<Category> categories, LineItem lineItem) =>
    cond([
            (string.IsNullOrWhiteSpace(input), log("Please enter a valid (non-empty) value")
                .Bind(_ => classify(categories, lineItem))),
            (parseInt(input).IsSome, selectCategoryStr(input, categories, lineItem)),
            (input.StartsWith('*'), applySubClassifications(input, categories, lineItem)),
            (input.ToLower().StartsWith("income"), classifyIncome(input, categories, lineItem))
            // "cancel" is also noteworthy syntax, but not/shouldn't be checked for here
        ], new Categorized(new Category(input.Trim()), lineItem))
        .Catch(StateCancelledCode, _ => log("Previous in-progress classification cancelled")
            .Bind(_ => classify(categories, lineItem)))
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