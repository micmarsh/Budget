using LanguageExt;
using LiteDB;

namespace Budget;

public readonly record struct Runtime<DbId>(IStorage<DbId> Storage, IConsole Console, IAutoClassifier AutoClassifier)
    : IHasConsole, IHasAutoClassifier;

public interface IHasAutoClassifier
{
    IAutoClassifier AutoClassifier { get; }
}

public interface IConsole
{
    IO<string> ReadLine();
    IO<Unit> WriteLine(string message);
}

public interface IHasConsole
{
    IConsole Console { get; }
}

//todo rename and re-tool this completely for non-"latest-based" user classification
public interface IStorage<DbId>
{
    IO<Unit> Save(DbId id, Classification classified);
   // querying for all is for later!
   
    IO<Unit> Delete(Seq<DbId> deleteIds);
}

public interface IHasStorage<DbId>
{
    IStorage<DbId> Storage { get; }
}

public interface IClassificationQuery<DbId>
{
    public Source<QueryResult<DbId>> GetDateRange(DateTime start, DateTime end);
    public IO<Seq<CategorySelectOption>> GetAllCategories();
}

public readonly record struct QueryResult<UniqueId>(UniqueId Id, Classification Record);

public interface IAutoClassifier
{
    IO<Unit> Save(string description, Category category);
    IO<Option<Category>> Lookup(string description);
}

public interface IFileReads
{
    IO<string> GetFileText(string filePath);
}



