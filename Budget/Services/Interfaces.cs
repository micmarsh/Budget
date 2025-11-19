using LanguageExt;
using LanguageExt.Common;

namespace Budget;

public record Runtime(IFileReads FileReads, IStorage Storage, IConsole Console);

public interface IConsole
{
    IO<string> ReadLine();
    IO<Unit> WriteLine(string message);
}

public interface IStorage
{
    IO<ClassificationsState> GetLatest();
    IO<Unit> Save(Classification classified);
   // querying for all is for later!
}

public record ClassificationsState(
    DateTime Date,
    Seq<Category> Categories,
    Seq<Classification> OnDate);

public interface IFileReads
{
    IO<string> GetFileText(string filePath);
}



