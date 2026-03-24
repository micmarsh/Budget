using LanguageExt;
using LanguageExt.Common;

namespace BudgetClassifier;

public record Runtime(IFileReads FileReads, IStorage Storage, IConsole Console, IAutoClassifier AutoClassifier)
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

public interface IStorage
{
    IO<ClassificationsState> GetLatest();
    IO<Unit> Save(Classification classified);
   // querying for all is for later!
}

public interface IAutoClassifier
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



