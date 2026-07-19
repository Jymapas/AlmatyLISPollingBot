namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public static class PollResultsCallbackCodec
{
    public static string Encode(Guid pollSessionId, Guid optionId, long voterId, bool exclude) =>
        $"{ToToken(pollSessionId)}|{ToToken(optionId)}|{ToToken(voterId)}|{(exclude ? 1 : 0)}";

    public static bool TryDecode(string value, out Guid pollSessionId, out Guid optionId, out long voterId, out bool exclude)
    {
        pollSessionId = Guid.Empty;
        optionId = Guid.Empty;
        voterId = 0;
        exclude = false;
        var tokens = value.Split('|');
        return tokens.Length == 4
            && TryParseGuidToken(tokens[0], out pollSessionId)
            && TryParseGuidToken(tokens[1], out optionId)
            && TryParseLongToken(tokens[2], out voterId)
            && TryParseAction(tokens[3], out exclude);
    }

    public static string EncodeOption(Guid pollSessionId, Guid optionId) => $"{ToToken(pollSessionId)}|{ToToken(optionId)}";

    public static bool TryDecodeOption(string value, out Guid pollSessionId, out Guid optionId)
    {
        pollSessionId = Guid.Empty;
        optionId = Guid.Empty;
        var tokens = value.Split('|');
        return tokens.Length == 2 && TryParseGuidToken(tokens[0], out pollSessionId) && TryParseGuidToken(tokens[1], out optionId);
    }

    private static string ToToken(Guid value) => Convert.ToBase64String(value.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ToToken(long value) => Convert.ToBase64String(BitConverter.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryParseGuidToken(string value, out Guid result)
    {
        result = Guid.Empty;
        if (value.Length != 22) return false;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "==");
            if (bytes.Length != 16) return false;
            result = new Guid(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryParseLongToken(string value, out long result)
    {
        result = 0;
        if (value.Length != 11) return false;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
            if (bytes.Length != sizeof(long)) return false;
            result = BitConverter.ToInt64(bytes, 0);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryParseAction(string value, out bool exclude)
    {
        exclude = value == "1";
        return exclude || value == "0";
    }
}
