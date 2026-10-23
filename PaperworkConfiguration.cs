using Microsoft.Extensions.Configuration;

namespace Paperwork
{
    /// <summary>
    /// Passes application configuration to the document engine. The engine reads the
    /// "Scryber" section; Paperwork.Core also reads a "Paperwork" section with the same
    /// structure (Fonts, Imaging, Parsing, Output, Tracing...), so either can be used.
    /// </summary>
    public static class PaperworkConfiguration
    {
        /// <summary>The Paperwork configuration section name.</summary>
        public const string SectionName = "Paperwork";

        /// <summary>The configuration section name read by the document engine.</summary>
        public const string EngineSectionName = "Scryber";

        /// <summary>
        /// Passes the configuration to the document engine, once at startup. The "Scryber"
        /// section is read first, then the "Paperwork" section, so a value in "Paperwork"
        /// replaces the same value in "Scryber". Lists are matched by position, so keep
        /// each list (e.g. Fonts:Register) in one section.
        /// </summary>
        public static void Init(IConfiguration configuration)
        {
            if (null == configuration)
                throw new ArgumentNullException(nameof(configuration));

            var paperwork = configuration.GetSection(SectionName);

            if (!paperwork.Exists())
            {
                Scryber.ServiceProvider.Init(configuration);
                return;
            }

            // Copy the Paperwork values under the engine's section name, after the
            // original configuration, so they take precedence.
            var values = paperwork.AsEnumerable(makePathsRelative: true)
                .Where(kv => null != kv.Value)
                .Select(kv => new KeyValuePair<string, string?>(EngineSectionName + ConfigurationPath.KeyDelimiter + kv.Key, kv.Value));

            var combined = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddInMemoryCollection(values)
                .Build();

            Scryber.ServiceProvider.Init(combined);
        }
    }
}
