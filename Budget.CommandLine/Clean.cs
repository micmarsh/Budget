using System.CommandLine;
using Budget.FileImport;
using CommandLine.Immutable;
using LanguageExt;

namespace Budget.CommandLine;

public static class Clean
{
    public static readonly Argument<DateTime> StartDate = new("start-date")
    {
        Description = "A date time to use as the lower bound for the full-database query",
        DefaultValueFactory = _ => DateTime.MinValue
    };
    
    public static readonly Argument<DateTime> EndDate = new("end-date")
    {
        Description = "A date time to use as the upper bound for the full-database query",
        DefaultValueFactory = _ => DateTime.MaxValue
    };
    
    public static readonly ICmd Command = Cmd.New("clean", "Run to query a database and take action on resolving duplicate line items")
        .AddOption(Shared.DbString)
        .AddOption(Shared.SetDb)
        .AddArgument(StartDate)
        .AddArgument(EndDate)
        .WithAction((dbString, setDb, startDate, endDate) => 
            CleanCommand.Run(dbString, startDate, endDate) >>
            Shared.maybeSetDbPath(setDb, dbString) * Prelude.ignore);

}