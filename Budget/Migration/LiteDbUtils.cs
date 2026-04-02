using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace Budget.Migration;

public static class LiteDbUtils
{
    public static Iterator<FlatClassification> ConvertToRows(ClassificationDoc doc) =>
        doc.Record switch
        {
            Categorized({ Value: var category }, var (desc, amount, date)) =>
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, category, desc, amount, EncodeHistory(doc.History))),
            UnCategorized(var (desc, amount, date)) => 
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, Prelude.None, desc, amount, EncodeHistory(doc.History))),
            SubClassifications(var children, var (desc, _, date)) =>
                Iterator.from(children.Map(c =>
                    new FlatClassification(doc.Id.ToString(), date, c.Category.Value, desc, c.Amount, EncodeHistory(doc.History)))),
            _ => throw Utilities.patternMatchError(doc.Record)
        };

    /// <summary>
    /// Doesn't add quotes '\"' to result, assumes final output to CSV will
    /// </summary>
    public static string EncodeHistory(Seq<History> history) => string.Join(",",
        history.Map(h => h switch
        {
            Added (var date) => $"A+{ToMinutesFromEpoch(date)}",
            Classified (var cat, var date) => $"C+{cat.Value}+{ToMinutesFromEpoch(date)}",
            _ => throw Utilities.patternMatchError(h)
        }));

    private static long ToMinutesFromEpoch(DateTime date) => (long)(date - DateTime.UnixEpoch).TotalMinutes;

    public static Seq<History> DecodeHistory(string history) =>
        history.Split(",").AsIterable()
            .Map(str => str.Split('+'))
            .Map(History (str) => str[0] switch
            {
                "A" => new Added(FromMinutesFromEpoch(str[1])),
                "C" => new Classified(new Category(str[1]), FromMinutesFromEpoch(str[2])),
                _ => throw new ArgumentException($"Unknown letter code for history decoding {str[0]}")
            })
            .ToSeq();

    private static DateTime FromMinutesFromEpoch(string str) => 
        DateTime.UnixEpoch + TimeSpan.FromMinutes(long.Parse(str));
}