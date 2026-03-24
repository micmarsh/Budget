using BudgetClassifier.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Export;

public interface IExport : IDisposable
{
    Iterator<ClassificationDoc> ExportClassifications();
}