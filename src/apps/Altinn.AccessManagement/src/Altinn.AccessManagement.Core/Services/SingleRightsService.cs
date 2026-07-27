using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Rights;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Urn;
using Altinn.Urn.Json;

namespace Altinn.AccessManagement.Core.Services
{
    /// <inheritdoc/>
    public class SingleRightsService : ISingleRightsService
    {
        private readonly IContextRetrievalService _contextRetrievalService;
        private readonly IPolicyAdministrationPoint _pap;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleRightsService"/> class.
        /// </summary>
        /// <param name="contextRetrievalService">Service for retrieving context information</param>
        /// <param name="pap">Service implementation for policy administration point</param>
        public SingleRightsService(IContextRetrievalService contextRetrievalService, IPolicyAdministrationPoint pap)
        {
            _contextRetrievalService = contextRetrievalService;
            _pap = pap;
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> TryWriteDelegationPolicyRules(Entity from, Entity to, Resource resource, List<string> ruleKeys, Entity performedBy, bool ignoreExistingPolicy = false, CancellationToken cancellationToken = default)
        {
            var rules = (await GenerateRules(from, to, resource, ruleKeys, performedBy, cancellationToken)).ToList();
            return await _pap.TryWriteDelegationPolicyRules(rules: rules, ignoreExistingPolicy: ignoreExistingPolicy, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<InstanceRule>> TryWriteInstanceDelegationPolicyRules(Entity from, Entity to, Resource resource, string instanceId, List<string> ruleKeys, Entity performedBy, bool ignoreExistingPolicy = false, CancellationToken cancellationToken = default)
        {
            var instanceRules = await GenerateInstanceRules(from, to, resource, instanceId, ruleKeys, performedBy, cancellationToken);

            var instanceRight = new InstanceRight
            {
                FromUuid = from.Id,
                FromType = DelegationHelper.GetUuidTypeFromEntityType(from.TypeId),
                ToUuid = to.Id,
                ToType = DelegationHelper.GetUuidTypeFromEntityType(to.TypeId),
                PerformedBy = performedBy.Id.ToString(),
                PerformedByType = DelegationHelper.GetUuidTypeFromEntityType(performedBy.TypeId),
                ResourceId = resource.RefId,
                InstanceId = instanceId,
                InstanceDelegationMode = InstanceDelegationMode.Normal,
                InstanceDelegationSource = InstanceDelegationSource.User,
                InstanceRules = instanceRules
            };

            InstanceRight result = await _pap.TryWriteInstanceDelegationPolicyRules(instanceRight, ignoreExistingPolicy, cancellationToken);
            return result.InstanceRules;
        }

        /// <inheritdoc/>
        public async Task<string> ClearPolicyRules(string policyPath, string policyVersion, CancellationToken cancellationToken = default)
        {
            return await _pap.ClearPolicyRules(policyPath, policyVersion, cancellationToken);
        }

        private async Task<IEnumerable<Rule>> GenerateRules(Entity from, Entity to, Resource resource, List<string> ruleKeys, Entity performedBy, CancellationToken cancellationToken = default)
        {
            var coveredBy = to;
            var offeredBy = from;

            var offeredByUuidType = DelegationHelper.GetUuidTypeFromEntityType(offeredBy.TypeId);

            List<Rule> rules = [];

            var rightKeys = await _contextRetrievalService.GetResourcePolicyV2(resource.RefId, cancellationToken: cancellationToken);

            foreach (string ruleKey in ruleKeys)
            {
                var rightKey = rightKeys?.FirstOrDefault(r => r.Key.Equals(ruleKey, StringComparison.InvariantCultureIgnoreCase));

                if (rightKey is null)
                {
                    throw new KeyNotFoundException($"The key: '{ruleKey}' did not exist in the keyset for the resource: '{resource.RefId}'");
                }

                (List<AttributeMatch> Resource, AttributeMatch Action) resourceAndAction = MapRightDtoToResourceListAndAction(rightKey);

                rules.Add(new Rule
                {
                    RuleId = Guid.CreateVersion7().ToString(),
                    Type = RuleType.None,
                    CoveredBy = ConvertEntityToAttributeMatch(coveredBy),
                    Resource = resourceAndAction.Resource,
                    Action = resourceAndAction.Action,
                    OfferedByPartyId = offeredBy.PartyId.HasValue ? offeredBy.PartyId.Value : 0,
                    OfferedByPartyUuid = offeredBy.Id,
                    OfferedByPartyType = offeredByUuidType,
                    PerformedBy = ConvertEntityToAttributeMatch(performedBy),
                    DelegatedByUserId = performedBy.UserId,
                    DelegatedByPartyId = performedBy.PartyId
                });
            }

            return rules;
        }

        private async Task<List<InstanceRule>> GenerateInstanceRules(Entity from, Entity to, Resource resource, string instanceId, List<string> ruleKeys, Entity performedBy, CancellationToken cancellationToken = default)
        {
            List<InstanceRule> instanceRules = [];

            var rightKeys = await _contextRetrievalService.GetResourcePolicyV2(resource.RefId, cancellationToken: cancellationToken);

            // Create instance-id URN that will be added to all rules
            UrnJsonTypeValue instanceUrn = KeyValueUrn.CreateUnchecked(
                $"{AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceInstanceAttribute}:{instanceId}",
                AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceInstanceAttribute.Length + 1
            );

            foreach (string ruleKey in ruleKeys)
            {
                var rightKey = rightKeys?.FirstOrDefault(r => r.Key.Equals(ruleKey, StringComparison.InvariantCultureIgnoreCase));

                if (rightKey is null)
                {
                    throw new KeyNotFoundException($"The key: '{ruleKey}' did not exist in the keyset for the resource: '{resource.RefId}'");
                }

                (List<UrnJsonTypeValue> Resource, ActionUrn Action) resourceAndAction = MapRightDtoToUrnTypes(rightKey);

                // Add instance-id URN to resource URNs
                resourceAndAction.Resource.Add(instanceUrn);

                instanceRules.Add(new InstanceRule
                {
                    RuleId = Guid.CreateVersion7().ToString(),
                    Resource = resourceAndAction.Resource,
                    Action = resourceAndAction.Action
                });
            }

            return instanceRules;
        }

        private static (List<AttributeMatch> Resource, AttributeMatch Action) MapRightDtoToResourceListAndAction(RightDto actionKey)
        {
            List<AttributeMatch> resourceList = [];
            AttributeMatch action;

            foreach (AttributeDto resource in actionKey.Resource)
            {
                AttributeMatch currentAttributeMatch = new(resource.Type, resource.Value);

                if (currentAttributeMatch.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, StringComparison.InvariantCultureIgnoreCase) && DelegationHelper.IsAppResourceId(currentAttributeMatch.Value, out string org, out string app))
                {
                    resourceList.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, Value = org });
                    resourceList.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, Value = app });
                }
                else
                {
                    resourceList.Add(currentAttributeMatch);
                }
            }

