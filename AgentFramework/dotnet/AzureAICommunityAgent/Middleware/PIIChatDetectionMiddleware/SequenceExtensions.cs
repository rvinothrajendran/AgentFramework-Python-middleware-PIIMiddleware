using System.Text.RegularExpressions;
using CreditCardValidator;
using Microsoft.Recognizers.Text;

namespace AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware;

public static class SequenceExtensions
{
    private static readonly Regex CardRegex =
        new(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled);

    public static IEnumerable<ModelResult> RecognizeCreditCard(string text)
    {
        var results = new List<ModelResult>();

        foreach (Match match in CardRegex.Matches(text))
        {
            var detector = new CreditCardDetector(match.Value);

            if (!detector.IsValid())
                continue;

            results.Add(new ModelResult
            {
                Text = match.Value,
                TypeName = "creditcard",
                Start = match.Index,
                End = match.Index + match.Length - 1,
                Resolution = new SortedDictionary<string, object>
                {
                    { "value", match.Value },
                    { "issuer", detector.Brand.ToString() },
                    //{ "score", 0.95 }
                }
            });
        }

        return results;
    }
}