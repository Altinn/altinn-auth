namespace Altinn.Authorization.ABAC
{
    /// <summary>
    /// The decision a single rule contributes to a rule-combining algorithm, and the decision
    /// a rule-combining algorithm produces. XACML 3.0 section 7.11 splits Indeterminate by the
    /// decisions the element could have reached had evaluation not errored, and the combining
    /// tables in Appendix C are written in terms of that split.
    /// </summary>
    internal enum RuleDecision
    {
        /// <summary>
        /// The rule does not apply to the request.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// The rule applies and its effect is Permit.
        /// </summary>
        Permit,

        /// <summary>
        /// The rule applies and its effect is Deny.
        /// </summary>
        Deny,

        /// <summary>
        /// Evaluation errored on a rule that could only have reached Permit. Written
        /// Indeterminate{P} in the specification.
        /// </summary>
        IndeterminatePermit,

        /// <summary>
        /// Evaluation errored on a rule that could only have reached Deny. Written
        /// Indeterminate{D} in the specification.
        /// </summary>
        IndeterminateDeny,

        /// <summary>
        /// Evaluation errored and both Permit and Deny were reachable. Written
        /// Indeterminate{DP} in the specification. No single rule produces this value;
        /// it only arises from combining.
        /// </summary>
        IndeterminateDenyPermit,
    }
}
