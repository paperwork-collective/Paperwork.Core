using Paperwork.Services;

namespace Paperwork
{
    /// <summary>
    /// Extension methods for <see cref="IPaperworkFactory"/>.
    /// </summary>
    public static class PaperworkFactoryExtensions
    {
        /// <summary>
        /// Returns a fluent <see cref="IDocumentBuilder"/> bound to this factory.
        /// </summary>
        public static IDocumentBuilder NewDocument(this IPaperworkFactory factory)
            => new DocumentBuilder(factory);
    }
}
