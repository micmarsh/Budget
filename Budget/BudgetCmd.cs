using LanguageExt;

namespace Budget;

public record Runtime(IGetLines GetLines, IStorage Storage, IConsole Console);

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

public interface IGetLines
{
    IO<Seq<LineItem>> GetLines();
}



