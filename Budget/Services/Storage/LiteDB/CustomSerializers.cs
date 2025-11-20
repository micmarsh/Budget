using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public static class CustomSerializers
{
    public static Func<Seq<A>, BsonValue> serializeSeq<A>(BsonMapper mapper) => seq =>
        new BsonArray(seq.Map(mapper.Serialize));

    public static Func<BsonValue, Seq<A>> deserializeSeq<A>(BsonMapper mapper) => bson =>
        toSeq(bson.AsArray.Select(mapper.Deserialize<A>));

    public static Func<Option<A>, BsonValue> serializeOption<A>(BsonMapper mapper) => opt =>
        opt.Match(mapper.Serialize, () => BsonValue.Null);

    public static Func<BsonValue, Option<A>> deserializeOption<A>(BsonMapper mapper) => bson =>
        bson.IsNull ? None : Some(mapper.Deserialize<A>(bson));
}