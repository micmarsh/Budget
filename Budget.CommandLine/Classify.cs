using Budget.Services.Storage.LiteDB;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Traits;
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
        var storage = new LiteDb(dbString.Name, ObjectId.NewObjectId);
        return (
            from categories in storage.GetAllCategories().Collect()
            from unclassifed in GetAllUnCategorized(storage).Collect()
            from _1 in UserClassification.classifyAll(categories, unclassifed).RunIO(CreateRT(storage))
            select unit
        ).Finally(IO.lift(storage.Dispose));
    }

    private static Runtime<ObjectId> CreateRT(LiteDb storage) => new(storage, new Console(), storage);

    private static Source<(ObjectId Id, LineItem lineItem)> GetAllUnCategorized(IClassificationQuery<ObjectId> storage) =>
        +storage.GetDateRange(DateTime.MinValue, DateTime.MaxValue)
            .Choose(r => r.Record switch
            {
                UnCategorized { LineItem: var lineItem } => Some((r.Id, lineItem)),
                _ => None
            });
}