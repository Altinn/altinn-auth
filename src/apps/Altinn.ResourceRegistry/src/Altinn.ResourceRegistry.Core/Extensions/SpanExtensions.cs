using System.Collections;

namespace Altinn.ResourceRegistry.Extensions;

/// <summary>
/// Extension methods for <see cref="Span{T}"/>.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Splits a span of characters by a separator.
    /// </summary>
    /// <param name="span">The span.</param>
    /// <param name="separator">The separator character.</param>
    /// <param name="options">Optional string split options.</param>
    /// <returns>An enumerable over the subslices of the string span.</returns>
    public static StringSplitEnumerable Split(this ReadOnlySpan<char> span, char separator, StringSplitOptions options = StringSplitOptions.None)
        => new(span, separator, options);

    /// <summary>
    /// An enumerable of subspans of a string, split by a separator.
    /// </summary>
    public readonly ref struct StringSplitEnumerable
    {
        private readonly ReadOnlySpan<char> _span;
        private readonly StringSplitOptions _options;
        private readonly char _separator;

        /// <summary>
        /// Constructs a new instance of <see cref="StringSplitEnumerable"/>.
        /// </summary>
        /// <param name="span">The span to split.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="options">The split options.</param>
        public StringSplitEnumerable(ReadOnlySpan<char> span, char separator, StringSplitOptions options)
        {
            _span = span;
            _separator = separator;
            _options = options;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator() => new(_span, _separator, _options);

        /// <summary>
        /// An enumerator of subspans of a string, split by a separator.
        /// </summary>
        public ref struct Enumerator
        {
            // Note: this bool is intentionally negated so that default(Enumerator) produces no elements.
            private bool _notEnded;
            private ReadOnlySpan<char> _current;
            private ReadOnlySpan<char> _rest;

            private readonly char _separator;
            private readonly StringSplitOptions _options;

            /// <summary>
            /// Constructs a new instance of <see cref="Enumerator"/>.
            /// </summary>
            /// <param name="span">The span to split.</param>
            /// <param name="separator">The separator.</param>
            /// <param name="options">The split options.</param>
            public Enumerator(ReadOnlySpan<char> span, char separator, StringSplitOptions options)
            {
                _rest = span;
                _separator = separator;
                _options = options;
                _notEnded = true;
                _current = default;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                while (_notEnded)
                {
                    var index = _rest.IndexOf(_separator);
                    if (index < 0)
                    {
                        _current = _rest;
                        _rest = default;
                        _notEnded = false;
                    }
                    else
                    {
                        _current = _rest[..index];
                        _rest = _rest[(index + 1)..];
                    }

                    if (_options.HasFlag(StringSplitOptions.TrimEntries))
                    {
                        _current = _current.Trim();
                    }

                    if (_options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && _current.IsEmpty)
                    {
                        continue;
                    }

                    return true;
                }

                _current = default;
                return false;
            }

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ReadOnlySpan<char> Current => _current;
        }
    }
}
