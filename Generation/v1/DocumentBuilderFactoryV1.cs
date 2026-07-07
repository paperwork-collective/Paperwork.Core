using Paperwork.Generation;
using Paperwork.Generation.v1;

namespace Paperwork.Generation.v1;

public class DocumentBuilderFactoryV1 : IDocumentBuilderFactory
{
    public IDocumentBuilder CreateDocumentBuilder(IPaperworkFactory factory)
    {
        return new DocumentBuilderV1(factory);
    }

    public IDocumentBuilder CreateDocumentBuilder(IPaperworkFactory factory, TemplateDefinition definition)
    {
        var defnV1 = (TemplateDefinitionV1) definition;
        return new DocumentBuilderV1(factory, defnV1);
    }
}