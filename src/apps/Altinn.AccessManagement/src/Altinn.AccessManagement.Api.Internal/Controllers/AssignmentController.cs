using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Internal.Controllers
{
    /// <summary>
    /// Assignments
    /// </summary>
    [Route("/accessmanagement/api/v1/assignments")]
    [ApiController]
    public class AssignmentController(
        IConnectionRepository connectionRepository,
        IConnectionPackageRepository connectionPackageRepository,
        IConnectionResourceRepository connectionResourceRepository,
        IPackageRepository packageRepository,
        IResourceRepository resourceRepository,
        IEntityRepository entityRepository,
        IAssignmentRepository assignmentRepository,
        IAssignmentPackageRepository assignmentPackageRepository,
        IAssignmentResourceRepository assignmentResourceRepository,
        IProviderRepository providerRepository,
        IRoleRepository roleRepository
        ) : ControllerBase
    {
        private readonly IConnectionRepository connectionRepository = connectionRepository;
        private readonly IConnectionPackageRepository connectionPackageRepository = connectionPackageRepository;
        private readonly IConnectionResourceRepository connectionResourceRepository = connectionResourceRepository;
        private readonly IPackageRepository packageRepository = packageRepository;
        private readonly IResourceRepository resourceRepository = resourceRepository;
        private readonly IEntityRepository entityRepository = entityRepository;
        private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
        private readonly IAssignmentPackageRepository assignmentPackageRepository = assignmentPackageRepository;
        private readonly IAssignmentResourceRepository assignmentResourceRepository = assignmentResourceRepository;
        private readonly IProviderRepository providerRepository = providerRepository;
        private readonly IRoleRepository roleRepository = roleRepository;

        /// <summary>
        /// Create a new assignment
        /// </summary>
        [Route("{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtAssignment>> Get(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            /*
            [X] User is to or TS for from
            */

            var assignment = await assignmentRepository.GetExtended(id);
            if (assignment == null)
            {
                return NotFound();
            }

            var userEntity = await entityRepository.Get(userId);

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, assignment.FromId);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:hovedadministrator"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, assignment.FromId.ToString()));
            }

            return Ok(assignment);
        }

        /// <summary>
        /// Create a new assignment
        /// From+To+Role
        /// </summary>
        [Route("")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Post([FromBody] CreatAssignmentRequestDto assignment)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var userEntity = await entityRepository.Get(userId);
            var fromEntity = await entityRepository.Get(assignment.FromEntityId);
            var toEntity = await entityRepository.Get(assignment.ToEntityId);
            var role = await roleRepository.Get(assignment.RoleId);

            if (userEntity == null || fromEntity == null || toEntity == null || role == null)
            {
                return BadRequest();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, fromEntity.Id);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:hovedadministrator"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, assignment.FromEntityId.ToString()));
            }

            await assignmentRepository.Create(new Assignment()
            {
                Id = Guid.CreateVersion7(),
                FromId = fromEntity.Id,
                ToId = toEntity.Id,
                RoleId = role.Id,
            });

            return Created();
        }

        /// <summary>
        /// Delete assignment
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

            var digdirProvider = (await providerRepository.Get(t => t.Name, "Digitaliseringsdirektoratet")).FirstOrDefault();
            if (digdirProvider == null)
            {
                return Problem("Unable to find provider");
            }

            var assignment = await assignmentRepository.GetExtended(id);
            if (assignment.Role.ProviderId != digdirProvider.Id)
            {
                return Problem("Not allowed to delete this assignment");
            }

            await assignmentRepository.Delete(id);
            return Ok();
        }

        /// <summary>
        /// Get direct packages for this assignment
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
            [X] Assignment exists
            [X] User is TS or User is ToId
            */

            var assignment = await assignmentRepository.Get(id);
            if (assignment == null)
            {
                return BadRequest();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, assignment.FromId);
            filter.Equal(t => t.ToId, userId);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:tilgangsstyrer"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, id.ToString()));
            }

            return Ok(await assignmentPackageRepository.GetB(id));
        }

        /// <summary>
        /// Get direct packages for this assignment
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
            [X] Assignment exists
            [X] User is TS or User is ToId
            */

            var assignment = await assignmentRepository.Get(id);
            if (assignment == null)
            {
                return BadRequest();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, assignment.FromId);
            filter.Equal(t => t.ToId, userId);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:tilgangsstyrer"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, id.ToString()));
            }

            return Ok(await assignmentResourceRepository.GetB(id));
        }

        /// <summary>
        /// Alle relasjoner hvor {id} er involvert i from, to eller facilitator.
        /// </summary>
        [Route("{id}/packages/{packageId}")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> AddPackage(Guid id, Guid packageId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var userEntity = await entityRepository.Get(userId);
            var assignment = await assignmentRepository.Get(id);
            var package = await packageRepository.Get(packageId);

            if (userEntity == null || assignment == null || package == null)
            {
                return BadRequest();
            }

            if (!package.IsAssignable)
            {
                return Problem("Package is not available for assignment");
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, assignment.FromId);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:tilgangsstyrer"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, assignment.FromId.ToString()));
            }

            var availablePackages = new List<Package>();
            foreach (var con in connections)
            {
                availablePackages.AddRange(await connectionPackageRepository.GetB(con.Id));
            }

            if (availablePackages.Count(t => t.Id == package.Id) == 0)
            {
                return Problem("USer dows not have the package available for assignment");
            }

            var assPckFilter = assignmentPackageRepository.CreateFilterBuilder();
            assPckFilter.Equal(t => t.AssignmentId, assignment.Id);
            assPckFilter.Equal(t => t.PackageId, package.Id);
            var assPck = await assignmentPackageRepository.Get(assPckFilter);
            if (assPck != null && assPck.Any())
            {
                return Created(); // Allready exists
            }

            /*
            
            - [x] User exists
            - [x] User is TS
            - [x] Package exists
            - [x] User has package
            - [x] Assignment exists
            - [x] AssignmentPackage does not exist (duplicate)

            */

            var dp = new AssignmentPackage()
            {
                Id = Guid.NewGuid(),
                AssignmentId = id,
                PackageId = packageId
            };

            var res = await assignmentPackageRepository.CreateCross(assignment.Id, package.Id);
            if (res == 1)
            {
                return Created();
            }

            return Problem("Unable to add package to assignment");

        }

        /// <summary>
        /// Alle relasjoner hvor {id} er involvert i from, to eller facilitator.
        /// </summary>
        [Route("{id}/resources/{resourceId}")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> AddResource(Guid id, Guid resourceId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var userEntity = await entityRepository.Get(userId);
            var assignment = await assignmentRepository.Get(id);
            var resource = await resourceRepository.Get(resourceId);

            if (userEntity == null || assignment == null || resource == null)
            {
                return BadRequest();
            }

            /*
             * Not yet available
            if (!resource.IsAssignable)
            {
                return Problem("Resource is not available for assignment");
            }
            */

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, assignment.FromId);
            filter.Equal(t => t.ToId, userEntity.Id);
            var connections = await connectionRepository.GetExtended(filter);

            string roleUrn = "urn:altinn:role:tilgangsstyrer"; // Or something else?
            if (connections.Count(t => t.Role.Urn == roleUrn) == 0)
            {
                return Unauthorized(string.Format("User '{0}' is missing role '{1}' on '{2}'", userId, roleUrn, assignment.FromId.ToString()));
            }

            var availableResources = new List<Resource>();
            foreach (var con in connections)
            {
                availableResources.AddRange(await connectionResourceRepository.GetB(con.Id));
            }

            if (availableResources.Count(t => t.Id == resource.Id) == 0)
            {
                return Problem("User does not have the resource available for assignment");
            }

            var assResFilter = assignmentResourceRepository.CreateFilterBuilder();
            assResFilter.Equal(t => t.AssignmentId, assignment.Id);
            assResFilter.Equal(t => t.ResourceId, resource.Id);
            var assPck = await assignmentPackageRepository.Get(assResFilter);
            if (assPck != null && assPck.Any())
            {
                return Created(); // Allready exists
            }

            /*
            
            - [x] User exists
            - [x] User is TS
            - [x] Resource exists
            - [x] User has resource
            - [x] Assignment exists
            - [x] AssignmentResource does not exist (duplicate)

            */

            var dp = new AssignmentResource()
            {
                Id = Guid.CreateVersion7(),
                AssignmentId = id,
                ResourceId = resourceId
            };

            var res = await assignmentResourceRepository.CreateCross(assignment.Id, resource.Id);
            if (res == 1)
            {
                return Created();
            }

            return Problem("Unable to add package to assignment");

        }

        /// <summary>
        /// Alle relasjoner hvor {id} er involvert i from, to eller facilitator.
        /// </summary>
        [Route("{id}/packages/{packageId}")]
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> RemovePackage(Guid id, Guid packageId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var userEntity = await entityRepository.Get(userId);

            // If user has access

            var assignment = await assignmentRepository.Get(id);
            var package = await packageRepository.Get(packageId);

            if (userEntity == null || assignment == null || package == null)
            {
                return BadRequest();
            }

            await assignmentPackageRepository.DeleteCross(id, packageId);

            return Ok();
        }

        /// <summary>
        /// Alle relasjoner hvor {id} er involvert i from, to eller facilitator.
        /// </summary>
        [Route("{id}/resources/{resourceId}")]
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> RemoveResource(Guid id, Guid resourceId)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var userEntity = await entityRepository.Get(userId);
            
            // If user has access
            
            var assignment = await assignmentRepository.Get(id);
            var resource = await resourceRepository.Get(resourceId);

            if (userEntity == null || assignment == null || resource == null)
            {
                return BadRequest();
            }

            await assignmentPackageRepository.DeleteCross(id, resourceId);

            return Ok();
        }
    }
}

/// <summary>
/// RequestDto for CreateAssignment
/// </summary>
public class CreatAssignmentRequestDto
{
    /// <summary>
    /// Party-Entity identifier (From)
    /// </summary>
    public Guid FromEntityId { get; set; }

    /// <summary>
    /// Party-Entity identifier (To)
    /// </summary>
    public Guid ToEntityId { get; set; }

    /// <summary>
    /// Role identifier
    /// </summary>
    public Guid RoleId { get; set; }
}
