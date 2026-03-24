using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static BudgetClassifier.Utilities;
using static LanguageExt.Prelude;

namespace BudgetClassifier;

public static class UserClassification
{
    public static Eff<Runtime, Unit> classifyAll(Seq<CategorySelectOption> categories, Seq<LineItem> lineItems) =>
        // Need "FoldBack" in order to stack actions in correct order? Is this a bug?
        lineItems.FoldBackM((Categories: categories, Remaining: lineItems.Length), (state, lineItem) =>
                from rt in askE<Runtime>()
                
                from _1 in rt.Console.WriteLine($"{state.Remaining} items left to classify")
                from @class in classifyWithAuto.CoMap(getClassifyRuntime(state.Categories, lineItem))
                
                // from @class in classify.CoMap(getClassifyRuntime(state.Categories, lineItem))
                from _ in rt.Storage.Save(@class)
                select (addNewCategories(@class, state.Categories), state.Remaining - 1))
            .IgnoreF()
            .As();

    private static Eff<RT, Unit> promptAndSaveAutoCategorized<RT>(Classification @class)
        where RT : IHasConsole, IHasAutoClassifier
        => @class switch
        {
            SubClassifications => unitEff,
            Categorized(var category, {Description: var desc})  => 
                from shouldAdd in readValue<RT, bool>("", parseBoolInput, "Use for auto-classify? (y/true/n/false)")
                from rt in askE<RT>()
                from _2 in when(shouldAdd,  rt.AutoClassifier.Save(desc, category)).As()
                select unit,
            _ => throw patternMatchError(@class)
        };


    static Option<bool> parseBoolInput(string input) =>
        parseBool(input) |
            (input.ToLower().StartsWith("n") ? false : 
                input.ToLower().StartsWith("y") ? Some(true) : 
                None);

    private static Seq<(string Description, Category Category)> getAutoClassifieds(Classification @class) =>
        CategorySelectOption.Create(@class).Map(c => 
            (@class.LineItem.Description, c.Category)
        );

    private static Func<Runtime, ClassifyRT> getClassifyRuntime(Seq<CategorySelectOption> cats, LineItem lineItem) =>
        rt => new ClassifyRT(rt.Console,
            cats.Filter(cat => cat.IsIncome ? lineItem.Amount > 0 : lineItem.Amount < 0
            )
            , lineItem,
            rt.AutoClassifier);

    /// <summary>
    /// Public for testing only
    /// </summary>
    public sealed record ClassifyRT(IConsole Console, Seq<CategorySelectOption> Categories, LineItem LineItem, IAutoClassifier AutoClassifier) 
        : IHasConsole, IHasAutoClassifier;

    /// <summary>
    /// Public for testing only
    /// </summary>
    public static readonly Eff<ClassifyRT, Classification> classify =
        from rt in askE<ClassifyRT>()
        from _1 in log(getMainPrompt(rt.Categories, rt.LineItem))
        from input in readLine()
        from result in classifyFromInput(input)
        select result;

    /// <summary>
    /// Public for testing only
    /// </summary>
    public static readonly Eff<ClassifyRT, Classification> classifyWithAuto =
        from rt in askE<ClassifyRT>()
        let lineItem = rt.LineItem
        from autoCategory in rt.AutoClassifier.Lookup(lineItem.Description)
        from result in autoCategory.Match(category =>
                Pure((Classification)new Categorized(category, lineItem)),
            () => from @class in classify
                        //todo should disable entirely with subcategories because will just auto-set last?
                        from _1 in promptAndSaveAutoCategorized<ClassifyRT>(@class)
                        select @class
        )
        select result;

    
    static Eff<ClassifyRT, Classification> classifyFromInput(string input) =>
        cond([
                (string.IsNullOrWhiteSpace(input), retry("Please enter a valid (non-empty) value")),
                (parseInt(input).IsSome, selectCategory(input).Map(c => (Classification)c)),
                (input.StartsWith('*'), applySubClassifications(input)),
                (input.Equals("cancel"), retry("Nothing to cancel"))
            ], askE<ClassifyRT>().Map(rt => (Classification) new Categorized(new Category(input.Trim()), rt.LineItem)))
            .Catch(StateCancelledCode, _ => retry("Previous in-progress classification cancelled"))
            .As();
    
    static Eff<ClassifyRT, Classification> retry(string message) => log(message).Bind(_ => classify);
    

