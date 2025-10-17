namespace Katana.Core.DTOs
{
    public class CreateMappingRequestDto
    {
        public string MappingType { get; set; } = string.Empty;
        public string SourceValue { get; set; } = string.Empty;
        public string TargetValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateMappingRequestDto
    {
        public string? TargetValue { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
