using LanguageExt;

namespace Budget.Migration;

public readonly record struct FlatClassification(string DbId, DateTime Date, Option<string> Category, string Description, decimal Amount, string History)
{
    public readonly static string CsvHeader =
        $"{nameof(DbId)},{nameof(Date)},{nameof(Description)},{nameof(Category)},{nameof(Amount)},{nameof(History)}";
    
    public override string ToString() => $"{DbId},\"{Date}\",\"{Description}\",\"{Category.IfNone(string.Empty)}\",{Amount},\"{History}\"";
}