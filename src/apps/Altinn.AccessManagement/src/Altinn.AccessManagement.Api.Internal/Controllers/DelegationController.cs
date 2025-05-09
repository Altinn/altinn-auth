using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Internal.Controllers
{
    /// <summary>
    /// Delegations
    /// </summary>
    [Route("/accessmanagement/api/v1/delegations")]
    [ApiController]
    public class DelegationController(
        IDelegationRepository delegationRepository,
        IDelegationPackageRepository delegationPackageRepository,
        IDelegationResourceRepository delegationResourceRepository,
        IAssignmentRepository assignmentRepository,
        IConnectionRepository connectionRepository,
        IEntityRepository entityRepository
        ) : ControllerBase
    {
        private readonly IDelegationRepository delegationRepository = delegationRepository;
        private readonly IDelegationPackageRepository delegationPackageRepository = delegationPackageRepository;
        private readonly IDelegationResourceRepository delegationResourceRepository = delegationResourceRepository;
        private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
        private readonly IConnectionRepository connectionRepository = connectionRepository;
        private readonly IEntityRepository entityRepository = entityRepository;

        /// <summary>
        /// Create a new assignment
        /// </summary>
        [Route("{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtDelegation>> Get(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            /*
            [X] User is to or TS for from
            */

            var delegation = await delegationRepository.GetExtended(id);
            if (delegation == null)
            {
                return NotFound();
            }

            var userEntity = await entityRepository.Get(userId);

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, delegation.FacilitatorId);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:hovedadministrator"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, delegation.FacilitatorId.ToString()));
            }

            return Ok(delegation);
        }

        /// <summary>
        /// Add package to delegation
        /// </summary>
        [Route("")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Post([FromBody] CreateDelegationRequestDto request)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            var fromAssignment = await assignmentRepository.Get(request.FromAssignmentId);
            var toAssignment = await assignmentRepository.Get(request.ToAssignmentId);

            if (fromAssignment == null || toAssignment == null)
            {
                // Assignments must exist
                return BadRequest();
            }

            if (!fromAssignment.ToId.Equals(toAssignment.FromId))
            {
                // Assignments must have a link
                return BadRequest();
            }

            var filter = delegationRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, fromAssignment.Id);
            filter.Equal(t => t.ToId, toAssignment.Id);
            filter.Equal(t => t.FacilitatorId, fromAssignment.ToId);
            var existsRes = (await delegationRepository.Get(filter)).FirstOrDefault();
            if (existsRes != null)
            {
                // Allready exists
                return Ok(); // 302 Found?
            }

            var res = await delegationRepository.Create(
                new Delegation()
                {
                    FromId = request.FromAssignmentId,
                    ToId = request.ToAssignmentId,
                    FacilitatorId = Guid.Empty
                }, 
                options: options
            );

            if (res > 0)
            {
                // Created
                return Created();
            }

            return Problem();
        }

        /// <summary>
        /// Remove package from delegation
        /// </summary>
        [Route("{id}")]
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            var userEntity = await entityRepository.Get(userId);
            var assignment = await assignmentRepository.Get(id);

            if (userEntity == null || assignment == null)
            {
                return BadRequest();
            }

            /*
            [X] User must have role:TS on Facilitator
            */

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, id);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:tilgangsstyrer"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, assignment.FromId.ToString()));
            }

            try
            {
                var res = await delegationRepository.Delete(id, options: options);
                if (res > 0)
                {
                    return NoContent();
                }

                throw new Exception("Unable to delete delegation");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Add package to delegation
        /// </summary>
        [Route("{id}/packages")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<Package>> GetPackages(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            /*
            - [ ] User must have role:TS
            */

            try
            {
                var res = await delegationPackageRepository.GetB(id);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Add package to delegation
        /// </summary>
        [Route("{id}/package/{packageId}")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> AddPackage(Guid id, Guid packageId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            /*
            
            - User must have role:TS on Facilitator
            - Package must exist in /connection/{assignment_id}/packages
            - Package must be delegable

            */

            try
            {
                var res = await delegationPackageRepository.Create(new DelegationPackage() { DelegationId = id, PackageId = packageId }, options);
                if (res > 0)
                {
                    return Created();
                }

                throw new Exception("Unable to add package to delegation");
            }
            catch (Exception ex) 
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Remove package from delegation
        /// </summary>
        [Route("{id}/package/{packageId}")]
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> RemovePackage(Guid id, Guid packageId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            /*

           - User must have role:TS on Facilitator
           - Package should exist in /connection/{delegation_id}/packages

           */

            try
            {
                var deleteFilter = delegationPackageRepository.CreateFilterBuilder();
                deleteFilter.Equal(t => t.DelegationId, id);
                deleteFilter.Equal(t => t.PackageId, packageId);
                var res = await delegationPackageRepository.Delete(deleteFilter, options);

                if (res > 0)
                {
                    return NoContent();
                }

                throw new Exception("Unable to remove package from delegation");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Add package to delegation
        /// </summary>
        [Route("{id}/resources")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<Resource>> GetResources(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            /*
            - [ ] User must have role:TS
            */

            try
            {
                var res = await delegationResourceRepository.GetB(id);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Add package to delegation
        /// </summary>
        [Route("{id}/resource/{resourceId}")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> AddResource(Guid id, Guid resourceId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            /*
            
            - User must have role:TS on Facilitator
            - Package must exist in /connection/{assignment_id}/packages
            - Package must be delegable

            */

            try
            {
                var res = await delegationResourceRepository.Create(new DelegationResource() { DelegationId = id, ResourceId = resourceId }, options);
                if (res > 0)
                {
                    return Created();
                }

                throw new Exception("Unable to add resource to delegation");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Remove package from delegation
        /// </summary>
        [Route("{id}/resource/{resourceId}")]
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> RemoveResource(Guid id, Guid resourceId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var options = new ChangeRequestOptions()
            {
                ChangedBy = userId,
                ChangedBySystem = AuditDefaults.EnduserApi
            };

            /*

           - User must have role:TS on Facilitator
           - Package should exist in /connection/{delegation_id}/packages

           */

            try
            {
                var deleteFilter = delegationResourceRepository.CreateFilterBuilder();
                deleteFilter.Equal(t => t.DelegationId, id);
                deleteFilter.Equal(t => t.ResourceId, resourceId);
                var res = await delegationResourceRepository.Delete(deleteFilter, options);

                if (res > 0)
                {
                    return NoContent();
                }

                throw new Exception("Unable to remove resource from delegation");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}

/// <summary>
/// RequestDto for CreateDelegation
/// </summary>
public class CreateDelegationRequestDto
{
    /// <summary>
    /// Assignment identifier (From)
    /// </summary>
    public Guid FromAssignmentId { get; set; }

    /// <summary>
    /// Assignment identifier (To)
    /// </summary>
    public Guid ToAssignmentId { get; set; }
}
