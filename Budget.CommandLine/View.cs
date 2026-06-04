using Budget.Migration.Export;
using Budget.Services.Storage.LiteDB;
using CommandLine.Immutable;
using ConsoleApps;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using static CommandLine.Immutable.Parsing;
using static LanguageExt.Prelude;

namespace Budget.CommandLine;

public static class View
{
    // Just get something up and running for command line app, can move business logic from here to main project later
    // Generally want an "interface" (maybe not even that) with
    // GetMonth(year month) -> HashMap<Category, Amount>
    // GetRange(StartDate, EndDate) -> HashMap<(year, month), HashMap<Category, Amount>
    //   (these are separate in case can be optimized at storage layer)
    // GetAverages(Enum Mean/Median) -> HashMap<Category, Amount>

    private static readonly System.CommandLine.Option<uint> SingleYearOpt = new ("--year", "-y")
    {
        Required = true
    };

    private static readonly System.CommandLine.Option<Month> SingleMonthOpt = new("--month", "-m")
    {
        Required = true,
        CustomParser = factory(arg => Enum.GetValues<Month>()
            .AsIterable()
            //todo probably match numbers too?
            .Filter(m => m.ToString().ToLower().StartsWith(arg.Tokens[0].Value.ToLower()))
            .Head
            .ToFin(Error.New($"Unable to match '{arg.Tokens[0].Value}' to a month")))
    };

    public static readonly ICmd Command =
        Cmd.New("view", "View spending/income for a month, range of months, or overall averages")
            .AddSub(Cmd.New("month", "View spending/income for a single month")
                .AddOption(Shared.DbString)
                .AddOption(SingleMonthOpt)
                .AddOption(SingleYearOpt)
                .AddOption(Shared.SetDb)
                .WithAction((dbString, month, year, shouldSetDb) => 
                    RunView(dbString, (int) month, (int) year) >> Shared.maybeSetDbPath(shouldSetDb, dbString) * ignore)
            );

    //todo re-do this to query in-db
    private static IO<Unit> RunView(FileInfo dbString, int month, int year) =>
        bracketIO(IO.lift(() => new LiteDb(dbString, ObjectId.NewObjectId)),
            storage => storage
                .GetDateRange(new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month)))
                .Reduce(HashMap<Category, decimal>(), (map, c) =>
                        c.Record switch
                        {
                            Categorized(var category, var lineItem) => map.AddOrUpdate(category, lineItem.Amount),
                            SubClassifications subs => map.AddOrUpdateRange(subs.Children.Map(sub => (sub.Category, sub.Amount))),
                            UnCategorized => map,
                            _ => throw Utilities.patternMatchError(c.Record)
                        }
                )
                .Bind(map => map.AsIterable().OrderBy(pair => pair.Value) //todo get this (and everything else) in order lol
                    .AsIterable()
                    .Traverse(pair => Prompt.logIO(Prompt.formattedLine(pair.Key.Value, pair.Value.ToString(), 50))))
                .Map(ignore),
            exporter => IO.lift(exporter.Dispose)).As();
}

public enum Month
{
    January = 1,
    February,
    March,
    April,
    May,
    June,
    July,
    August,
    September,
    October,
    November,
    December
}