    static string getMainPrompt(Seq<CategorySelectOption> categories, LineItem lineItem) =>
        string.Join(Environment.NewLine, $"{lineItem.Description}: {lineItem.Amount:C} on {lineItem.Date:D}"
            .Cons((lineItem.Amount > 0 ? "(Income)" : "(Spending)")
                .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Category.Value}"))));

    private static Seq<CategorySelectOption> addNewCategories(Classification @class, Seq<CategorySelectOption> cats) =>
        CategorySelectOption.Create(@class).Concat(cats).Distinct();

    static Eff<ClassifyRT, Categorized> selectCategory(string input) =>
        from rt in askE<ClassifyRT>()
        let cats = rt.Categories
        from index in readValue<ClassifyRT, int>(input, parseBetween1And(cats.Count), $"Please select a number between 1 and {cats.Count}")
        select new Categorized(cats[index - 1].Category, rt.LineItem);


    static Func<string, Option<int>> parseBetween1And(int max) =>
        str => parseInt(str).Filter(i => i >= 1 && i <= max);

    static Eff<ClassifyRT, A> readValue<A>(string read, Func<string, Option<A>> parse, string retryPrompt) =>
        readValue<ClassifyRT, A>(read, parse, retryPrompt);

    
    static Eff<RT, A> readValue<RT, A>(string read, Func<string, Option<A>> parse, string retryPrompt)
        where RT : IHasConsole
        =>
        readValue<RT, A>(IO.pure(read), parse, retryPrompt);

    static Eff<RT, A> readValue<RT, A>(IO<string> read, Func<string, Option<A>> parse, string retryPrompt)
        where RT : IHasConsole
        =>
        from line in read
        from _1 in guardNotCancelled(line)
        from result in parse(line).Match(
            a => Pure(a),
            () =>
                from _2 in log<RT>(retryPrompt)
                from rt in askE<RT>()
                from r in readValue<RT, A>(readLine<RT>().RunIO(rt), parse, retryPrompt)
                select r
        )
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
            var amount = Math.Abs(lineItem.Amount);
            if (total > amount)
            {
                var previousItems = toSeq(all.SkipLast());
                var previousTotal = previousItems.Sum(c => c.Amount);
                return
                    from _1 in log($"Last entry exceeded total by {total - amount:C} (only {amount - previousTotal:C} left), please try again")
                    from input in readLine()
                    from result in applySubClassifications(input, previousItems)
                    select result;
            }

            if (total < amount)
            {
                return from _1 in log($"{amount - total:C} remaining to classify")
                    from input in readLine()
                    from result in applySubClassifications(input, all)
                    select result;
            }

            return Pure(new SubClassifications(all, lineItem));
        });

    private static Eff<ClassifyRT, SubCategorized> getSubCategorized(string s) =>
        from tuple in readValue(s, parseSubCategoryParts, $"Incorrectly formatted subcategory '{s}', please try again")
        from result in parseInt(tuple.CategoryString)
            .Match(_ => selectCategory(tuple.CategoryString)
                    .Map(c => new SubCategorized(c.Category, tuple.Amount)),
                () => Pure(new SubCategorized(new Category(tuple.CategoryString), tuple.Amount)))
        select result;

    private static Option<(string CategoryString, decimal Amount)> parseSubCategoryParts(string str)
    {
        var parts = toSeq(str.Replace("*", "").Trim().Split(" ").Select(s1=> s1.Trim()));
        return 
            from cat in parts.At(0)
            from amount in parts.At(1).Bind(parseDecimal)
            select (CategoryString: cat, Amount: Math.Abs(amount));
    }

    static Eff<ClassifyRT, Unit> log(string message) => log<ClassifyRT>(message);

    private static Eff<ClassifyRT, string> readLine() => readLine<ClassifyRT>();
    
    static Eff<RT, Unit> log<RT>(string message)
        where RT : IHasConsole
        => askE<RT>().Bind(c => c.Console.WriteLine(message));

    static Eff<RT, string> readLine<RT>() 
        where RT : IHasConsole
        => askE<RT>().Bind(c => c.Console.ReadLine());

    private const int StateCancelledCode = 345;

    static IO<Unit> guardNotCancelled(string input) =>
        input.StartsWith("cancel") ? Fail(Error.New(StateCancelledCode, "state cancelled")) : Pure(unit);
}