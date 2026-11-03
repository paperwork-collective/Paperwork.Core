using Paperwork.Generation.v1;
using Paperwork.Generation;

namespace Paperwork;

public interface IDocumentBuilderFactory
{
    IDocumentBuilder CreateDocumentBuilder(IPaperworkFactory factory);
    
    IDocumentBuilder CreateDocumentBuilder(IPaperworkFactory factory, TemplateDefinition definition);
}