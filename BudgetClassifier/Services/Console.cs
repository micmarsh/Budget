using LanguageExt;

namespace BudgetClassifier;

public class Console : IConsole
{
    public IO<string> ReadLine() => IO.lift(System.Console.ReadLine)
        .Bind(s => s == null ? 
            IO.fail<string>("Somehow read a null string from prompt") : 
            IO.pure(s));

    public IO<Unit> WriteLine(string message) => IO.lift(() => System.Console.WriteLine(message));
}