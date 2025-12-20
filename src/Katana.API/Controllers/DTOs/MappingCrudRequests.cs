namespace Katana.API.Controllers.DTOs;

public class BaseMappingRequest
{
    public string? TargetValue { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateMappingRequest : BaseMappingRequest
{
    public string MappingType { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
}

public class UpdateMappingRequest : BaseMappingRequest
{
}

