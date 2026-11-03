using System.Text.Json.Serialization;

namespace Paperwork.Generation.v1;

public class TemplateConfigurationV1() : TemplateConfiguration(new Version(1, 0))
{
    [JsonPropertyName("meta")]
    public TemplateMetaDataV1 MetaData { get; set; }
    
    [JsonPropertyName("template")]
    public TemplateDefinitionV1 Definition { get; set; }
    
    //
    // base class overrides
    //
    
    public override TemplateDefinition GetDefinition()
    {
        return this.Definition;
    }

    public override TemplateMetaData GetMetaData()
    {
        return this.MetaData;
    }
}
