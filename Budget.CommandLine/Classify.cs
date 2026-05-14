using Budget.Services.Storage.LiteDB;
using CommandLine.Immutable;
using ConsoleApps;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.CommandLine;

public static class Classify
{
    public static readonly ICmd Command = Cmd
        .New("classify", "Read unclassified (usually newly-'import'ed) line items and classify them manually")
        .AddOption(Shared.DbString)
        .AddOption(Shared.SetDb)
        .WithAction((dbString, setDb) =>
            RunClassify(dbString) >>
            Shared.maybeSetDbPath(setDb, dbString) * ignore);

    private static IO<Unit> RunClassify(FileInfo dbString)
    {
        System.Console.WriteLine($"Looking up stuff in {dbString}");
        var storage = new LiteDb(dbString, ObjectId.NewObjectId);
        return (
            from unclassifed in GetAllUnCategorized(storage).Collect()
            from _0 in guard(unclassifed.Count > 0, Error.New(EarlyReturnNoUnclassifed, ""))
            from categories in storage.GetAllCategories().Collect()
            from _1 in UserClassification.classifyAll(categories, unclassifed).RunIO(CreateRT(storage))
            select unit
        )
        .Catch(EarlyReturnNoUnclassifed, _ => Prompt.logIO($"Found no unclassified line items in {dbString}")).As()
        .Finally(IO.lift(storage.Dispose));
    }

    private const int EarlyReturnNoUnclassifed = 2413;

    private static Runtime<ObjectId> CreateRT(LiteDb storage) => new(storage, Console.Default, storage);

    private static Source<(ObjectId Id, LineItem lineItem)> GetAllUnCategorized(IClassificationQuery<ObjectId> storage) =>
        +storage.GetDateRange(DateTime.MinValue, DateTime.MaxValue)
            .Choose(r => r.Record switch
            {
                UnCategorized { LineItem: var lineItem } => Some((r.Id, lineItem)),
                _ => None
            });
}