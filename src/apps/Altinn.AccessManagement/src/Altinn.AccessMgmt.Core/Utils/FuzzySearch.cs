using System.Linq.Expressions;
using Altinn.AccessMgmt.Core.Utils.Models;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Provides fuzzy search functionality using Jaro-Winkler, Levenshtein, and Trigram similarity.
/// Supports configurable weightings and fuzziness levels for improved relevance.
/// </summary>
public static class FuzzySearch
{
    /// <summary>
    /// The weight assigned to Jaro-Winkler similarity in the final match score.
    /// Jaro-Winkler performs well on short words and minor typos.
    /// </summary>
    private const double JaroWeight = 0.4;

    /// <summary>
    /// The weight assigned to Levenshtein distance in the final match score.
    /// Levenshtein is effective for detecting character edits such as insertions, deletions, and substitutions.
    /// </summary>
    private const double LevenshteinWeight = 0.4;

    /// <summary>
    /// The weight assigned to Trigram similarity in the final match score.
    /// Trigram matching helps detect partial word matches and is useful for longer text.
    /// </summary>
    private const double TrigramWeight = 0.2;

    /// <summary>
    /// Defines the fuzzy matching settings for each fuzziness level.
    /// Includes the threshold for determining a match and the maximum Levenshtein distance allowed.
    /// </summary>
    private static readonly Dictionary<FuzzynessLevel, (double Threshold, int MaxDistance)> FuzzynessSettings = new()
    {
        { FuzzynessLevel.High, (0.55, 5) }, // High tolerance, allows up to 5 character differences
        { FuzzynessLevel.Medium, (0.75, 3) }, // Medium tolerance, allows up to 3 character differences
        { FuzzynessLevel.Low, (0.85, 1) } // Low tolerance, requires near-exact match
    };

    /// <summary>
    /// Performs a fuzzy search on a collection of objects, scoring matches based on multiple similarity algorithms.
    /// </summary>
    /// <typeparam name="T">The type of objects being searched.</typeparam>
    /// <param name="data">The list of objects to search through.</param>
    /// <param name="term">The search term to match against.</param>
    /// <param name="builder">A configured <see cref="SearchPropertyBuilder{T}"/> defining the searchable fields.</param>
    /// <returns>
    /// A list of <see cref="SearchObject{T}"/> containing objects that matched the search term,
    /// ranked by their calculated match score.
    /// </returns>
    public static List<SearchObject<T>> PerformFuzzySearch<T>(List<T> data, string term, SearchPropertyBuilder<T> builder)
    {
        List<SearchObject<T>> results = new();
        if (string.IsNullOrEmpty(term))
        {
            return results;
        }

        var properties = builder.Build();
        var searchTerms = term.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var obj in data)
        {
            double totalScore = 0;
            double totalWeight = 0;
            int matchedTerms = 0;
            List<SearchField> fieldHits = new();

            foreach (var (propertyName, (selector, weight, fuzzyness)) in properties)
            {
                var (fieldThreshold, fieldMaxDistance) = FuzzynessSettings[fuzzyness];

                var valueObj = selector(obj) ?? string.Empty;
                string value = valueObj.ToString();
                List<SearchWord> words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => new SearchWord() { Content = word, LowercaseContent = word.ToLower(), IsMatch = false, Score = 0 }).ToList();
                double fieldScore = 0;

                foreach (var searchTerm in searchTerms)
                {
                    bool termMatched = false;
                    foreach (var word in words)
                    {
                        double jaroScore = JaroWinklerSimilarity(searchTerm, word.LowercaseContent);
                        double levenshteinScore = 1.0 - (LevenshteinDistance(searchTerm, word.LowercaseContent, fieldMaxDistance) / (double)Math.Max(searchTerm.Length, word.LowercaseContent.Length));
                        double trigramScore = TrigramSimilarity(searchTerm, word.LowercaseContent);

                        // Kombinert score med vekting
                        double finalScore = (jaroScore * JaroWeight) + (levenshteinScore * LevenshteinWeight) + (trigramScore * TrigramWeight);
                        bool isMatch = finalScore >= fieldThreshold;

                        if (isMatch)
                        {
                            word.IsMatch = true;
                            word.Score += finalScore;

                            fieldScore += finalScore;
                            termMatched = true;
                        }
                    }

                    if (termMatched)
                    {
                        matchedTerms++;
                    }
                }

                if (words.Any(t => t.IsMatch))
                {
                    double weightedFieldScore = fieldScore * weight;
                    fieldHits.Add(new SearchField
                    {
                        Field = propertyName,
                        Value = value,
                        Score = weightedFieldScore,
                        Words = words
                    });

                    totalScore += weightedFieldScore;
                    totalWeight += weight;
                }
            }

