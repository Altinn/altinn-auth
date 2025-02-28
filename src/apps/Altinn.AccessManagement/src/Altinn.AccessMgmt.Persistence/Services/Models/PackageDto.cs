namespace Altinn.AccessMgmt.Persistence.Services.Models;

public class AreaGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Urn { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } //EntityType
    public List<AreaDto> Areas { get; set; }
}

public class AreaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Urn { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public List<PackageDto> Packages { get; set; }
}

public class PackageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Urn { get; set; }
    public string Description { get; set; }
}
