using LanguageExt;
using LiteDB;

namespace BudgetClassifier.Services.Storage.LiteDB;

public readonly record struct ClassificationDoc(ObjectId Id, DateTime DateTime, Classification Record);