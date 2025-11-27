using LanguageExt;
using LanguageExt.Common;

namespace Budget;

public record Runtime(IFileReads FileReads, IStorage Storage, IConsole Console, IAutoClassifierStorage AutoClassifierStorage)
    : IHasConsole, IHasAutoClassifierStorage;

public interface IHasAutoClassifierStorage
{
    IAutoClassifierStorage AutoClassifierStorage { get; }
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

public interface IStorage
{
    IO<ClassificationsState> GetLatest();
    IO<Unit> Save(Classification classified);
   // querying for all is for later!
}

public interface IAutoClassifierStorage
{
    IO<Unit> Save(string description, Category category);
    IO<Option<Category>> Lookup(string description);
}

public record ClassificationsState(
    DateTime Date,
    Seq<CategorySelectOption> Categories,
    Set<Classification> OnDate);

public interface IFileReads
{
    IO<string> GetFileText(string filePath);
}



