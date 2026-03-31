using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace Budget.Migration.Export;

public interface IExport : IDisposable
{
    Source<FlatClassification> ExportClassifications();
}