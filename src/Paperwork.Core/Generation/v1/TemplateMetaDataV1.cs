using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paperwork.Generation.v1;

public class TemplateMetaDataV1 : TemplateMetaData
{

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
    
    [JsonPropertyName("title")]
    public override string Title { get; set; } 
    
    [JsonPropertyName("desc")]
    public override string Description { get; set; }
    
    [JsonPropertyName("author")]
    public override string Author { get; set; }
    
    [JsonPropertyName("publishedOn")]
    public override DateTime Published { get; set; }
    
    [JsonPropertyName("publishedBy")]
    public override string Publisher { get; set; }
    
    [JsonPropertyName("publishedVersion")]
    public override string PublishedVersion { get; set; }
    
    public  TemplateMetaDataV1()
    {
        ExtensionData = new Dictionary<string, JsonElement>();
    }
}