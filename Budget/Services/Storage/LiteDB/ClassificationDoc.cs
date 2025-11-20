using LanguageExt;
using LiteDB;

namespace Budget.Services.Storage.LiteDB;

public record ClassificationDoc(ObjectId Id, LineItem LineItem, Seq<Category> Categories, Classification Record);