using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetMigration;

public static class LiteDbUtils
{
    public static Iterator<FlatClassification> ConvertToRows(ClassificationDoc doc) =>
        doc.Record switch
        {
            Categorized({ Value: var category }, var (desc, amount, date)) =>
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, category, desc, amount)),
            UnCategorized(var (desc, amount, date)) => 
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, Prelude.None, desc, amount)),
            SubClassifications(var children, var (desc, _, date)) =>
                Iterator.from(children.Map(c =>
                    new FlatClassification(doc.Id.ToString(), date, c.Category.Value, desc, c.Amount))),
            _ => throw Utilities.patternMatchError(doc.Record)
        };
}