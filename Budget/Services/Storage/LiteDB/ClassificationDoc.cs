using LanguageExt;
using LiteDB;

namespace Budget.Services.Storage.LiteDB;

public readonly record struct ClassificationDoc(ObjectId Id, DateTime DateTime, Classification Record);