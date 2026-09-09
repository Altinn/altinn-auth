namespace Altinn.Authorization.ABAC
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Altinn.Authorization.ABAC.Constants;
    using Altinn.Authorization.ABAC.Xacml;

    /// <summary>
    /// This is the Policy Decision Point performing validation of request against policies.
    /// </summary>
    public class PolicyDecisionPoint
    {
        /// <summary>
        /// Method that validated if the subject is allwoed to perform the requested operation on a given resource.
        /// </summary>
        /// <param name="decisionRequest">The Xacml Context request.</param>
        /// <param name="policy">The relevant policy.</param>
        /// <returns>The decision reponse.</returns>
        public XacmlContextResponse Authorize(XacmlContextRequest decisionRequest, XacmlPolicy policy)
        {
            XacmlContextResult contextResult;

            ICollection<XacmlRule> matchingRules = this.GetMatchingRules(policy, decisionRequest, out bool requiredAttributesMissingFromContextRequest);

            if (requiredAttributesMissingFromContextRequest)
            {
                contextResult = new XacmlContextResult(XacmlContextDecision.Indeterminate)
                {
                    Status = new XacmlContextStatus(XacmlContextStatusCode.MissingAttribute),
                };
                return new XacmlContextResponse(contextResult);
            }

            List<RuleDecision> ruleDecisions = new List<RuleDecision>(matchingRules?.Count ?? 0);
            bool processingError = false;

            foreach (XacmlRule rule in matchingRules ?? Array.Empty<XacmlRule>())
            {
                RuleDecision decision;

                // Need to authorize based on the information in the Xacml context request
                XacmlAttributeMatchResult subjectMatchResult = rule.MatchAttributes(decisionRequest, XacmlConstants.MatchAttributeCategory.Subject);
                if (subjectMatchResult.Equals(XacmlAttributeMatchResult.Match))
                {
                    decision = rule.Effect.Equals(XacmlEffectType.Permit) ? RuleDecision.Permit : RuleDecision.Deny;
                }
                else if (subjectMatchResult.Equals(XacmlAttributeMatchResult.RequiredAttributeMissing))
                {
                    ruleDecisions.Add(IndeterminateFor(rule));
                    continue;
                }
                else
                {
                    decision = RuleDecision.NotApplicable;
                }

                if ((decision.Equals(RuleDecision.Deny) || decision.Equals(RuleDecision.Permit)) && rule.Condition != null)
                {
                    XacmlAttributeMatchResult conditionDidEvaluate = rule.EvaluateCondition(decisionRequest);

                    if (conditionDidEvaluate.Equals(XacmlAttributeMatchResult.NoMatch))
                    {
                        decision = RuleDecision.NotApplicable;
                    }
                    else if (conditionDidEvaluate.Equals(XacmlAttributeMatchResult.RequiredAttributeMissing)
                        || conditionDidEvaluate.Equals(XacmlAttributeMatchResult.BagSizeConditionFailed))
                    {
                        decision = IndeterminateFor(rule);
                    }
                    else if (conditionDidEvaluate.Equals(XacmlAttributeMatchResult.ToManyAttributes))
                    {
                        decision = IndeterminateFor(rule);
                        processingError = true;
                    }
                }

                ruleDecisions.Add(decision);
            }

            RuleDecision combinedDecision = RuleCombiner.Combine(policy.RuleCombiningAlgId, ruleDecisions);
            bool indeterminate = RuleCombiner.IsIndeterminate(combinedDecision);

            XacmlContextDecision overallDecision = indeterminate ? XacmlContextDecision.Indeterminate : ToContextDecision(combinedDecision);
            XacmlContextStatusCode statusCode = indeterminate && processingError ? XacmlContextStatusCode.ProcessingError : XacmlContextStatusCode.Success;

            contextResult = new XacmlContextResult(overallDecision)
            {
                Status = new XacmlContextStatus(statusCode),
            };
            this.AddObligations(policy, contextResult);

            // The result echoes request attributes only when rules were evaluated and reached a
            // decision. A policy the request reaches no rule of, and an Indeterminate that stems
            // from an evaluation error, both report none.
            if (!indeterminate && ruleDecisions.Count > 0)
            {
                this.AddRequestAttributes(decisionRequest, contextResult);
            }

            return new XacmlContextResponse(contextResult);
        }

        /// <summary>
        /// The extended Indeterminate value of a rule whose evaluation errored. Per XACML 3.0
        /// section 7.11 the value is given by the effect the rule would have had.
        /// </summary>
        /// <param name="rule">The rule that errored.</param>
        /// <returns>Indeterminate{P} for a Permit rule, Indeterminate{D} for a Deny rule.</returns>
        private static RuleDecision IndeterminateFor(XacmlRule rule) =>
            rule.Effect.Equals(XacmlEffectType.Permit) ? RuleDecision.IndeterminatePermit : RuleDecision.IndeterminateDeny;

        /// <summary>
        /// Maps a combined rule decision that is not Indeterminate to the decision of the context result.
        /// </summary>
        /// <param name="decision">The combined rule decision.</param>
        /// <returns>The context decision.</returns>
        private static XacmlContextDecision ToContextDecision(RuleDecision decision) => decision switch
        {
            RuleDecision.Permit => XacmlContextDecision.Permit,
            RuleDecision.Deny => XacmlContextDecision.Deny,
            _ => XacmlContextDecision.NotApplicable,
        };

        /// <summary>
        /// Returns the list of rules that matched the ContextRequest.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="decisionRequest">The decision request.</param>
        /// <param name="requiredAttributeMissing">Tels if a required attribute is missing.</param>
        /// <returns>All rules that are matching.</returns>
        private ICollection<XacmlRule> GetMatchingRules(XacmlPolicy policy, XacmlContextRequest decisionRequest, out bool requiredAttributeMissing)
        {
            ICollection<XacmlRule> matchingRules = new Collection<XacmlRule>();

            requiredAttributeMissing = false;

            foreach (XacmlRule rule in policy.Rules)
            {
                XacmlAttributeMatchResult resourceMatch = rule.MatchAttributes(decisionRequest, XacmlConstants.MatchAttributeCategory.Resource);
                XacmlAttributeMatchResult actionMatch = rule.MatchAttributes(decisionRequest, XacmlConstants.MatchAttributeCategory.Action);

                if (resourceMatch.Equals(XacmlAttributeMatchResult.Match) && actionMatch.Equals(XacmlAttributeMatchResult.Match))
                {
                    matchingRules.Add(rule);
                }
                else if (resourceMatch.Equals(XacmlAttributeMatchResult.RequiredAttributeMissing) || actionMatch.Equals(XacmlAttributeMatchResult.RequiredAttributeMissing))
                {
                    requiredAttributeMissing = true;
                }
            }

            return matchingRules;
        }

        private void AddObligations(XacmlPolicy policy, XacmlContextResult result)
        {
            if (result.Decision.Equals(XacmlContextDecision.Permit))
            {
                if (policy.ObligationExpressions.Count > 0)
                {
                    IEnumerable<XacmlObligationExpression> obligationsWithPermit = policy.ObligationExpressions.Where(o => o.FulfillOn == XacmlEffectType.Permit);
                    foreach (XacmlObligationExpression expression in obligationsWithPermit)
                    {
                        List<XacmlAttributeAssignment> attributeAssignments = new List<XacmlAttributeAssignment>();
                        foreach (XacmlAttributeAssignmentExpression ex in expression.AttributeAssignmentExpressions)
                        {
                            Type applyElemType = ex.Property.GetType();

                            if (applyElemType == typeof(XacmlAttributeValue))
                            {
                                XacmlAttributeValue attributeValue = ex.Property as XacmlAttributeValue;

                                XacmlAttributeAssignment attributeAssignment = new XacmlAttributeAssignment(ex.AttributeId, attributeValue.DataType, attributeValue.Value)
                                {
                                    Category = ex.Category,
                                    Issuer = ex.Issuer,
                                };

                                attributeAssignments.Add(attributeAssignment);
                            }
                        }

                        XacmlObligation obligation = new XacmlObligation(expression.ObligationId, attributeAssignments)
                        {
                            FulfillOn = XacmlEffectType.Permit,
                        };

                        result.Obligations.Add(obligation);
                    }
                }
            }
        }

        private void AddRequestAttributes(XacmlContextRequest decisionRequest, XacmlContextResult result)
        {
            foreach (XacmlContextAttributes attribute in decisionRequest.Attributes)
            {
                bool hasResponseAttributes = false;
                XacmlContextAttributes responseAttribute = new XacmlContextAttributes(attribute.Category) { Content = attribute.Content, Id = attribute.Id };

                foreach (XacmlAttribute atr in attribute.Attributes)
                {
                    if (atr.IncludeInResult)
                    {
                        hasResponseAttributes = true;
                        responseAttribute.Attributes.Add(atr);
                    }
                }

                if (hasResponseAttributes)
                {
                    result.Attributes.Add(responseAttribute);
                }
            }
        }
    }
}
