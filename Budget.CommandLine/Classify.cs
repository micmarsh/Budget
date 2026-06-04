using Budget.Services.Storage.LiteDB;
using CommandLine.Immutable;
using ConsoleApps;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using static LanguageExt.Prelude;
using LiteDb = Budget.Services.Storage.LiteDB.LiteDb;

namespace Budget.CommandLine;

public static class Classify
{
    
    private static readonly ICmd AutoClassifyCommand = Cmd
        .New("auto", "Run the auto-classifier (populated during manual classification) on all unclassified line items in database")
        .AddOption(Shared.DbString)
        .AddOption(Shared.SetDb)
        .WithAction((dbString, setDb) =>
            RunAutoClassify(dbString) >>
            Shared.maybeSetDbPath(setDb, dbString) * ignore);

    private static IO<Unit> RunAutoClassify(FileInfo dbString) =>
        RunClassify(dbString, (unclassified, storage) =>
            from lookupResults in autoClassifyAll(storage, unclassified)
            let foundCategories = lookupResults.Somes()
            from _1 in storage.Save(foundCategories)
            from _2 in Prompt.logIO($"Auto-classified {foundCategories.Count} out of {unclassified.Count} line items.")
            select unit);

    private static IO<Seq<Option<(ObjectId Id, Classification)>>> autoClassifyAll(LiteDb storage, Seq<(ObjectId Id, LineItem LineItem)> unclassified) =>
        unclassified.Traverse(pair => storage.Lookup(pair.LineItem.Description)
            .Map(opt => opt.Map(c => (pair.Id, (Classification) new Categorized(c, pair.LineItem)))))
            .As();

    public static readonly ICmd Command = Cmd
        .New("classify", "Read unclassified (usually newly-'import'ed) line items and classify them. Use 'auto' subcommand to run auto-classifier")
        .AddSub(AutoClassifyCommand)
        .AddOption(Shared.DbString)
        .AddOption(Shared.SetDb)
        .WithAction((dbString, setDb) =>
            RunManualClassify(dbString) >>
            Shared.maybeSetDbPath(setDb, dbString) * ignore);

    private static IO<Unit> RunManualClassify(FileInfo dbString) =>
        RunClassify(dbString, (unclassified, storage) =>
            from categories in storage.GetAllCategories().Collect()
            from _1 in UserClassification.classifyAll(categories, unclassified).RunIO(CreateRT(storage))
            select unit);
    
    private static IO<Unit> RunClassify(FileInfo dbString, 
        Func<Seq<(ObjectId Id, LineItem LineItem)>, LiteDb, IO<Unit>> classify)
    {
        var storage = new LiteDb(dbString, ObjectId.NewObjectId);
        return (
                from _ in Prompt.logIO($"Looking up unclassified line items in {dbString}")
                from unclassifed in GetAllUnCategorized(storage).Collect()
                from _0 in guard(unclassifed.Count > 0, Error.New(EarlyReturnNoUnclassifed, ""))
                from _1 in classify(unclassifed, storage)
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