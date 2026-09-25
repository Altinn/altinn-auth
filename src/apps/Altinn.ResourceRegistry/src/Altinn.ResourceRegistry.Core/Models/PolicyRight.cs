#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines a flatten Policy Rule
    /// </summary>
    public class PolicyRight
    {
        private string? _rightKey;
        private SortedSet<string>? _subjectTypes;

        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public required UrnJsonTypeValue Action { get; init; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Resource { get; init; }

        /// <summary>
        /// List of subjects that is allowed to perform the action on the resource
        /// </summary>
        public required IReadOnlyList<PolicySubject> Subjects { get; init; }

        /// <summary>
        /// Returns the right key for the right part of policy resource action
        /// </summary>
        public string RightKey
            => _rightKey ??= CalculateRightKey();

        /// <summary>
        /// Returns a list of subject types that is allowed to perform the action on the resource
        /// IS used for filtering the 
        /// </summary>
        public IReadOnlySet<string> SubjectTypes
            => _subjectTypes ??= CalculateSubjectTypes();

        private SortedSet<string> CalculateSubjectTypes()
        {
            SortedSet<string> subjectTypes = new SortedSet<string>();

            foreach (var subject in Subjects)
            {
                foreach (var attr in subject.SubjectAttributes)
                {
                    subjectTypes.Add(attr.Value.PrefixSpan.ToString().ToLowerInvariant());
                }
            }

            return subjectTypes;
        }

        private string CalculateRightKey()
        {
            var sb = _stringBuilder ?? new();

            UrnJsonTypeValue[]? resourcesArray = null;
            byte[]? rentedBytes = null;

            try
            {
                resourcesArray = ArrayPool<UrnJsonTypeValue>.Shared.Rent(Resource.Count);

                // copy all of the resources so we can sort them
                var resources = CopyUrns(Resource, resourcesArray);
                resources.Sort(static (x, y) => x.Value.Urn.CompareTo(y.Value.Urn));

                // first, fill with the string to be hashed
                sb.Append(Action.Value.ValueSpan);
                foreach (var resource in resources)
                {
                    sb.Append(';').Append(resource.Value.Urn);
                }

                // compute the MD5 hash
                Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
                rentedBytes = ArrayPool<byte>.Shared.Rent(sb.Length * 2); // every char is 2 bytes
                var toHash = CopyBytes(sb, rentedBytes);
                MD5.HashData(toHash, hash);

                // build the final key
                sb.Clear();
                sb.Append(Action.Value.ValueSpan);
                foreach (var resource in resources)
                {
                    sb.Append(';').Append(resource.Value.ValueSpan);
                }

                var success = true;
                sb.Append(';');
                Span<char> dest = stackalloc char[2];
                for (var i = 0; i < MD5.HashSizeInBytes; i++)
                {
                    var b = hash[i];
                    success |= b.TryFormat(dest, out _, "x2");
                    sb.Append(dest);
                }

                Debug.Assert(success);
                return sb.ToString();
            }
            finally
            {
                if (resourcesArray is not null)
                {
                    ArrayPool<UrnJsonTypeValue>.Shared.Return(resourcesArray);
                }

                if (rentedBytes is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBytes);
                }

                sb.Clear();
                _stringBuilder = sb;
            }

            static Span<UrnJsonTypeValue> CopyUrns(IReadOnlyList<UrnJsonTypeValue> from, UrnJsonTypeValue[] to)
            {
                if (from is ICollection<UrnJsonTypeValue> collection)
                {
                    collection.CopyTo(to, 0);
                    return to.AsSpan(0, from.Count);
                }

                for (int i = 0; i < from.Count; i++)
                {
                    to[i] = from[i];
                }

                return to.AsSpan(0, from.Count);
            }

            static ReadOnlySpan<byte> CopyBytes(StringBuilder from, byte[] to)
            {
                var position = 0;
                foreach (var chunk in from.GetChunks())
                {
                    var bytes = MemoryMarshal.AsBytes(chunk.Span);
                    bytes.CopyTo(to.AsSpan(position));
                    position += bytes.Length;
                }

                return to.AsSpan(0, position);
            }
        }

        [ThreadStatic]
        private static StringBuilder? _stringBuilder;
    }
}
