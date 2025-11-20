using LanguageExt;

namespace Budget;

public class FileReads : IFileReads
{
    private readonly AtomHashMap<string, string> Cache = Prelude.AtomHashMap(new HashMap<string, string>());
    
    public IO<string> GetFileText(string filePath) => 
        Cache.Find(filePath, IO.pure, 
            () => IO.lift(() =>
            {
                var fileText = File.ReadAllText(filePath);
                Cache.Swap(map => map.Add(filePath, fileText));
                return fileText;
            }));
}