using System.Text;
using Budget;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;
using static Budget.Utilities;
using Console = Budget.Console;

Eff<IConsole, Unit> log(string message) => askE<IConsole>().Bind(c => c.WriteLine(message));

Eff<IConsole, string> readLine() => askE<IConsole>().Bind(c => c.ReadLine());

Eff<IConsole, Categorized> reselectCategory(Seq<Category> categories, LineItem lineItem1) =>
    from _1 in log($"Please select a number between 1 and {categories.Count}")
    from selection in readLine()
    from result in int.TryParse(selection, out var index)
        ? selectCategory(index, categories, lineItem1)
        : reselectCategory(categories, lineItem1)
    select result;

Eff<IConsole, Categorized> selectCategory(int result, Seq<Category> seq, LineItem lineItem1) =>
    cond([
            (result < 1, reselectCategory(seq, lineItem1)),
            (result > seq.Count, reselectCategory(seq, lineItem1))
        ], new Categorized(seq[result - 1], lineItem1))
        .As();

Eff<IConsole, Classification> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem1) =>
    new Fail<Error>(new NotImplementedException());

Eff<IConsole, Classification> classifyIncome(string s, Seq<Category> seq, LineItem lineItem1)
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
Eff<IConsole, Classification> selectCategoryStr(string input, Seq<Category> categories, LineItem lineItem) =>
    parseInt(input)
        .Match(index => selectCategory(index, categories, lineItem), 
            () =>  reselectCategory(categories, lineItem))
        .Map(c => (Classification)c)
        .As();

Eff<IConsole, Classification> classifyFromInput(string input, Seq<Category> categories, LineItem lineItem) =>
    cond([
            (string.IsNullOrWhiteSpace(input), log("Please enter a valid (non-empty) value")
                .Bind(_ => classify(categories, lineItem))),
            (parseInt(input).IsSome, selectCategoryStr(input, categories, lineItem)),
            (input.StartsWith('*'), applySubClassifications(input, categories, lineItem)),
            (input.ToLower().StartsWith("income"), classifyIncome(input, categories, lineItem))
        ], new Categorized(new Category(input.Trim()), lineItem))
        .As();

string getMainPrompt(Seq<Category> categories, LineItem lineItem) =>
    string.Join(Environment.NewLine, $"{lineItem.Description}: {lineItem.Amount:N}"
        .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Value}")));

Eff<IConsole, Classification> classify(Seq<Category> categories, LineItem lineItem) =>
    from _1 in log(getMainPrompt(categories, lineItem))
    from input in readLine()
    from result in classifyFromInput(input, categories, lineItem)
    select result;

var cats = Seq(new Category("Almsgiving"), new Category("Food"), new Category("Cart"));

var lineItems = Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
    new LineItem("Progressive Insurance", 800M, DateTime.Now),
    new LineItem("Stuff", 10, DateTime.Now));

lineItems.TraverseM(l => classify(cats, l))
    .Run(new Console())
    .ThrowIfFail();

// Similarly nothing to do with budget at all, but generally useful for C#? Doesn't even need LanguageExt dep!
    // public static ArgumentException patternMatchError<Supertype>(object unmatchable, string? paramName = null) =>
    //     new ($"Unknown case type {unmatchable.GetType().Name} in" +
    //          $" pattern-match for {typeof(Supertype).Name}" +
    //          fileNameAndLine(), paramName);
    //
    // private static string fileNameAndLine()
    // {
    //     var stackTrace = new System.Diagnostics.StackTrace();
    //     // 0 is fileNameAndLine frame, 1 is pattermMatchError, 2 is where this is used?
    //     var matchFrame = stackTrace.GetFrame(2);
    //     if (matchFrame == null || matchFrame.GetFileName() == null)
    //     {
    //         return string.Empty;
    //     }
    //
    //     return $" at {matchFrame.GetFileName()}:{matchFrame.GetFileLineNumber()}";
    // }

// Overall idea: localhost/0.0.0.0 running server, "local first" app that tries to sync with "basic REST" (maybe 
// some kind of tcp/udp check in a background service to avoiding messy polling), but phone is kind of main point of "input". Interesting!
// Do it in MAUI???
// Links to help with project overall
// * Some kind of basis for accessing text messages (at least for Androind) https://stackoverflow.com/questions/72656609/read-sms-for-opt-programically-in-maui-android
// * Background jobs for MAUI: https://github.com/shinyorg/shiny
// * Less terrile MAUI UI? https://github.com/adospace/reactorui-maui