            if (fieldHits.Any())
            {
                double normalizedScore = totalWeight > 0 ? (totalScore / totalWeight) : totalScore; // Normaliserer scoren
                double matchFactor = matchedTerms / (double)searchTerms.Length; // Hvor stor andel av søkeordene som traff
                results.Add(new SearchObject<T>
                {
                    Object = obj,
                    Score = normalizedScore * matchFactor,
                    Fields = fieldHits
                });
            }
        }

        return results;
    }

    private static double JaroWinklerSimilarity(string s1, string s2)
    {
        if (s1 == s2)
        {
            return 1.0;
        }

        int matchDistance = (Math.Max(s1.Length, s2.Length) / 2) - 1;
        bool[] s1Matches = new bool[s1.Length];
        bool[] s2Matches = new bool[s2.Length];

        int matches = 0, transpositions = 0;

        for (int i = 0; i < s1.Length; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, s2.Length);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                {
                    continue;
                }

                s1Matches[i] = s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0.0;
        }

        int k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i])
            {
                continue;
            }

            while (!s2Matches[k])
            {
                k++;
            }

            if (s1[i] != s2[k])
            {
                transpositions++;
            }

            k++;
        }

        double jaro = ((matches / (double)s1.Length) +
                      (matches / (double)s2.Length) +
                      ((matches - (transpositions / 2.0)) / matches)) / 3.0;

        return jaro;
    }

    private static int LevenshteinDistance(string s1, string s2, int maxDistance)
    {
        if (s1.Length == 0)
        {
            return s2.Length;
        }

        if (s2.Length == 0)
        {
            return s1.Length;
        }

        int beyond = (maxDistance == int.MaxValue) ? int.MaxValue : maxDistance + 1;

        if (Math.Abs(s1.Length - s2.Length) > maxDistance)
        {
            return beyond;
        }

        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= s2.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= s1.Length; i++)
        {
            int minRowValue = int.MaxValue;

            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);

                minRowValue = Math.Min(minRowValue, d[i, j]);
            }

            if (minRowValue > maxDistance)
            {
                return beyond;
            }
        }

        return d[s1.Length, s2.Length];
    }

    private static double TrigramSimilarity(string s1, string s2)
    {
        var set1 = GenerateTrigrams(s1);
        var set2 = GenerateTrigrams(s2);

        int intersection = set1.Intersect(set2).Count();
        int union = set1.Count + set2.Count - intersection;

        return union == 0 ? 0 : ((double)intersection / union);
    }

    private static HashSet<string> GenerateTrigrams(string input)
    {
        HashSet<string> trigrams = new();
        if (input.Length < 3)
        {
            return trigrams;
        }

        for (int i = 0; i <= input.Length - 3; i++)
        {
            trigrams.Add(input.Substring(i, 3));
        }

        return trigrams;
    }
}

/// <summary>
/// Defines the level of fuzziness applied in search matching.
/// Determines the tolerance for inexact matches.
/// </summary>
public enum FuzzynessLevel
{
    /// <summary>
    /// High tolerance for typos and partial matches.
    /// Best for short fields like names where small differences should still result in a match.
    /// </summary>
    High,

    /// <summary>
    /// Medium tolerance for typos and partial matches.
    /// Used for moderately sized text fields like descriptions or categories.
    /// </summary>
    Medium,

    /// <summary>
    /// Low tolerance for typos and partial matches.
    /// Best for long text fields where accuracy is more important than flexibility.
    /// </summary>
    Low
}

/// <summary>
/// Builder for configuring property-based search criteria with fuzzy matching.
/// Allows defining weighted properties and handling collections dynamically.
/// </summary>
/// <typeparam name="T">The type of objects being searched.</typeparam>
public class SearchPropertyBuilder<T>
{
    private readonly Dictionary<string, (Func<T, object> Selector, double Weight, FuzzynessLevel Fuzzyness)> _properties = [];

