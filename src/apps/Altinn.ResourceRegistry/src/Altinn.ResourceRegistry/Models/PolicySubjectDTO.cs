using Altinn.ResourceRegistry.Core.Models;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// Defines a  Policy Subject
    /// </summary>
    public class PolicySubjectDTO
    {
        /// <summary>
        /// Subject attributes that defines the subject
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> SubjectAttributes { get; init; }

        /// <summary>
        /// Map to DTO List
        /// </summary>
        public static IEnumerable<PolicySubjectDTO> MapFrom(IEnumerable<PolicySubject> policySubjects)
        {
            return policySubjects?.Select(r => MapFrom(r));
        }

        /// <summary>
        /// Map to DTO
        /// </summary>
        public static PolicySubjectDTO MapFrom(PolicySubject policySubject)
        {
            if (policySubject == null)
            {
                return null;
            }

            PolicySubjectDTO policySubjectDTO = new PolicySubjectDTO
            {
                SubjectAttributes = policySubject.SubjectAttributes
            };

            return policySubjectDTO;
        }
    }
}
