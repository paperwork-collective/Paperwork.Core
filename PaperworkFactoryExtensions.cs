using System.Text.Json;
using System.Text.Json.Nodes;
using Paperwork.Generation;
using Paperwork.Generation.v1;

namespace Paperwork
{
    /// <summary>
    /// Extension methods for <see cref="IPaperworkFactory"/> and <see cref="PaperworkFactory"/>
    /// </summary>
    public static class PaperworkFactoryExtensions
    {
        /// IPaperworkFactory extensions
        
        /// <summary>
        /// Returns a fluent <see cref="IDocumentBuilder"/> bound to this factory.
        /// </summary>
        public static IDocumentBuilder NewDocument(this IPaperworkFactory factory)
            => new DocumentBuilderV1(factory);

        /// <summary>
        /// Returns a fluent <see cref="IDocumentBuilder"/> initialized with the configuration provided
        /// </summary>
        /// <param name="defn">The configuration to initialize the document with</param>
        /// <returns>A document builder</returns>
        /// <exception cref="InvalidCastException"></exception>
        public static IDocumentBuilder FromDefinition(this IPaperworkFactory factory, TemplateDefinition defn)
        {
            var defnV1 = (defn as TemplateDefinitionV1) ?? throw new InvalidCastException("Could not convert the base to a V1 configuration");
            return new DocumentBuilderV1(factory, defnV1);
        }
        
        /// <summary>
        /// Returns a fluent <see cref="IDocumentBuilder"/> initialized with the template definition loaded from the JSON content as a stream
        /// A definition is a single object describing a template { data: [...], layouts: [...], styles [....] }
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="defn"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static IDocumentBuilder FromDefinition(this IPaperworkFactory factory, Stream defn, int majorVers = 1, int minorVers = 1)
        {
            using var reader = new StreamReader(defn, leaveOpen: true);
            
            if(majorVers > 1)
                throw new NotSupportedException("Only supports version 1.0 and 1.1");
            if(minorVers > 1)
                throw new NotSupportedException("Only supports version 1.0 and 1.1");
            
            var template = JsonSerializer.Deserialize<TemplateDefinitionV1>(reader.ReadToEnd());
            if(null == template)
                throw new InvalidCastException("Could not convert the stream to a V1 configuration");
            
            return FromDefinition(factory, template);
            
            
        }
        
        /// <summary>
        /// Returns a fluent <see cref="IDocumentBuilder"/> initialized with the configuration loaded from the JSON content as a stream 
        /// A configuration is a self-contained template description, including the schema version, its metadata, and the actual template content. 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static IDocumentBuilder FromConfig(this IPaperworkFactory factory, Stream config)
        {
            using var reader = new StreamReader(config, leaveOpen: true);
            var all = reader.ReadToEnd();
            
            var instance = (PaperworkInstanceFactory)factory;
            JsonDocumentOptions options = new JsonDocumentOptions();
            var serializer = instance.SerializerOptions ?? JsonSerializerOptions.Default;
            
            options.AllowTrailingCommas = serializer.AllowTrailingCommas;
            options.CommentHandling = serializer.ReadCommentHandling;
            options.MaxDepth = serializer.MaxDepth;
            
            var decoded = JsonDocument.Parse(all, options);

            JsonElement vers;
            if(!decoded.RootElement.TryGetProperty("schemaVers", out vers))
                throw new InvalidOperationException("The required schema version was not found in the configuration file");

            if(vers.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("The required schema version was not found in the configuration file");
            
            if(vers.GetString() != "1.1")
                throw new InvalidOperationException("The required schema version must be 1.1");
            
            var templateConfig = JsonSerializer.Deserialize<TemplateConfigurationV1>(all);
            
            if(null == templateConfig)
                throw new InvalidCastException("Could not convert the stream to a V1 configuration");
            
            return FromConfig(factory, templateConfig);
            
            
        }

        public static IDocumentBuilder FromConfig(this IPaperworkFactory factory, TemplateConfiguration templateConfig)
        {
            if(null == templateConfig)
                throw new ArgumentNullException(nameof(templateConfig));
            
            var templatedefn = templateConfig.GetDefinition();
            if(null == templatedefn)
                throw new NullReferenceException("No template definition found");
            
            return FromDefinition(factory, templatedefn);
        }
        
    }
}
