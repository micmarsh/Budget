using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetMigration.Export;

public interface IExport : IDisposable
{
    Source<FlatClassification> ExportClassifications();
}