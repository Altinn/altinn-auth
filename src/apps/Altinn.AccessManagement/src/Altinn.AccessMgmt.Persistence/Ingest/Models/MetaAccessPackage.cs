namespace Altinn.AccessMgmt.Repo.Ingest.Models
{
    /// <summary>
    /// Access Packages
    /// </summary>
    public class MetaAccessPackage : MetaPackage
    {
        /// <summary>
        /// Area group
        /// </summary>
        public string AreaGroup { get; set; }

        /// <summary>
        /// Area
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// Has Resources
        /// </summary>
        public bool HasResources { get; set; }
    }

    /// <summary>
    /// Area Group
    /// </summary>
    public class MetaAreaGroup
    {
        /// <summary>
        /// Identity
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Urn
        /// </summary>
        public string Urn { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type (e.g. Organization, Person)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Areas
        /// </summary>
        public List<MetaArea> Areas { get; set; }
    }

    /// <summary>
    /// Area
    /// </summary>
    public class MetaArea
    {
        /// <summary>
        /// Identity
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Urn
        /// </summary>
        public string Urn { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon ref
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Area group
        /// </summary>
        public string AreaGroup { get; set; }

        /// <summary>
        /// Packages
        /// </summary>
        public List<MetaPackage> Packages { get; set; }
    }

    /// <summary>
    /// MetaPackage
    /// </summary>
    public class MetaPackage
    {
        /// <summary>
        /// Identity
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Urn
        /// </summary>
        public string Urn { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Area
        /// </summary>
        public string Area { get; set; }
    }

    public class MetaRolePackage : MetaPackage
    {
        public string Tilgangspakke { get; set; }

        public List<string> Enhetsregisterroller { get; set; }

        public bool Delegerbar { get; set; }

        public bool HarTilgang { get; set; }
    }
}
