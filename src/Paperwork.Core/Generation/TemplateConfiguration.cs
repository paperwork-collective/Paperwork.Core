using Paperwork.Generation;

namespace Paperwork.Generation;

public abstract class TemplateConfiguration
{
    
    public Version Version { get; set; }

    public TemplateConfiguration(Version version)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public abstract TemplateMetaData GetMetaData();

    public abstract TemplateDefinition GetDefinition();
    
}

public class TemplateMetaData
{

    public virtual string Title { get; set; }
    
    public virtual string Description { get; set; }
    
    public virtual string Author { get; set; }
    
    public virtual DateTime Published { get; set; }
    
    public virtual string Publisher { get; set; }
    
    public virtual string PublishedVersion { get; set; }
    
    public TemplateMetaData()
    {}
}