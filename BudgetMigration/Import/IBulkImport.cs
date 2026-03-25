using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetMigration.Import;

public interface IBulkImport
{
    IO<Unit> WriteAll(Seq<FlatClassification> items);
}