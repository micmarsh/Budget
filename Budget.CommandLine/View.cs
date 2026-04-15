using System.CommandLine;
using Budget.Config;
using Budget.Migration.Export;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static CommandLine.Immutable.Parsing;

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

    private static readonly System.CommandLine.Option<string> DbString = new("-db")
    {
        DefaultValueFactory = factory(_ => Database.readDbFilePath.RunSafe()),
        Required = false
    };
    
    //todo move this somewhere
    private static readonly System.CommandLine.Option<bool> SetDb = new("--set-db")
    {
        Required = false
    };

    private static IO<Unit> log(object? obj) => IO.lift(() => System.Console.WriteLine(obj));

    public static readonly ICmd Command =
        Cmd.New("view", "View spending/income for a month, range of months, or overall averages")
            .AddSub(Cmd.New("month", "View spending/income for a single month")
                .AddOption(DbString)
                .AddOption(SingleMonthOpt)
                .AddOption(SingleYearOpt)
                .AddOption(SetDb)
                .WithAction((dbString, month, year, shouldSetDb) => 
                    RunView(dbString, month, year) >> 
                    (shouldSetDb ? Database.setDbFilePath(dbString) : Prelude.unitIO))
            );

    private static IO<Unit> RunView(string dbString, Month month, uint year)
    {
        return Prelude.bracketIO(IO.lift(() => new LiteDBExport(dbString)),
            exporter => exporter.ExportClassifications()
                .Filter(c => c.Date.Month == (int)month && c.Date.Year == (int)year)
                .Reduce(HashMap<Category, decimal>.Empty, (map, c) =>
                    c.Category.Match(
                        category => map.AddOrUpdate(new Category(category), total => total + c.Amount,
                            c.Amount),
                        None: () => map))
                .Bind(map => map.AsIterable().OrderBy(pair => pair.Key.Value) //todo get this (and everything else) in order lol
                    .AsIterable()
                    .Traverse(pair => log($"{pair.Key.Value}: {pair.Value}")))
                .Map(Prelude.ignore),
            exporter => IO.lift(exporter.Dispose)).As();
    }
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
