using System.ComponentModel.DataAnnotations;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.Rights;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.ABAC;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Urn;
using Altinn.Urn.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using DbModels = Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// The Policy Information Point responsible for storing and modifying delegation policies
    /// </summary>
    public class PolicyInformationPoint : IPolicyInformationPoint
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IPolicyRetrievalPoint _prp;
        private readonly IDelegationMetadataRepository _delegationRepository;
        private readonly IContextRetrievalService _contextRetrievalService;
        private readonly IProfileClient _profile;
        private readonly IFeatureManager _featureManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyInformationPoint"/> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="policyRetrievalPoint">The policy retrieval point</param>
        /// <param name="delegationRepository">The delegation change repository</param>
        /// <param name="contextRetrievalService">Service for retrieving context information</param>
        /// <param name="profile">Service for retrieving user profile information</param>
        /// <param name="dbContext">The database context</param>
        /// <param name="featureManager">The feature manager</param>
        public PolicyInformationPoint(ILogger<IPolicyInformationPoint> logger, IPolicyRetrievalPoint policyRetrievalPoint, IDelegationMetadataRepository delegationRepository, IContextRetrievalService contextRetrievalService, IProfileClient profile, AppDbContext dbContext, IFeatureManager featureManager)
        {
            _logger = logger;
            _prp = policyRetrievalPoint;
            _delegationRepository = delegationRepository;
            _contextRetrievalService = contextRetrievalService;
            _profile = profile;
            _dbContext = dbContext;
            _featureManager = featureManager;
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> GetRulesAsync(List<string> resourceIds, List<int> offeredByPartyIds, List<int> coveredByPartyIds, List<int> coveredByUserIds, CancellationToken cancellationToken = default)
        {
            List<Rule> rules = new List<Rule>();
            List<DelegationChange> delegationChanges = await _delegationRepository.GetAllCurrentAppDelegationChanges(offeredByPartyIds, resourceIds, coveredByPartyIds, coveredByUserIds, cancellationToken);
            foreach (DelegationChange delegationChange in delegationChanges)
            {
                if (delegationChange.DelegationChangeType != DelegationChangeType.RevokeLast)
                {
                    XacmlPolicy policy = await _prp.GetPolicyVersionAsync(delegationChange.BlobStoragePolicyPath, delegationChange.BlobStorageVersionId, cancellationToken);
                    rules.AddRange(GetRulesFromPolicyAndDelegationChange(policy.Rules, delegationChange));
                }
            }

            return rules;
        }

        /// <inheritdoc />
        public async Task<List<Right>> GetDelegableRightsByApp(RightsQuery rightsQuery, CancellationToken cancellationToken = default)
        {
            Dictionary<string, Right> result = new Dictionary<string, Right>();
            if (rightsQuery.Type != RightsQueryType.AltinnApp)
            {
                return result.Values.ToList();
            }

            XacmlPolicy policy = await GetPolicy(rightsQuery.Resource.AuthorizationReference, cancellationToken);

            int minimumAuthenticationLevel = PolicyHelper.GetMinimumAuthenticationLevelFromXacmlPolicy(policy);
            RightSourceType policyType = (rightsQuery.Resource.ResourceType == ResourceType.AltinnApp || rightsQuery.Resource.ResourceType == ResourceType.MigratedApp) ? RightSourceType.AppPolicy : RightSourceType.ResourceRegistryPolicy;
            EnrichRightsDictionaryWithRightsFromPolicy(result, policy, policyType, rightsQuery.To, minimumAuthenticationLevel: minimumAuthenticationLevel, returnAllPolicyRights: false, getDelegableRights: true);

            return result.Values.Where(r => r.CanDelegate.HasValue && r.CanDelegate.Value).ToList();
        }

        private async Task<XacmlPolicy> GetPolicy(List<AttributeMatch> resource, CancellationToken cancellationToken)
        {
            XacmlPolicy policy = null;

            // Verify resource
            if (!DelegationHelper.TryGetResourceFromAttributeMatch(resource, out ResourceAttributeMatchType resourceMatchType, out string resourceId, out string org, out string app, out string _, out string _)
                || resourceMatchType == ResourceAttributeMatchType.None)
            {
                throw new ValidationException($"RightsQuery must specify a valid Resource. Valid resource can either be a single resource from the Altinn resource registry ({AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute}) or an Altinn app (identified by both {AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute} and {AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute})");
            }

            if (resourceMatchType == ResourceAttributeMatchType.ResourceRegistry)
            {
                policy = await _prp.GetPolicyAsync(resourceId, cancellationToken);
            }
            else if (resourceMatchType == ResourceAttributeMatchType.AltinnAppId)
            {
                policy = await _prp.GetPolicyAsync(org, app, cancellationToken);
            }

            if (policy == null)
            {
                throw new ValidationException($"No valid policy found for the specified resource");
            }

            return policy;
        }

        /// <inheritdoc/>
        public async Task<DelegationChangeList> GetAllDelegations(DelegationChangeInput request, bool includeInstanceDelegations = false, CancellationToken cancellationToken = default)
        {
            DelegationChangeList result = new DelegationChangeList();
            bool validSubjectUser = DelegationHelper.TryGetUserIdFromAttributeMatch(request.Subject.SingleToList(), out int subjectUserId);
            bool validSubjectParty = DelegationHelper.TryGetPartyIdFromAttributeMatch(request.Subject.SingleToList(), out int subjectPartyId);
            bool validSubjectUuid = DelegationHelper.TryGetUuidFromAttributeMatch(request.Subject.SingleToList(), out Guid subjectUuid, out UuidType subjectUuidType);
            bool validParty = DelegationHelper.TryGetPartyIdFromAttributeMatch(request.Party.SingleToList(), out int partyId);
            bool validResourceMatchType = DelegationHelper.TryGetResourceFromAttributeMatch(request.Resource, out ResourceAttributeMatchType resourceMatchType, out string resourceId, out string _, out string _, out string _, out string _);

            if (!validSubjectUser && !validSubjectParty && (!validSubjectUuid || subjectUuidType != UuidType.SystemUser))
            {
                result.Errors.Add("request.Subject", $"Missing valid subject on request. Valid subject attribute types: either {AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute}, {AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute} or {AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid}");
                return result;
            }

            if (!validParty)
            {
                result.Errors.Add("request.Party", $"Missing valid party on request. Valid party attribute type: {AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute}");
                return result;
            }

            if (!validResourceMatchType)
            {
                result.Errors.Add("request.Resource", $"Missing valid resource on request. Valid resource attribute types: either a single {AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute} or combination of both {AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute} and {AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute}");
                return result;
            }

            result.DelegationChanges = await FindAllDelegations(subjectUserId, subjectPartyId, subjectUuid, subjectUuidType, partyId, resourceId, resourceMatchType, includeInstanceDelegations, cancellationToken);
            return result;
        }

        /// <inheritdoc/>
        public async Task<List<AppsInstanceDelegationResponse>> GetInstanceDelegations(AppsInstanceGetRequest request, CancellationToken cancellationToken)
        {
            List<AppsInstanceDelegationResponse> result = new List<AppsInstanceDelegationResponse>();

            List<InstanceDelegationChange> delegations = await _delegationRepository.GetAllLatestInstanceDelegationChanges(request.InstanceDelegationSource, request.ResourceId, request.InstanceId, cancellationToken);

            List<Guid> fromParties = delegations.Select(d => d.FromUuid).Distinct().ToList();
            if (fromParties.Count > 1)
            {
                throw new ValidationException($"Multiple from parties found for instance delegations: {string.Join(", ", fromParties)}");
            }

            foreach (InstanceDelegationChange delegation in delegations)
            {
                AppsInstanceDelegationResponse appsInstanceDelegationResponse = new AppsInstanceDelegationResponse
                {
                    From = GetPartyUrnFromUuidTypeAndUuid(delegation.FromUuid, delegation.FromUuidType),
                    To = GetPartyUrnFromUuidTypeAndUuid(delegation.ToUuid, delegation.ToUuidType),
                    InstanceDelegationMode = delegation.InstanceDelegationMode,
                    ResourceId = delegation.ResourceId,
                    InstanceId = delegation.InstanceId
                };

                XacmlPolicy policy = await _prp.GetPolicyVersionAsync(delegation.BlobStoragePolicyPath, delegation.BlobStorageVersionId, cancellationToken);
                appsInstanceDelegationResponse.Rights = GetRightsFromPolicy(policy);
                result.Add(appsInstanceDelegationResponse);
            }

            return result;
        }

        private static List<InstanceRightDelegationResult> GetRightsFromPolicy(XacmlPolicy policy)
        {
            List<InstanceRightDelegationResult> result = new List<InstanceRightDelegationResult>();

            foreach (XacmlRule xacmlRule in policy.Rules)
            {
                result.Add(GetInstanceRightDelegationResultFromPolicyRule(xacmlRule));
            }

            return result;
        }

        private static InstanceRightDelegationResult GetInstanceRightDelegationResultFromPolicyRule(XacmlRule xacmlRule)
        {
            InstanceRightDelegationResult rule = new InstanceRightDelegationResult { Resource = [], Status = DelegationStatus.Delegated };

            foreach (XacmlAnyOf anyOf in xacmlRule.Target.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    foreach (XacmlMatch xacmlMatch in allOf.Matches)
                    {
                        if (xacmlMatch.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Action))
                        {
                            rule.Action = ActionUrn.Parse($"{xacmlMatch.AttributeDesignator.AttributeId.OriginalString}:{xacmlMatch.AttributeValue.Value}");
                        }

                        if (xacmlMatch.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Resource))
                        {
                            UrnJsonTypeValue resourcePart = KeyValueUrn.Create($"{xacmlMatch.AttributeDesignator.AttributeId.OriginalString}:{xacmlMatch.AttributeValue.Value}", xacmlMatch.AttributeDesignator.AttributeId.OriginalString.Length + 1);
                            rule.Resource.Add(resourcePart);
                        }
                    }
                }
            }

            return rule;
        }

        private static PartyUrn GetPartyUrnFromUuidTypeAndUuid(Guid uuid, UuidType type)
        {
            string urnString = null;

            switch (type)
            {
                case UuidType.Person:
                case UuidType.Organization:
                    urnString = $"urn:altinn:party:uuid:{uuid.ToString()}";
                    break;
            }

            bool validParty = PartyUrn.TryParse(urnString, out PartyUrn result);

            return validParty ? result : null;
        }

        private async Task<List<DelegationChange>> FindAllDelegations(int subjectUserId, int subjectPartyId, Guid subjectUuid, UuidType subjectUuidType, int reporteePartyId, string resourceId, ResourceAttributeMatchType resourceMatchType, bool includeInstanceDelegations = false, CancellationToken cancellationToken = default)
        {
            if (resourceMatchType == ResourceAttributeMatchType.None)
            {
                throw new NotSupportedException("Must specify the resource match type");
            }

            if ((subjectUserId == 0 ^ subjectPartyId == 0 ^ subjectUuidType == UuidType.NotSpecified) || (subjectUserId != 0 && subjectPartyId != 0 && subjectUuidType != UuidType.NotSpecified))
            {
                throw new NotSupportedException("Must specify the single subjectUserId, subjectPartyId or subjectUuid");
            }

            List<DelegationChange> delegations = new List<DelegationChange>();
            List<int> offeredByPartyIds = reporteePartyId.SingleToList();
            List<string> resourceIds = resourceId.SingleToList();

            Guid? fromParty = null;
            List<Guid> toParties = null;
            HashSet<Guid> toAppControlledRightholders = null;

            var from = await _dbContext.Entities
                    .AsNoTracking() 
                    .Where(e => e.PartyId == reporteePartyId)
                    .FirstOrDefaultAsync(cancellationToken);
            if (includeInstanceDelegations)
            {
                fromParty = from?.Id;
                toParties = new List<Guid>();
                toAppControlledRightholders = [];
            }

            // Check if mainunit exists
            if (from?.ParentId.HasValue == true)
            {
                var parent = await _dbContext.Entities
                    .AsNoTracking()
                    .Where(e => e.Id == from.ParentId.Value)
                    .FirstOrDefaultAsync(cancellationToken);
                if (parent?.PartyId.HasValue == true)
                {
                    offeredByPartyIds.Add(parent.PartyId.Value);
                }
            }

            // 1. Direct user delegations
            if (subjectUserId > 0)
            {
                delegations = resourceMatchType == ResourceAttributeMatchType.AltinnAppId
                ? await _delegationRepository.GetAllCurrentAppDelegationChanges(offeredByPartyIds, resourceIds, coveredByUserIds: subjectUserId.SingleToList(), cancellationToken: cancellationToken)
                : await _delegationRepository.GetAllCurrentResourceRegistryDelegationChanges(offeredByPartyIds, resourceIds, coveredByUserId: subjectUserId, cancellationToken: cancellationToken);

                if (includeInstanceDelegations)
                {
                    NewUserProfile subjectUserProfile = await _profile.GetUser(new UserProfileLookup { UserId = subjectUserId }, cancellationToken: cancellationToken);
                    if (subjectUserProfile != null)
                    {
                        toParties.Add(subjectUserProfile.Party.PartyUuid.Value);
                    }
                }
            }
            else if (subjectUuidType == UuidType.SystemUser)
            {
                delegations = resourceMatchType == ResourceAttributeMatchType.AltinnAppId
                ? await _delegationRepository.GetAllCurrentAppDelegationChanges(resourceIds, offeredByPartyIds, subjectUuidType, subjectUuid, cancellationToken)
                : await _delegationRepository.GetAllCurrentResourceRegistryDelegationChanges(resourceIds, offeredByPartyIds, subjectUuidType, subjectUuid, cancellationToken);
            }

            // 2. Direct party delegations incl. any keyrole units
            List<int> coveredByPartyIds = subjectPartyId > 0 ? new List<int> { subjectPartyId } : new List<int>();
            List<Guid> coveredByPartyUuids = new List<Guid>();

            if (subjectUserId > 0)
            {
                var subject = await _dbContext.Entities
                    .AsNoTracking()
                    .Where(e => e.UserId == subjectUserId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (subject != null)
                {
                    var keyRoleAssignments = await _dbContext.Assignments
                        .AsNoTracking()
                        .Where(t => t.ToId == subject.Id)
                        .Include(t => t.Role)
                        .Include(t => t.From)
                        .Where(t => t.Role.IsKeyRole)
                        .ToListAsync(cancellationToken);
                    var keyRoleSubUnits = await _dbContext.Entities
                        .AsNoTracking()
                        .Where(e => e.ParentId.HasValue && keyRoleAssignments.Select(k => k.FromId).Distinct().Contains(e.ParentId.Value))
                        .ToListAsync(cancellationToken);

                    coveredByPartyIds.AddRange(keyRoleAssignments.Where(t => t.From.PartyId.HasValue).Select(t => t.From.PartyId.Value).Distinct().ToList());
                    coveredByPartyIds.AddRange(keyRoleSubUnits.Where(s => s.PartyId.HasValue).Select(s => s.PartyId.Value));
                    coveredByPartyUuids = keyRoleAssignments.Select(t => t.FromId).Distinct().ToList();
                    coveredByPartyUuids.AddRange(keyRoleSubUnits.Select(s => s.Id).ToList());

                    if (includeInstanceDelegations)
                    {
                        // Fetch all the parties the subject user has the CompanyRepresentativeFormTasks package for, and add them to separate list of toPartiesRuntimeDelegated to verify only AltinnApp instance delegations.
                        var representativeFormTasks = await _dbContext.Assignments
                            .AsNoTracking()
                            .Where(t => t.ToId == subject.Id)
                            .Join(_dbContext.AssignmentPackages, a => a.Id, p => p.AssignmentId, (a, p) => new { Assignment = a, AssignmentPackage = p })
                            .Where(t => t.AssignmentPackage.PackageId == PackageConstants.CompanyRepresentativeFormTasks)
                            .ToListAsync(cancellationToken);
                        var representativeFormTasksSubUnits = await _dbContext.Entities
                            .AsNoTracking()
                            .Where(e => e.ParentId.HasValue && representativeFormTasks.Select(k => k.Assignment.FromId).Distinct().Contains(e.ParentId.Value))
                            .ToListAsync(cancellationToken);

                        toAppControlledRightholders.UnionWith(representativeFormTasks.Select(t => t.Assignment.FromId));
                        toAppControlledRightholders.UnionWith(representativeFormTasksSubUnits.Select(s => s.Id));
                    }
                }
            }

            if (coveredByPartyIds.Count > 0)
            {
                List<DelegationChange> partyDelegations = resourceMatchType == ResourceAttributeMatchType.AltinnAppId
                    ? await _delegationRepository.GetAllCurrentAppDelegationChanges(offeredByPartyIds, resourceIds, coveredByPartyIds: coveredByPartyIds, cancellationToken: cancellationToken)
                    : await _delegationRepository.GetAllCurrentResourceRegistryDelegationChanges(offeredByPartyIds, resourceIds, coveredByPartyIds: coveredByPartyIds, cancellationToken: cancellationToken);
                delegations.AddRange(partyDelegations);

                if (includeInstanceDelegations)
                {
                    if (coveredByPartyUuids.Any())
                    {
                        toParties.AddRange(coveredByPartyUuids);
                    }
                }
            }

            // 3. Get all instance delegations of the resource both directly delegated to user and indirectly through keyrole units and runtime-delegated signing packages
            if (includeInstanceDelegations && fromParty.HasValue && (toParties.Count > 0 || toAppControlledRightholders.Count > 0))
            {
                if (resourceMatchType == ResourceAttributeMatchType.AltinnAppId)
                {
                    string[] resourceOrgApp = resourceId.Split('/');
                    resourceIds = $"app_{resourceOrgApp[0]}_{resourceOrgApp[1]}".SingleToList();
                }

                delegations.AddRange(await GetInstanceDelegations(resourceIds, fromParty.Value, toParties.ToList(), toAppControlledRightholders.ToList(), cancellationToken));
            }

            // 4. Client-delegated resources (v2)
            if (await _featureManager.IsEnabledAsync("AccessManagement.Pip.IncludeClientDelegatedResources", cancellationToken))
            {
                var clientDelegations = await GetClientDelegatedResources(subjectUserId, subjectUuid, subjectUuidType, from, resourceId, resourceMatchType, cancellationToken);
                delegations.AddRange(clientDelegations);
            }

            return delegations;
        }

        private async Task<IEnumerable<DelegationChange>> GetInstanceDelegations(List<string> resourceIds, Guid from, List<Guid> to, List<Guid> toAppControlledRightholders, CancellationToken cancellationToken = default)
        {
            IEnumerable<InstanceDelegationChange> instanceDelegations = await _delegationRepository.GetActiveInstanceDelegations(resourceIds, from, to, toAppControlledRightholders, cancellationToken);
            return from InstanceDelegationChange instanceDelegation in instanceDelegations
                   let delegationChange = new DelegationChange
                   {
                       ResourceId = instanceDelegation.ResourceId,
                       InstanceId = instanceDelegation.InstanceId,
                       FromUuidType = instanceDelegation.FromUuidType,
                       FromUuid = instanceDelegation.FromUuid,
                       ToUuidType = instanceDelegation.ToUuidType,
                       ToUuid = instanceDelegation.ToUuid,
                       PerformedByUuidType = instanceDelegation.PerformedByType,
                       PerformedByUuid = instanceDelegation.PerformedBy,
                       DelegationChangeType = instanceDelegation.DelegationChangeType,
                       BlobStoragePolicyPath = instanceDelegation.BlobStoragePolicyPath,
                       BlobStorageVersionId = instanceDelegation.BlobStorageVersionId,
                       Created = instanceDelegation.Created
                   }
                   select delegationChange;
        }

        private async Task<List<DelegationChange>> GetClientDelegatedResources(int subjectUserId, Guid subjectUuid, UuidType subjectUuidType, DbModels.Entity fromEntity, string resourceId, ResourceAttributeMatchType resourceMatchType, CancellationToken cancellationToken)
        {
            if (fromEntity == null)
            {
                return [];
            }

            // Resolve subject entity UUID
            Guid? subjectEntityId = null;
            if (subjectUserId > 0)
            {
                var subjectEntity = await _dbContext.Entities
                    .AsNoTracking()
                    .Where(e => e.UserId == subjectUserId)
                    .FirstOrDefaultAsync(cancellationToken);
                subjectEntityId = subjectEntity?.Id;
            }
            else if (subjectUuidType == UuidType.SystemUser)
            {
                subjectEntityId = subjectUuid;
            }

            if (!subjectEntityId.HasValue)
            {
                return [];
            }

            // Build list of offering entity IDs (reportee + parent/main unit)
            List<Guid> offeringEntityIds = [fromEntity.Id];
            if (fromEntity.ParentId.HasValue)
            {
                offeringEntityIds.Add(fromEntity.ParentId.Value);
            }

            // Resolve the resource RefId to match against
            string resourceRefId = resourceId;
            if (resourceMatchType == ResourceAttributeMatchType.AltinnAppId)
            {
                string[] resourceOrgApp = resourceId.Split('/');
                resourceRefId = $"app_{resourceOrgApp[0]}_{resourceOrgApp[1]}";
            }

            // Query v2 DelegationResources where:
            // - The delegation's "from" assignment's FromId matches the reportee (offering party)
            // - The delegation's "to" assignment's ToId matches the subject (direct match only)
            // - The resource RefId matches the requested resource
            var clientDelegationResults = await _dbContext.DelegationResources
                .AsNoTracking()
                .Include(dr => dr.Delegation)
                    .ThenInclude(d => d.From)
                        .ThenInclude(a => a.From)
                .Include(dr => dr.Delegation)
                    .ThenInclude(d => d.To)
                        .ThenInclude(a => a.To)
                .Include(dr => dr.AssignmentResource)
                .Include(dr => dr.Resource)
                .Where(dr => offeringEntityIds.Contains(dr.Delegation.From.FromId))
                .Where(dr => dr.Delegation.To.ToId == subjectEntityId.Value)
                .Where(dr => dr.Resource.RefId == resourceRefId)
                .ToListAsync(cancellationToken);

            // Map to DelegationChange objects
            return clientDelegationResults.Select(dr =>
            {
                var fromEntity = dr.Delegation.From.From;
                var toEntity = dr.Delegation.To.To;
                var toUuidType = MapEntityTypeToUuidType(toEntity.TypeId);

                var result = new DelegationChange
                {
                    ResourceId = dr.Resource.RefId,
                    DelegationChangeType = DelegationChangeType.Grant,
                    BlobStoragePolicyPath = dr.AssignmentResource.PolicyPath,
                    BlobStorageVersionId = dr.AssignmentResource.PolicyVersion,
                    OfferedByPartyId = fromEntity.PartyId ?? 0,
                    FromUuid = fromEntity.Id,
                    FromUuidType = MapEntityTypeToUuidType(fromEntity.TypeId),
                    ToUuid = toEntity.Id,
                    ToUuidType = toUuidType,
                };

                if (toUuidType == UuidType.Person)
                {
                    result.CoveredByUserId = toEntity.UserId;
                }
                else if (toUuidType == UuidType.Organization)
                {
                    result.CoveredByPartyId = toEntity.PartyId;
                }

                return result;
            }).ToList();
        }

        private static UuidType MapEntityTypeToUuidType(Guid entityTypeId)
        {
            if (entityTypeId == EntityTypeConstants.Person)
            {
                return UuidType.Person;
            }

            if (entityTypeId == EntityTypeConstants.Organization)
            {
                return UuidType.Organization;
            }

            if (entityTypeId == EntityTypeConstants.SystemUser)
            {
                return UuidType.SystemUser;
            }

            return UuidType.NotSpecified;
        }

        private static List<Rule> GetRulesFromPolicyAndDelegationChange(ICollection<XacmlRule> xacmlRules, DelegationChange delegationChange)
        {
            List<Rule> rules = new List<Rule>();
            foreach (XacmlRule xacmlRule in xacmlRules)
            {
                if (xacmlRule.Effect.Equals(XacmlEffectType.Permit) && xacmlRule.Target != null)
                {
                    Rule rule = new Rule
                    {
                        RuleId = xacmlRule.RuleId,
                        OfferedByPartyId = delegationChange.OfferedByPartyId,
                        DelegatedByUserId = delegationChange.PerformedByUserId,
                        CoveredBy = new List<AttributeMatch>(),
                        Resource = new List<AttributeMatch>()
                    };
                    AddAttributeMatchesToRule(xacmlRule.Target, rule);
                    rules.Add(rule);
                }
            }

            return rules;
        }

        private static void AddAttributeMatchesToRule(XacmlTarget xacmlTarget, Rule rule)
        {
            foreach (XacmlAnyOf anyOf in xacmlTarget.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    foreach (XacmlMatch xacmlMatch in allOf.Matches)
                    {
                        AddAttributeMatchToRule(xacmlMatch, rule);
                    }
                }
            }
        }

        private static void AddAttributeMatchToRule(XacmlMatch xacmlMatch, Rule rule)
        {
            if (xacmlMatch.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Action))
            {
                rule.Action = new AttributeMatch
                {
                    Id = xacmlMatch.AttributeDesignator.AttributeId.OriginalString,
                    Value = xacmlMatch.AttributeValue.Value
                };
            }

            if (xacmlMatch.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Subject))
            {
                rule.CoveredBy.Add(new AttributeMatch
                {
                    Id = xacmlMatch.AttributeDesignator.AttributeId.OriginalString,
                    Value = xacmlMatch.AttributeValue.Value
                });
            }

            if (xacmlMatch.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Resource))
            {
                rule.Resource.Add(new AttributeMatch
                {
                    Id = xacmlMatch.AttributeDesignator.AttributeId.OriginalString,
                    Value = xacmlMatch.AttributeValue.Value
                });
            }
        }

        private static void EnrichRightsDictionaryWithRightsFromPolicy(Dictionary<string, Right> rights, XacmlPolicy policy, RightSourceType policySourceType, List<AttributeMatch> subjectMatches, int minimumAuthenticationLevel = 0, int delegationOfferedByPartyId = 0, bool returnAllPolicyRights = false, bool getDelegableRights = false)
        {
            PolicyDecisionPoint pdp = new PolicyDecisionPoint();

            foreach (XacmlRule rule in policy.Rules)
            {
                XacmlPolicy singleRulePolicy = new XacmlPolicy(new Uri($"{policy.PolicyId}_{rule.RuleId}"), policy.RuleCombiningAlgId, policy.Target);
                singleRulePolicy.Rules.Add(rule);

                List<List<PolicyAttributeMatch>> ruleSubjects = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Subject);
                ICollection<Right> ruleRights = PolicyHelper.GetRightsFromXacmlRules(rule.SingleToList());
                foreach (Right ruleRight in ruleRights)
                {
                    ICollection<XacmlContextAttributes> contextAttributes = PolicyHelper.GetContextAttributes(subjectMatches, ruleRight.Resource, ruleRight.Action.SingleToList());
                    XacmlContextRequest authRequest = new XacmlContextRequest(false, false, contextAttributes);

                    XacmlContextResponse response = pdp.Authorize(authRequest, singleRulePolicy);
                    XacmlContextResult decisionResult = response.Results.First();

                    // If getting rights for delegation, the right source is a delegation policy and the right does no longer exist in the app/resource policy: it should NOT be added as a delegable right
                    if (getDelegableRights && policySourceType == RightSourceType.DelegationPolicy && !rights.ContainsKey(ruleRight.RightKey))
                    {
                        continue;
                    }

                    if (!rights.TryGetValue(ruleRight.RightKey, out Right right))
                    {
                        rights.Add(ruleRight.RightKey, ruleRight);
                        right = ruleRight;
                    }

                    // If getting rights for delegation, the xacml decision is to be used for indicating if the user can delegate the right. Otherwise the decision indicate whether the user actually have the right.
                    if (getDelegableRights)
                    {
                        right.CanDelegate = (right.CanDelegate.HasValue && right.CanDelegate.Value) || decisionResult.Decision.Equals(XacmlContextDecision.Permit);
                    }
                    else
                    {
                        right.HasPermit = (right.HasPermit.HasValue && right.HasPermit.Value) || decisionResult.Decision.Equals(XacmlContextDecision.Permit);
                    }

                    if (decisionResult.Decision.Equals(XacmlContextDecision.Permit) || returnAllPolicyRights)
                    {
                        right.RightSources.Add(
                            new RightSource
                            {
                                PolicyId = policy.PolicyId.OriginalString,
                                PolicyVersion = policy.Version,
                                RuleId = rule.RuleId,
                                RightSourceType = policySourceType,
                                HasPermit = getDelegableRights ? null : decisionResult.Decision.Equals(XacmlContextDecision.Permit),
                                CanDelegate = getDelegableRights ? decisionResult.Decision.Equals(XacmlContextDecision.Permit) : null,
                                MinimumAuthenticationLevel = minimumAuthenticationLevel,
                                OfferedByPartyId = delegationOfferedByPartyId,
                                UserSubjects = subjectMatches,
                                PolicySubjects = ruleSubjects
                            });
                    }
                }
            }
        }
    }
}