    /// <summary>
    /// Adds a property to the search configuration.
    /// </summary>
    /// <param name="expression">An expression selecting the property from the object.</param>
    /// <param name="weight">The weight assigned to this property in search scoring.</param>
    /// <param name="fuzzyness">Defines the level of fuzziness applied in search matching</param>
    /// <returns>The current instance of <see cref="SearchPropertyBuilder{T}"/> for chaining.</returns>
    public SearchPropertyBuilder<T> Add(Expression<Func<T, object>> expression, double weight, FuzzynessLevel fuzzyness)
    {
        string propertyName = GetPropertyName(expression);
        _properties[propertyName] = (expression.Compile(), weight, fuzzyness);
        return this;
    }

    /// <summary>
    /// Adds a collection-based property to the search configuration.
    /// Supports both combined and detailed search modes.
    /// </summary>
    /// <typeparam name="TCollection">The type of elements in the collection.</typeparam>
    /// <param name="collectionSelector">An expression selecting the collection property from the object.</param>
    /// <param name="itemSelector">A function to extract the searchable string from each item in the collection.</param>
    /// <param name="weight">The weight assigned to this collection in search scoring.</param>
    /// <param name="fuzzyness">Defines the level of fuzziness applied in search matching</param>
    /// <param name="detailed">
    /// If <c>true</c>, treats each item in the collection as a separate searchable entity.
    /// If <c>false</c>, combines all items into a single searchable string.
    /// </param>
    /// <returns>The current instance of <see cref="SearchPropertyBuilder{T}"/> for chaining.</returns>
    public SearchPropertyBuilder<T> AddCollection<TCollection>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Func<TCollection, string> itemSelector,
        double weight,
        FuzzynessLevel fuzzyness,
        bool detailed = false)
    {
        string propertyName = GetPropertyName(collectionSelector);

        if (detailed)
        {
            // Each item is treated as a separate entity
            _properties[$"{propertyName} (Detailed)"] = (pkg =>
                string.Join(" | ", collectionSelector.Compile()(pkg).Select(itemSelector)), weight, fuzzyness);
        }
        else
        {
            // All items are combined into a single searchable string
            _properties[$"{propertyName} (Combined)"] = (pkg =>
                string.Join(", ", collectionSelector.Compile()(pkg).Select(itemSelector)), weight, fuzzyness);
        }

        return this;
    }

    /// <summary>
    /// Builds and returns the configured property dictionary for search operations.
    /// </summary>
    /// <returns>A dictionary mapping property names to their search configurations.</returns>
    public Dictionary<string, (Func<T, object> Callback, double A, FuzzynessLevel FuzzynessLevel)> Build()
    {
        return _properties;
    }

    /// <summary>
    /// Extracts the full property name from an expression, preserving nested properties.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being extracted.</typeparam>
    /// <param name="expression">An expression selecting a property from the object.</param>
    /// <returns>
    /// A string representing the full property path, with nested properties joined by an underscore.
    /// Example: "Area_Group_Name" for `pkg.Area.Group.Name`.
    /// Returns "UnknownProperty" if the expression type is not recognized.
    /// </returns>
    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return GetFullPropertyName(member);
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return GetFullPropertyName(unaryMember);
        }

        if (expression.Body is MethodCallExpression methodCall)
        {
            return methodCall.Method.Name;
        }

        return "UnknownProperty";
    }

    /// <summary>
    /// Recursively constructs the full property path from a nested MemberExpression.
    /// </summary>
    /// <param name="member">The member expression representing the property.</param>
    /// <returns>
    /// A string containing the full property path, with each level separated by an underscore.
    /// Example: "Area_Group_Name" for a nested property structure.
    /// </returns>
    private static string GetFullPropertyName(MemberExpression member)
    {
        List<string> parts = [];
        while (member != null)
        {
            parts.Add(member.Member.Name);
            member = member.Expression as MemberExpression;
        }

        parts.Reverse();
        return string.Join("_", parts);
    }
}

/// <summary>
/// Represents an individual word in a search field and its match status.
/// </summary>
public class SearchWord
{
    /// <summary>
    /// Gets or sets the original word content from the field.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the lowercase version of the word for case-insensitive matching.
    /// </summary>
    public string LowercaseContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this word matched the search term.
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Gets or sets the match score for this word.
    /// </summary>
    public double Score { get; set; }
}

/// <summary>
/// Represents a specific field within an object that matched a search query.
/// </summary>
public class SearchField
{
    /// <summary>
    /// Gets or sets the name of the field where the match occurred.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the original value of the field that was searched.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the relevance score of this specific field within the object.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the list of words that contributed to the match.
    /// </summary>
    public List<SearchWord> Words { get; set; } = new();
}
