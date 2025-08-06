namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Helpers;

public static class StringExtensions
{
    public static string ToTitleCase(this string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        var words = source.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }

        return string.Join(" ", words);
    }
}