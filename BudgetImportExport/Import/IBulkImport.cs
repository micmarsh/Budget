using LanguageExt;

namespace BudgetImportExport.Import;

public interface IBulkImport<T>
{
    Unit WriteAll(Seq<T> items);
}