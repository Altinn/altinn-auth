namespace Altinn.Authorization.ABAC
{
    using System;
    using System.Collections.Generic;
    using Altinn.Authorization.ABAC.Constants;

    /// <summary>
    /// Combines the decisions of the rules in a policy according to the combining algorithm
    /// tables in XACML 3.0 Core (OASIS Standard, 22 January 2013), Appendix C: C.2
    /// deny-overrides, C.3 ordered-deny-overrides, C.4 permit-overrides, C.5
    /// ordered-permit-overrides, C.6 deny-unless-permit, C.7 permit-unless-deny and C.8
    /// first-applicable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ordered variants share a decision table with their unordered counterparts. Ordering
    /// constrains how obligations and advice are collected, not the decision. Each table is
    /// also reachable through the matching policy-combining URN, because policies generated for
    /// delegation name a policy-combining algorithm in the RuleCombiningAlgId attribute.
    /// </para>
    /// <para>
    /// A policy whose rules are all excluded by their own target combines an empty list of
    /// children. Every table states that case for itself: deny-overrides, permit-overrides and
    /// first-applicable answer NotApplicable, deny-unless-permit answers Deny, permit-unless-deny
    /// answers Permit, and an identifier that names no table answers Indeterminate.
    /// </para>
    /// </remarks>
    internal static class RuleCombiner
    {
        private static readonly string[] DenyOverridesAlgorithms =
        {
            XacmlConstants.CombiningAlgorithms.RuleDenyOverrides,
            XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides,
            XacmlConstants.CombiningAlgorithms.RuleOrderedDenyOverrides,
            XacmlConstants.CombiningAlgorithms.PolicyOrderedDenyOverrided,
        };

        private static readonly string[] PermitOverridesAlgorithms =
        {
            XacmlConstants.CombiningAlgorithms.RulePermitOverrides,
            XacmlConstants.CombiningAlgorithms.PolicyPermidOverrides,
            XacmlConstants.CombiningAlgorithms.RuleOrderedPermitOverrides,
            XacmlConstants.CombiningAlgorithms.PolicyOrderedPermitOverrides,
        };

        private static readonly string[] DenyUnlessPermitAlgorithms =
        {
            XacmlConstants.CombiningAlgorithms.RuleDenyUnlessPermit,
            XacmlConstants.CombiningAlgorithms.PolicyDenyUnlessPermit,
        };

        private static readonly string[] PermitUnlessDenyAlgorithms =
        {
            XacmlConstants.CombiningAlgorithms.RulePermittUnlessDeny,
            XacmlConstants.CombiningAlgorithms.PolicyPermitUnlessDeny,
        };

        private static readonly string[] FirstApplicableAlgorithms =
        {
            XacmlConstants.CombiningAlgorithms.RuleFirstApplicable,
            XacmlConstants.CombiningAlgorithms.PolicyFirstApplicable,
        };

        /// <summary>
        /// Combines the rule decisions of a policy with the algorithm the policy names.
        /// </summary>
        /// <param name="ruleCombiningAlgId">The RuleCombiningAlgId of the policy.</param>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>
        /// The combined decision. An algorithm identifier that names no known combining table
        /// yields Indeterminate{DP}: the policy cannot be evaluated, so neither Permit nor Deny
        /// can be ruled out.
        /// </returns>
        internal static RuleDecision Combine(Uri ruleCombiningAlgId, IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleCombiningAlgId == null)
            {
                return RuleDecision.IndeterminateDenyPermit;
            }

            if (IsAnyOf(ruleCombiningAlgId, DenyOverridesAlgorithms))
            {
                return DenyOverrides(ruleDecisions);
            }

            if (IsAnyOf(ruleCombiningAlgId, PermitOverridesAlgorithms))
            {
                return PermitOverrides(ruleDecisions);
            }

            if (IsAnyOf(ruleCombiningAlgId, DenyUnlessPermitAlgorithms))
            {
                return DenyUnlessPermit(ruleDecisions);
            }

            if (IsAnyOf(ruleCombiningAlgId, PermitUnlessDenyAlgorithms))
            {
                return PermitUnlessDeny(ruleDecisions);
            }

            if (IsAnyOf(ruleCombiningAlgId, FirstApplicableAlgorithms))
            {
                return FirstApplicable(ruleDecisions);
            }

            return RuleDecision.IndeterminateDenyPermit;
        }

        /// <summary>
        /// Tells whether the combined decision is one of the extended Indeterminate values.
        /// </summary>
        /// <param name="decision">The combined decision.</param>
        /// <returns>True when the decision collapses to Indeterminate.</returns>
        internal static bool IsIndeterminate(RuleDecision decision) =>
            decision == RuleDecision.IndeterminatePermit
            || decision == RuleDecision.IndeterminateDeny
            || decision == RuleDecision.IndeterminateDenyPermit;

        /// <summary>
        /// XACML 3.0 C.2 deny-overrides and C.3 ordered-deny-overrides. No children means
        /// NotApplicable.
        /// </summary>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>The combined decision.</returns>
        private static RuleDecision DenyOverrides(IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleDecisions.Count == 0)
            {
                return RuleDecision.NotApplicable;
            }

            bool atLeastOneErrorDeny = false;
            bool atLeastOneErrorPermit = false;
            bool atLeastOneErrorDenyPermit = false;
            bool atLeastOnePermit = false;

            foreach (RuleDecision decision in ruleDecisions)
            {
                switch (decision)
                {
                    case RuleDecision.Deny:
                        return RuleDecision.Deny;
                    case RuleDecision.Permit:
                        atLeastOnePermit = true;
                        break;
                    case RuleDecision.IndeterminateDeny:
                        atLeastOneErrorDeny = true;
                        break;
                    case RuleDecision.IndeterminatePermit:
                        atLeastOneErrorPermit = true;
                        break;
                    case RuleDecision.IndeterminateDenyPermit:
                        atLeastOneErrorDenyPermit = true;
                        break;
                    default:
                        break;
                }
            }

            if (atLeastOneErrorDenyPermit || (atLeastOneErrorDeny && (atLeastOneErrorPermit || atLeastOnePermit)))
            {
                return RuleDecision.IndeterminateDenyPermit;
            }

            if (atLeastOneErrorDeny)
            {
                return RuleDecision.IndeterminateDeny;
            }

            if (atLeastOnePermit)
            {
                return RuleDecision.Permit;
            }

            if (atLeastOneErrorPermit)
            {
                return RuleDecision.IndeterminatePermit;
            }

            return RuleDecision.NotApplicable;
        }

        /// <summary>
        /// XACML 3.0 C.4 permit-overrides and C.5 ordered-permit-overrides. No children means
        /// NotApplicable.
        /// </summary>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>The combined decision.</returns>
        private static RuleDecision PermitOverrides(IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleDecisions.Count == 0)
            {
                return RuleDecision.NotApplicable;
            }

            bool atLeastOneErrorDeny = false;
            bool atLeastOneErrorPermit = false;
            bool atLeastOneErrorDenyPermit = false;
            bool atLeastOneDeny = false;

            foreach (RuleDecision decision in ruleDecisions)
            {
                switch (decision)
                {
                    case RuleDecision.Permit:
                        return RuleDecision.Permit;
                    case RuleDecision.Deny:
                        atLeastOneDeny = true;
                        break;
                    case RuleDecision.IndeterminateDeny:
                        atLeastOneErrorDeny = true;
                        break;
                    case RuleDecision.IndeterminatePermit:
                        atLeastOneErrorPermit = true;
                        break;
                    case RuleDecision.IndeterminateDenyPermit:
                        atLeastOneErrorDenyPermit = true;
                        break;
                    default:
                        break;
                }
            }

            if (atLeastOneErrorDenyPermit || (atLeastOneErrorPermit && (atLeastOneErrorDeny || atLeastOneDeny)))
            {
                return RuleDecision.IndeterminateDenyPermit;
            }

            if (atLeastOneErrorPermit)
            {
                return RuleDecision.IndeterminatePermit;
            }

            if (atLeastOneDeny)
            {
                return RuleDecision.Deny;
            }

            if (atLeastOneErrorDeny)
            {
                return RuleDecision.IndeterminateDeny;
            }

            return RuleDecision.NotApplicable;
        }

        /// <summary>
        /// XACML 3.0 C.6 deny-unless-permit. Permit when a rule permits, otherwise Deny, and so
        /// Deny when there are no children. The algorithm never yields NotApplicable or
        /// Indeterminate.
        /// </summary>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>The combined decision.</returns>
        private static RuleDecision DenyUnlessPermit(IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleDecisions.Count == 0)
            {
                return RuleDecision.Deny;
            }

            foreach (RuleDecision decision in ruleDecisions)
            {
                if (decision == RuleDecision.Permit)
                {
                    return RuleDecision.Permit;
                }
            }

            return RuleDecision.Deny;
        }

        /// <summary>
        /// XACML 3.0 C.7 permit-unless-deny. Deny when a rule denies, otherwise Permit, and so
        /// Permit when there are no children. The algorithm never yields NotApplicable or
        /// Indeterminate.
        /// </summary>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>The combined decision.</returns>
        private static RuleDecision PermitUnlessDeny(IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleDecisions.Count == 0)
            {
                return RuleDecision.Permit;
            }

            foreach (RuleDecision decision in ruleDecisions)
            {
                if (decision == RuleDecision.Deny)
                {
                    return RuleDecision.Deny;
                }
            }

            return RuleDecision.Permit;
        }

        /// <summary>
        /// XACML 3.0 C.8 first-applicable. The first rule that reaches Permit, Deny or an error
        /// decides; a policy whose rules are all NotApplicable, or that has no children at all,
        /// is NotApplicable.
        /// </summary>
        /// <param name="ruleDecisions">The decisions of the rules of the policy, in policy order.</param>
        /// <returns>The combined decision.</returns>
        private static RuleDecision FirstApplicable(IReadOnlyList<RuleDecision> ruleDecisions)
        {
            if (ruleDecisions.Count == 0)
            {
                return RuleDecision.NotApplicable;
            }

            foreach (RuleDecision decision in ruleDecisions)
            {
                if (decision != RuleDecision.NotApplicable)
                {
                    return decision;
                }
            }

            return RuleDecision.NotApplicable;
        }

        private static bool IsAnyOf(Uri ruleCombiningAlgId, string[] algorithms)
        {
            foreach (string algorithm in algorithms)
            {
                if (ruleCombiningAlgId.Equals(algorithm))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
