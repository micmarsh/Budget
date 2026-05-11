using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.FileImport;

public static class CleanCommand
{
    public static IO<Unit> Run(FileInfo dbString, DateTime startRange, DateTime endRange)
    {
        var storage = new LiteDb(dbString.Name, ObjectId.NewObjectId);
        return 
            from groups in lookupAndGroup(startRange, endRange, storage)
            let message = duplicatesMessage(groups)
            // prompt everything
            // read instruction, either 
               // do delete and re-run clean (so can see results)
               // do nothing so can exit
            // look into old stuff and see what we can re-use!
            from _1 in log(message)
            select unit;
    }

    private static IO<HashMap<LineItem, Seq<QueryResult<ObjectId>>>> lookupAndGroup(DateTime startRange, DateTime endRange, LiteDb storage) =>
        storage.GetDateRange(startRange, endRange)
            .Reduce(HashMap<LineItem, Seq<QueryResult<ObjectId>>>(), (groups, c) =>
                groups.AddOrUpdate(c.Record.LineItem, cs => cs.Add(c), Seq(c))
            );

    //todo utilize some nice, re-usable method like instead of this internal thing (there's currently a couple in "User Classification")
    // also need an error or warning version of this, does/could that exist in CommandLine LanguageExt library?
    private static IO<Unit> log(object? obj) => IO.lift(() => System.Console.WriteLine(obj));
    
    private static string duplicatesMessage(HashMap<LineItem, Seq<QueryResult<ObjectId>>> groups)
    {
        var onlyDuplicates = groups.Filter(cs => cs.Count > 1);
        return onlyDuplicates.IsEmpty
            ? "Found no duplicate entries in db after import"
            : $"Found duplicate entries in db {Environment.NewLine}{string.Join(Environment.NewLine,
                onlyDuplicates.AsIterable()
                    .Map(kv => $"{kv.Value.Count} for {kv.Key.Description}: {kv.Key.Amount:C} on {kv.Key.Date:D}") // template copied from UserClassificaiton.cs, should consolidate?
            )}";
    }

    private const string Prompt = "What do you want to do to resolve the duplicates?" +
                                  "    1) Delete unclassified " +
                                  "    2) Do nothing (exit)";
}