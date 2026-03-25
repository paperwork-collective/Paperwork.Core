using System;
namespace Paperwork.Services
{
    public enum PaperworkGenerationStage
    {
        None = 0,
        ConfigLoading = 1,
        TemplateParsing = 2,
        DocumentLoading = 3,
        DataBinding = 4,
        LayingOutPages = 5,
        RenderingDocument = 6,
        Finished = 7
    }
}

