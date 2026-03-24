using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Import;

public interface IBulkImport
{
    Unit WriteAll(Seq<FlatClassification> items);
}