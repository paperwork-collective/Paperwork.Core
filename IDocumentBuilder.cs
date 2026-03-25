using Paperwork.Services.Generation;
using Scryber;
using Scryber.Components;
using Scryber.Styles;

namespace Paperwork
{
    /// <summary>
    /// Fluent builder for constructing and generating a Paperwork PDF document.
    /// Use <see cref="IPaperworkFactory.NewDocument"/> to obtain an instance.
    /// </summary>
    public interface IDocumentBuilder
    {
        // ── Layout ────────────────────────────────────────────────────────────

        /// <summary>Adds an inline HTML/XHTML layout string.</summary>
        IDocumentBuilder WithLayout(string htmlContent, string name = "main");

        /// <summary>Reads a layout from a stream.</summary>
        IDocumentBuilder WithLayout(Stream stream, string name = "main");

        /// <summary>
        /// Adds a named Scryber component. If <paramref name="name"/> matches the main layout name
        /// (default <c>"main"</c>) and the component is a <see cref="Document"/>, it is used as
        /// the root document. All named components are exposed as <c>_templates</c> in the document
        /// params, so the main template can reference partials via
        /// <c>{{_templates["myPartial"]}}</c>.
        /// </summary>
        IDocumentBuilder WithLayout(IComponent component, string name = "main");

        /// <summary>Reads a layout from the local filesystem.</summary>
        IDocumentBuilder WithLayoutFile(string filePath, string name = "main");

        /// <summary>Fetches a layout from a remote URL at generation time.</summary>
        IDocumentBuilder WithLayoutUrl(string url, string name = "main", string? auth = null);

        // ── Styles ────────────────────────────────────────────────────────────

        /// <summary>Adds an inline CSS string.</summary>
        IDocumentBuilder WithStyle(string cssContent, string? name = null, string? format = null);

        /// <summary>Reads CSS from a stream.</summary>
        IDocumentBuilder WithStyle(Stream stream, string? name = null);

        /// <summary>Injects a pre-built Scryber <see cref="StyleGroup"/> directly into the document.</summary>
        IDocumentBuilder WithStyle(StyleGroup group, string? name = null);

        /// <summary>Reads CSS from the local filesystem.</summary>
        IDocumentBuilder WithStyleFile(string filePath, string? name = null);

        /// <summary>Fetches CSS from a remote URL at generation time.</summary>
        IDocumentBuilder WithStyleUrl(string url, string? name = null, string? auth = null, string? format = null);

        // ── Data ──────────────────────────────────────────────────────────────

        /// <summary>Adds a named data object as an inline JSON string.</summary>
        IDocumentBuilder WithData(string name, string jsonContent, string? format = null);

        /// <summary>Reads JSON data from a stream.</summary>
        IDocumentBuilder WithData(string name, Stream stream);

        /// <summary>Sets a named data parameter to any object value, set directly on <c>doc.Params</c>.</summary>
        IDocumentBuilder WithData(string name, object data);

        /// <summary>Reads JSON data from the local filesystem.</summary>
        IDocumentBuilder WithDataFile(string name, string filePath);

        /// <summary>Fetches JSON data from a remote URL at generation time.</summary>
        IDocumentBuilder WithDataUrl(string name, string url, string? auth = null, string? format = null);

        // ── Parameters (fields[]) ─────────────────────────────────────────────

        /// <summary>
        /// Sets a parameter value, accessible in templates as <c>fields['id']</c>.
        /// Adds both a template default and a runtime override so the value is
        /// always available regardless of what the template config declares.
        /// </summary>
        IDocumentBuilder WithParameter(string id, string value, string type = "string");

        // ── Render options ────────────────────────────────────────────────────

        /// <summary>Uses Scryber's async render path (default: false).</summary>
        IDocumentBuilder UseAsync(bool useAsync = true);

        /// <summary>Sets the Scryber trace log level.</summary>
        IDocumentBuilder WithLogLevel(PaperworkRequestLogOption level);

        // ── Terminal operations ───────────────────────────────────────────────

        /// <summary>Generates the document and returns the full <see cref="PaperworkResult"/>.</summary>
        Task<PaperworkResult> BuildAsync();

        /// <summary>Generates the document and returns the raw PDF bytes.</summary>
        Task<byte[]> BuildBytesAsync();

        /// <summary>Generates the document and writes the PDF to <paramref name="outputPath"/>.</summary>
        Task SaveAsync(string outputPath);
    }
}
