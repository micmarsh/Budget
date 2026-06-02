using Budget.Services.Storage.LiteDB;
using ConsoleApps;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using static LanguageExt.Prelude;
using static ConsoleApps.Prompt;
using static Budget.Utilities;

namespace Budget.FileImport;

public static class CleanCommand
{
    private readonly record struct CleanRT(IConsole Console, IStorage<ObjectId> Storage) 
        : IHasConsole, IHasStorage<ObjectId>;
    
    public static IO<Unit> Run(FileInfo dbString, DateTime startRange, DateTime endRange)
    {
        var storage = new LiteDb(dbString, ObjectId.NewObjectId);
        return (
            from groups in lookupAndGroup(startRange, endRange) * (g => g.Filter(cs => cs.Count > 1))
            from _0 in guard(groups.Count > 0, Error.New(EarlyExitNoDuplicates, ""))
            let message = duplicatesMessage(groups)
            from _1 in logCleanPrompt(message)
            from selection in readValue<CleanRT, int>(parseBetween1And(2), "Please enter a selection number, 1 or 2")
            from _2 in runSelection<CleanRT>(selection, groups)
            select unit
        )
        .Catch(EarlyExitNoDuplicates, _ => log<CleanRT>("Found no duplicate entries in db"))
        .RunIO(new CleanRT(Console.Default, storage))
        .Finally(IO.lift(storage.Dispose));
    }

    private static Eff<CleanRT, Unit> logCleanPrompt(string message) => 
        log<CleanRT>($"{message}{Environment.NewLine + Environment.NewLine}{DuplicateActionPrompt}");

    private static Eff<RT, Unit> runSelection<RT>(int cleanUpSelection, HashMap<LineItem, Seq<QueryResult<ObjectId>>> duplicateGroups) 
        where RT: IHasStorage<ObjectId>, IHasConsole =>
        cleanUpSelection switch
        {
            1 => deleteUnCategorized<RT>(duplicateGroups.Values.ToSeq().Flatten()),
            2 => log<RT>("Exiting without cleaning database"),
            _ => throw new ArgumentOutOfRangeException(nameof(cleanUpSelection), cleanUpSelection, null)
        };

    private static Eff<RT, Unit> deleteUnCategorized<RT>(Seq<QueryResult<ObjectId>> duplicateGroupsValues)
        where RT : IHasStorage<ObjectId>, IHasConsole =>
            from rt in askE<RT>()
            from deleted in rt.Storage.Delete(duplicateGroupsValues
                .Choose(r => r.Record switch
                {
                    UnCategorized => Some(r.Id),
                    _ => None
                }))
            from _1 in log<RT>($"Deleted {deleted} uncategorized duplicate entries from db, please run 'clean' command again to verify results")
            select unit;

    private static Eff<CleanRT, HashMap<LineItem, Seq<QueryResult<ObjectId>>>> lookupAndGroup(DateTime startRange, DateTime endRange) =>
        askE<CleanRT>() >>
        (rt =>
            rt.Storage.GetDateRange(startRange, endRange)
                .Reduce(HashMap<LineItem, Seq<QueryResult<ObjectId>>>(), (groups, c) =>
                    groups.AddOrUpdate(c.Record.LineItem, cs => cs.Add(c), Seq(c))
                ));
    
    private static string duplicatesMessage(HashMap<LineItem, Seq<QueryResult<ObjectId>>> onlyDuplicates) =>
        $"Found duplicate entries in db {Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine,
                onlyDuplicates.AsIterable()
                    .OrderBy(kv => kv.Key.Date)
                    .Select(kv => duplicateInfoLine(kv.Key, kv.Value))
            )}";

    private static string duplicateInfoLine(LineItem lineItem, Seq<QueryResult<ObjectId>> duplicates)
    {
        var objects = duplicates.Map(q => q.Record);
        var unClassified = objects.OfType<UnCategorized>().Count();
        return $"{objects.Count} for {lineItem.Description}: {lineItem.Amount:C} on {lineItem.Date:D} " + // template copied from UserClassificaiton.cs, should consolidate?
               $"{Environment.NewLine}({objects.Count - unClassified} classified, {unClassified} un-classified)"; 
    }

    public static readonly string DuplicateActionPrompt = "What do you want to do to resolve the duplicates?" + Environment.NewLine +
                                                          "    1) Delete unclassified " + Environment.NewLine +
                                                          "    2) Do nothing (exit)" + Environment.NewLine;

    private const int EarlyExitNoDuplicates = 8987;
}