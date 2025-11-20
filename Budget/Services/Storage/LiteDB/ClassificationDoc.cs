using LanguageExt;
using LiteDB;

namespace Budget.Services.Storage.LiteDB;

public record ClassificationDoc(ObjectId Id, DateTime DateTime, Classification Record);