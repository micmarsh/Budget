using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace Budget.Migration.Import;

public interface IBulkImport : IDisposable
{
    IO<Unit> WriteAll(Seq<FlatClassification> items);
}