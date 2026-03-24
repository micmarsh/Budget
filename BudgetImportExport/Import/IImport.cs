using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Import;

public interface IImport<T> : IDisposable
{
    Unit Write(T doc);
}