using Scryber.Components;

namespace Paperwork.Generation
{
    /// <summary>
    /// The document information Paperwork applies to every document it generates,
    /// unless the template has set its own.
    /// </summary>
    public static class PaperworkDocumentDefaults
    {
        /// <summary>The default Creator and Producer for a generated document.</summary>
        public const string GeneratorName = "Paperwork Document Generator";

        // What Scryber itself puts in Creator and Producer when a template says nothing.
        private static readonly string ScryberCreator = new DocumentInfo().Creator;
        private static readonly string ScryberProducer = new DocumentInfo().Producer;

        /// <summary>
        /// Sets the Creator and Producer to <see cref="GeneratorName"/> where they are
        /// empty or still Scryber's own default - a value set by the template is kept.
        /// </summary>
        public static void Apply(Document doc)
        {
            if (null == doc)
                return;

            var info = doc.Info;
            if (null == info)
                doc.Info = info = new DocumentInfo();

            if (string.IsNullOrEmpty(info.Creator) || info.Creator == ScryberCreator)
                info.Creator = GeneratorName;

            if (string.IsNullOrEmpty(info.Producer) || info.Producer == ScryberProducer)
                info.Producer = GeneratorName;
        }
    }
}
