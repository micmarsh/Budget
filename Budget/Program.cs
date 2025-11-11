using System.Text;
using Budget;
using LanguageExt;
using LanguageExt.Traits;
using static LanguageExt.Prelude;
using Seq = LanguageExt.Seq;


IO<Unit> log(string message) => IO.lift(() => Console.WriteLine(message));

IO<string> readLine() => IO.lift(Console.ReadLine)
    .Bind(s => s == null ? 
        IO.fail<string>("Somehow read a null string from prompt") : 
        IO.pure(s));


IO<Classification> selectCategory(int result, Seq<Category> seq, LineItem lineItem1)
{
    throw new NotImplementedException();
}

// K<M, A> cond<M, A>(Seq<(K<M, bool> Pred, K<M, A> True)> seq, A Default)
//     where M : Monad<M>
//     => cond(seq, M.Pure(Default));

//todo based version where everything is monad, overlaods wrap up in Pure as needed
K<M, A> cond<M, A>(Seq<(bool Pred, K<M, A> True)> seq, A Default)
    where M : Monad<M>
    => seq.Rev().Fold(M.Pure(Default), (prev, nextIf) => iff(
        nextIf.Pred,
        nextIf.True,
        prev
    ));

IO<Classification> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem1)
{
    throw new NotImplementedException();
}

IO<Classification> classifyFromInput(string input, Seq<Category> categories, LineItem lineItem) =>
    cond([
            (string.IsNullOrWhiteSpace(input), log("Please enter a valid (non-empty) value")
                .Bind(_ => classify(categories, lineItem))),
            (int.TryParse(input, out var index), selectCategory(index, categories, lineItem)),
            (input.StartsWith('*'), applySubClassifications(input, categories, lineItem))
        ], new Categorized(new Category(input), lineItem))
        .As();

string getMainPrompt(Seq<Category> categories, LineItem lineItem) =>
    string.Join(Environment.NewLine, $"Please classify {lineItem.Description}: {lineItem.Amount:N}"
        .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Value}")));

IO<Classification> classify(Seq<Category> categories, LineItem lineItem) =>
    from _1 in log(getMainPrompt(categories, lineItem))
    from input in readLine()
    from result in classifyFromInput(input, categories, lineItem)
    select result;

//     
// //
// // var parsed = Csv.ParseFile("/home/michael/Downloads/Huntington_Delimited_Old_Account.csv");
// // var categories = Atom(new Seq<Category>());
//
// classify([new Category("Food"), new Category("Car")], new LineItem("THE STORE", 23.45M))
//     .Bind(c => log(c.ToString()))
//     .Run();

//Console.WriteLine("Foo");



// Might be nice own utility, "neglected" sort of "ecosystem library"?

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

// this class is more of a sketch, use function to think about what inputs are needed?
// maybe just need (LineItem -> IO<Classification>), user prompt, categoryStore, and even 
// whether or not user input is needed at all is then somehow encapsulated away? Maybe now
// need to do real design and figuring out problem to solve
public static class BusinessLogic
{
    public static IO<Classification> classify(CategoryStore categories, LineItem input) 
        => throw new NotImplementedException();
}

public sealed record Category(string Value); // NonEmpty string, use those "domain NewType substitutes", for this?

public interface CategoryStore
{
    public IO<Seq<Category>> Query(CategoryQuery query);
    public IO<Unit> Save(Category category);
}
//todo maybe "module" methods that follow the same pattern as http: requires MonadIO and Readable<CategoryStore>?

public abstract record CategoryQuery;
public sealed record Search(string term) : CategoryQuery;


public abstract record Classification(LineItem LineItem);

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);

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

// Overall idea: localhost/0.0.0.0 running server, "local first" app that tries to sync with "basic REST" (maybe 
// some kind of tcp/udp check in a background service to avoiding messy polling), but phone is kind of main point of "input". Interesting!
// Do it in MAUI???
// Links to help with project overall
// * Some kind of basis for accessing text messages (at least for Androind) https://stackoverflow.com/questions/72656609/read-sms-for-opt-programically-in-maui-android
// * Background jobs for MAUI: https://github.com/shinyorg/shiny
// * Less terrile MAUI UI? https://github.com/adospace/reactorui-maui