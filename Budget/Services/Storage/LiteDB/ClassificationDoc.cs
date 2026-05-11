using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public readonly record struct ClassificationDoc(ObjectId Id, Classification Record, Seq<History> History)
{
    public static ClassificationDoc NewAdd(ObjectId objectid, DateTime now, Classification classified)
    {
        var categorySelectOptions = CategorySelectOption.Create(classified);
        return new ClassificationDoc(objectid, classified, Seq<History>(new Added(now))
            .Concat(categorySelectOptions.Map(opt => new Classified(opt.Category, now))));
    }
    
    public static ClassificationDoc NewClassify(ObjectId objectid, DateTime now, Classification classified)
    {
        var categorySelectOptions = CategorySelectOption.Create(classified);
        return new ClassificationDoc(objectid, classified, Seq<History>(new Added(now))
            .Concat(categorySelectOptions.Map(opt => new Classified(opt.Category, now))));
    }
};

public abstract record History(DateTime DateTime);
public sealed record Added(DateTime DateTime) : History(DateTime);
public sealed record Classified(Category Category, DateTime DateTime) : History(DateTime);