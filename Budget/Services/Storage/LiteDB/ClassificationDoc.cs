using LanguageExt;
using LiteDB;

namespace Budget.Services.Storage.LiteDB;

public readonly record struct ClassificationDoc(ObjectId Id, Classification Record, Seq<History> History);

public abstract record History(DateTime DateTime);
public sealed record Added(DateTime DateTime) : History(DateTime);
public sealed record Classified(Category Category, DateTime DateTime) : History(DateTime);