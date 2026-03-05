namespace GameHub.Domain.Chats;

public class MessagePreviewService
{
    public string CreatePreview(string content, int maxPreviewLength)
    {
        var normalized = content
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();

        normalized = CollapseSpaces(normalized);

        return normalized.Length <= maxPreviewLength
            ? normalized
            : normalized[..maxPreviewLength];
    }

    private string CollapseSpaces(string input)
    {
        if (!input.Contains("  ", StringComparison.Ordinal))
            return input;

        var sb = new System.Text.StringBuilder(input.Length);
        bool prevSpace = false;

        foreach (var ch in input)
        {
            var isSpace = ch == ' ';
            if (isSpace)
            {
                if (prevSpace) continue;
                prevSpace = true;
                sb.Append(' ');
            }
            else
            {
                prevSpace = false;
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