            action = new AttributeMatch(actionKey.Action.Type, actionKey.Action.Value);
            return (resourceList, action);
        }

        private static (List<UrnJsonTypeValue> Resource, ActionUrn Action) MapRightDtoToUrnTypes(RightDto rightDto)
        {
            List<UrnJsonTypeValue> resourceUrns = [];

            foreach (AttributeDto resource in rightDto.Resource)
            {
                // Check if this is an app resource that needs splitting
                if (resource.Type.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, StringComparison.InvariantCultureIgnoreCase)
                    && DelegationHelper.IsAppResourceId(resource.Value, out string org, out string app))
                {
                    // Split app resource into org and app URNs
                    UrnJsonTypeValue orgUrn = KeyValueUrn.CreateUnchecked(
                        $"{AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute}:{org}",
                        AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute.Length + 1
                    );
                    UrnJsonTypeValue appUrn = KeyValueUrn.CreateUnchecked(
                        $"{AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute}:{app}",
                        AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute.Length + 1
                    );
                    resourceUrns.Add(orgUrn);
                    resourceUrns.Add(appUrn);
                }
                else
                {
                    // Regular resource URN
                    UrnJsonTypeValue resourceUrn = KeyValueUrn.CreateUnchecked(
                        $"{resource.Type}:{resource.Value}",
                        resource.Type.Length + 1
                    );
                    resourceUrns.Add(resourceUrn);
                }
            }

            // Convert action to ActionUrn
            ActionIdentifier actionIdentifier = ActionIdentifier.CreateUnchecked(rightDto.Action.Value);
            ActionUrn actionUrn = ActionUrn.ActionId.Create(actionIdentifier);

            return (resourceUrns, actionUrn);
        }

        private static List<AttributeMatch> ConvertEntityToAttributeMatch(Entity entity)
        {
            var matches = new List<AttributeMatch>();

            // UUID
            var uuidType = DelegationHelper.GetUuidTypeFromEntityType(entity.TypeId);
            matches.Add(new AttributeMatch
            {
                Id = uuidType.EnumMemberAttributeValueOrName(),
                Value = entity.Id.ToString()
            });

            // User
            if (entity.UserId.HasValue)
            {
                matches.Add(new AttributeMatch
                {
                    Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute,
                    Value = entity.UserId.Value.ToString()
                });
            }
            else if (entity.PartyId.HasValue)
            {
                // Party
                matches.Add(new AttributeMatch
                {
                    Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute,
                    Value = entity.PartyId.Value.ToString()
                });
            }

            return matches;
        }
    }
}
