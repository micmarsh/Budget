using Budget;
using LanguageExt;
using LanguageExt.Common;
using Console = Budget.Console;

namespace ConsoleApps;
using static LanguageExt.Prelude;
using static Budget.Utilities;

public static class Prompt
{
    private readonly record struct ConsoleOnly(IConsole Console) : IHasConsole
    {
        public static readonly ConsoleOnly Default = new (new Console());
    }

    public static IO<A> readValueIO<A>(Func<string, Option<A>> parse, string retryPrompt) =>
        readValue<ConsoleOnly, A>(parse, retryPrompt).RunIO(ConsoleOnly.Default);
    
    public static Eff<RT, A> readValue<RT, A>(Func<string, Option<A>> parse, string retryPrompt)
        where RT : IHasConsole =>
        from line in readLine<RT>()
        from result in readValue<RT, A>(line, parse, retryPrompt)
        select result;
    
    public static Func<string, Option<int>> parseBetween1And(int max) =>
        str => parseInt(str).Filter(i => i >= 1 && i <= max);
    
    public static Eff<RT, A> readValue<RT, A>(string read, Func<string, Option<A>> parse, string retryPrompt)
        where RT : IHasConsole
        => readValue<RT, A>(IO.pure(read), parse, retryPrompt);

    public static Eff<RT, A> readValue<RT, A>(IO<string> read, Func<string, Option<A>> parse, string retryPrompt)
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

    public static IO<Unit> logIO(string message) => log<ConsoleOnly>(message).RunIO(ConsoleOnly.Default);
    
    public static Eff<RT, Unit> log<RT>(string message)
        where RT : IHasConsole
        => askE<RT>().Bind(c => c.Console.WriteLine(message));

    public static Eff<RT, string> readLine<RT>() 
        where RT : IHasConsole
        => askE<RT>().Bind(c => c.Console.ReadLine());
    
    public const int StateCancelledCode = 345;

    public static IO<Unit> guardNotCancelled(string input) =>
        input.StartsWith("cancel") ? Fail(Error.New(StateCancelledCode, "state cancelled")) : Pure(unit);

}