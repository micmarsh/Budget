using System.Text;
using LanguageExt;
using LanguageExt.Traits;

using static LanguageExt.Prelude;

namespace Budget;

public static class Utilities
{
    public static Eff<Sub, Sub> askE<Sub>() => new (
        new ReaderT<Sub, IO, Sub>(IO.pure)
    );
    
    // todo this is a whole project to merge back into main as proper HKT
    public static Eff<Env2, A> CoMap<Env1, Env2, A>(this Eff<Env1, A> eff, Func<Env2, Env1> f) =>
        new (new ReaderT<Env2, IO, A>(env2 => eff.effect.Run(f(env2)) ));
    
    //todo based version where everything is monad, overlaods wrap up in Pure as needed, then can go in main too!
    //tricky problems found with not everything being lazy (attempting to "guard" Trues with "bad" Try methods, for example)
    //   Another example: Doesn't work to "guard" an index lookup (such as in Default value), for obvious reasons if you think about it
    public static K<M, A> cond<M, A>(Seq<(bool Pred, K<M, A> True)> seq, A Default)
        where M : Monad<M>
        => cond(seq, M.Pure(Default));
    
    public static K<M, A> cond<M, A>(Seq<(bool Pred, K<M, A> True)> seq, K<M, A> Default)
        where M : Monad<M>
        => seq.Rev().Fold(Default, (prev, nextIf) => iff(
            nextIf.Pred,
            nextIf.True,
            prev
        ));
    
    // Similarly nothing to do with budget at all, but generally useful for C#? Doesn't even need LanguageExt dep!
    public static ArgumentException patternMatchError<Supertype>(Supertype unmatchable, string? paramName = null) =>
        new ($"Unknown case type {unmatchable.GetType().Name} in" +
             $" pattern-match for {typeof(Supertype).Name}" +
             fileNameAndLine(), paramName);
    
    private static string fileNameAndLine()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        // 0 is fileNameAndLine frame, 1 is patternMatchError, 2 is where this is used?
        var matchFrame = stackTrace.GetFrame(2);
        if (matchFrame == null || matchFrame.GetFileName() == null)
        {
            return string.Empty;
        }
    
        return $" at {matchFrame.GetFileName()}:{matchFrame.GetFileLineNumber()}";
    }
}

public static class Csv
{
    public static IO<CsvFile> ParseFile(string filePath)
        => IO.lift(() => File.ReadAllText(filePath))
             .Map(ParseText)
             .Map(lines => new CsvFile(lines.Lines, lines.Header, filePath));

    public static CsvLines ParseText(string fileText)
        => fileText.Split(Environment.NewLine)
                   .Match(() => new CsvLines(Empty, Empty),
                          (head, tail) =>
                          {
                              var keys = toSeq(csvSplit(head));
                              return new CsvLines(tail.Map(CreateCsvLine(keys)), keys);
                          });
    
    private static Func<string, int, CsvLine> CreateCsvLine(Seq<string> keys) 
        => (lineString, linesIndex) =>
           {
               var values = csvSplit(lineString);
               var lineNumber = (uint) linesIndex + 2; // + 2 b/c line 1 should always be header, and index is of course 0-indexed
               var fields = keys.Zip(values).ToMap();
               if (keys.Length == values.Length)
               {
                   return new ValidCsvLine(fields, lineNumber);
               }

               if (values.Length > keys.Length)
               {
                   return new LongCsvLine(fields,  values.Skip(keys.Length), lineNumber);
               }
               // else value.Length < keys.Length
               return new ShortCsvLine(fields, keys.Skip(values.Length), lineNumber);
           };

    //todo needs actual perf testing! also perf testing vs. plain list!? This is why premature optimization is no good
    private static Seq<string> csvSplit(string line)
    {
        var result  = new LinkedList<string>();
        var quoted  = false;
        var builder = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                quoted = !quoted;
                builder.Append(line[i]);
                continue;
            }

            if (line[i] == ',' && !quoted)
            {
                result.AddLast(builder.ToString());
                builder = new StringBuilder();
                continue;
            }
            
            if (line[i] == ',')// && quoted)
            {
                builder.Append(line[i]);
                continue;
            }
            
            if (line[i] == '\n' || line[i] == 'r')
            {
                continue;
            }

            // else is just regular character
            builder.Append(line[i]);
        }
        //todo remove newline at end?
        result.AddLast(builder.ToString());
        return toSeq(result);
    }
}

public abstract record CsvLine(Map<string, string> Fields, uint LineNumber);

public sealed record ValidCsvLine(Map<string, string> Fields, uint LineNumber) : CsvLine(Fields, LineNumber);
public sealed record ShortCsvLine(Map<string, string> Fields, Seq<string> MissingFields, uint LineNumber)
    : CsvLine(Fields, LineNumber);
public sealed record LongCsvLine(Map<string, string> Fields, Seq<string> ExtraValues, uint LineNumber)
    : CsvLine(Fields, LineNumber);

public record CsvLines(Seq<CsvLine> Lines, Seq<string> Header);
public record CsvFile(Seq<CsvLine> Lines, Seq<string> Header, string FileName) : CsvLines(Lines, Header);
