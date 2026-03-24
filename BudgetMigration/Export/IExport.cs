using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetMigration.Export;

public interface IExport : IDisposable
{
    Iterator<FlatClassification> ExportClassifications();
}