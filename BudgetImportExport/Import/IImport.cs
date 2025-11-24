using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Import;

public interface IImport : IDisposable
{
    Unit Write(ClassificationDoc doc);
}