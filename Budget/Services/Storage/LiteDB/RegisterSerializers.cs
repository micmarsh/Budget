using LiteDB;
using static Budget.Services.Storage.LiteDB.CustomSerializers;

namespace Budget.Services.Storage.LiteDB;

public static class RegisterSerializers
{
    public static void Register()
    {
        var mapper = BsonMapper.Global;
        mapper.RegisterType(serializeSeq<SubCategorized>(mapper), deserializeSeq<SubCategorized>(mapper));
        mapper.RegisterType(
            serialize: c => new BsonDocument
            {
                ["_id"] = $"{c.Category.Value}|{c.IsIncome}" ,
                ["isIncome"] = c.IsIncome
            },
            deserialize: doc => new CategorySelectOption(
                new Category(doc["_id"].AsString.Split('|')[0]), 
                doc["isIncome"].AsBoolean)
        );
    }
}