using System.Text;
using Budget;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;
using static Budget.Utilities;
using Console = Budget.Console;


var cats = Seq(new Category("Almsgiving"), new Category("Food"), new Category("Cart"));

var lineItems = Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
    new LineItem("Progressive Insurance", 800M, DateTime.Now),
    new LineItem("Stuff", 10, DateTime.Now));

lineItems.TraverseM(l => UserClassification.classify(cats, l))
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