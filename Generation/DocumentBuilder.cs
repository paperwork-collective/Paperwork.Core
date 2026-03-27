using System.Text.Json;
using Scryber;
using Scryber.Components;
using Scryber.Styles;
using Paperwork.Services;
using Paperwork.Services.Generation;
using Paperwork.Services.Generation.v1;

namespace Paperwork
{
    /// <summary>
    /// Fluent builder that constructs and generates a Paperwork PDF document.
    /// </summary>
    /// <remarks>
    /// When only string/file/URL inputs are used the request is delegated to
    /// <see cref="IPaperworkFactory"/> (full auth/proxy pipeline).
    /// When a pre-built <see cref="Document"/> or <see cref="StyleGroup"/> is
    /// supplied the builder renders directly via the Scryber API.
    /// <code>
    /// var pdf = await factory.NewDocument()
    ///     .WithLayoutFile("invoice.html")
    ///     .WithData("invoice", invoiceJson)
    ///     .WithField("date", "2026-03-25")
    ///     .BuildBytesAsync();
    /// </code>
    /// </remarks>
    public class DocumentBuilder : IDocumentBuilder
    {
        private readonly IPaperworkFactory _factory;
        private readonly TemplateConfigV1 _config;
        private readonly List<PaperworkRequestField> _overrides;
        private readonly PaperworkRequestRenderOptions _renderOptions;

        // Direct-object storage — triggers the Scryber render path
        private readonly Dictionary<string, IComponent> _componentLayouts;   // named layouts/partials
        private readonly List<(string? name, StyleGroup group)> _directStyleGroups;
        private readonly List<(string name, object value)> _directDataObjects;

        private int _styleCount;
        private int _dataCount;

        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public DocumentBuilder(IPaperworkFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _config = new TemplateConfigV1();
            _overrides = new List<PaperworkRequestField>();
            _renderOptions = new PaperworkRequestRenderOptions();
            _componentLayouts = new Dictionary<string, IComponent>();
            _directStyleGroups = new List<(string?, StyleGroup)>();
            _directDataObjects = new List<(string, object)>();
        }

        private bool HasDirectObjects =>
            _componentLayouts.Count > 0 || _directStyleGroups.Count > 0 || _directDataObjects.Count > 0;

        // ── Layout ────────────────────────────────────────────────────────────

        public IDocumentBuilder WithLayout(string htmlContent, string name = "main")
        {
            _config.Layout.Add(new LayoutContent
            {
                Name = name,
                Content = htmlContent,
                Type = "Content"
            });
            return this;
        }

        public IDocumentBuilder WithLayout(Stream stream, string name = "main")
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            return WithLayout(reader.ReadToEnd(), name);
        }

        public IDocumentBuilder WithLayout(IComponent component, string name = "main")
        {
            _componentLayouts[name] = component ?? throw new ArgumentNullException(nameof(component));
            return this;
        }

        public IDocumentBuilder WithLayoutFile(string filePath, string name = "main")
            => WithLayout(File.ReadAllText(filePath), name);

        public IDocumentBuilder WithLayoutUrl(string url, string name = "main", string? auth = null)
        {
            _config.Layout.Add(new LayoutContent
            {
                Name = name,
                Source = url,
                Type = "Source",
                Auth = auth ?? string.Empty
            });
            return this;
        }

        // ── Styles ────────────────────────────────────────────────────────────

        public IDocumentBuilder WithStyle(string cssContent, string? name = null, string? format = null)
        {
            _config.Style.Add(new StyleContent
            {
                Name = name ?? $"style{++_styleCount}",
                Content = cssContent,
                Type = "Content",
                Format = format ?? string.Empty
            });
            return this;
        }

        public IDocumentBuilder WithStyle(Stream stream, string? name = null)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            return WithStyle(reader.ReadToEnd(), name);
        }

        public IDocumentBuilder WithStyle(StyleGroup group, string? name = null)
        {
            _directStyleGroups.Add((name, group ?? throw new ArgumentNullException(nameof(group))));
            return this;
        }

        public IDocumentBuilder WithStyleFile(string filePath, string? name = null)
            => WithStyle(
                File.ReadAllText(filePath),
                name ?? System.IO.Path.GetFileNameWithoutExtension(filePath));

        public IDocumentBuilder WithStyleUrl(string url, string? name = null, string? auth = null, string? format = null)
        {
            _config.Style.Add(new StyleContent
            {
                Name = name ?? $"style{++_styleCount}",
                Source = url,
                Type = "Source",
                Auth = auth ?? string.Empty,
                Format = format ?? string.Empty
            });
            return this;
        }

        // ── Data ──────────────────────────────────────────────────────────────

        public IDocumentBuilder WithData(string name, string jsonContent, string? format = null)
        {
            _config.Data.Add(new DataContent
            {
                Name = name,
                Content = jsonContent,
                Type = "Content",
                Format = format ?? string.Empty
            });
            return this;
        }

        public IDocumentBuilder WithData(string name, Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            return WithData(name, reader.ReadToEnd());
        }

        public IDocumentBuilder WithData(string name, object data)
        {
            _directDataObjects.Add((name, data));
            return this;
        }

        // Keep for back-compat
        public IDocumentBuilder WithDataObject(string name, object data)
            => WithData(name, data);

        public IDocumentBuilder WithDataFile(string name, string filePath)
            => WithData(name, File.ReadAllText(filePath));

        public IDocumentBuilder WithDataUrl(string name, string url, string? auth = null, string? format = null)
        {
            _config.Data.Add(new DataContent
            {
                Name = name,
                Source = url,
                Type = "Source",
                Auth = auth ?? string.Empty,
                Format = format ?? string.Empty
            });
            return this;
        }

        // ── Fields ────────────────────────────────────────────────────────────

        public IDocumentBuilder WithField(string id, string value, string type = "string")
        {
            _config.Fields ??= new List<DataField>();
            var existing = _config.Fields.FirstOrDefault(p => p.ID == id);
            if (existing != null)
                existing.Value = value;
            else
                _config.Fields.Add(new DataField { ID = id, Value = value, Type = type });

            var existingOverride = _overrides.FirstOrDefault(p => p.Id == id);
            if (existingOverride != null)
                existingOverride.Value = value;
            else
                _overrides.Add(new PaperworkRequestField { Id = id, Value = value, Type = type });

            return this;
        }

        [Obsolete("Use WithField instead.")]
        public IDocumentBuilder WithParameter(string id, string value, string type = "string")
            => WithField(id, value, type);

        // ── Render options ────────────────────────────────────────────────────

        public IDocumentBuilder UseAsync(bool useAsync = true)
        {
            _renderOptions.UseAsync = useAsync;
            return this;
        }

        public IDocumentBuilder WithLogLevel(PaperworkRequestLogOption level)
        {
            _renderOptions.LogLevel = level;
            return this;
        }

        // ── Terminal operations ───────────────────────────────────────────────

        public Task<PaperworkResult> BuildAsync()
            => HasDirectObjects ? BuildDirectAsync() : BuildViaFactoryAsync();

        public async Task<byte[]> BuildBytesAsync()
        {
            var result = await BuildAsync();

            if (!result.Success)
                throw new InvalidOperationException(
                    $"Document generation failed: {result.Message}" +
                    (string.IsNullOrEmpty(result.Error) ? "" : $"\n{result.Error}"));

            if (result.Document?.Binary == null)
                throw new InvalidOperationException(
                    "Generation succeeded but no document data was returned.");

            return result.Document.Binary;
        }

        public async Task SaveAsync(string outputPath)
            => await File.WriteAllBytesAsync(outputPath, await BuildBytesAsync());

        // ── Factory path (string/file/URL inputs) ─────────────────────────────

        private Task<PaperworkResult> BuildViaFactoryAsync()
        {
            if (_config.Layout.Count == 0)
                throw new InvalidOperationException(
                    "At least one layout must be added before building.");

            var configJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null   // TemplateConfigV1 uses [JsonPropertyName] attributes
            });

            var request = new PaperworkRequest
            {
                Content = configJson,
                Fields = _overrides,
                RenderOptions = _renderOptions
            };

            return _factory.Generate(request);
        }

        // ── Direct Scryber path (Document / StyleGroup / object inputs) ───────

        private async Task<PaperworkResult> BuildDirectAsync()
        {
            Document doc;
            var mainName = _config.MainLayoutName ?? TemplateConfigV1.DefaultMainLayoutName;

            // Prefer a pre-built Document registered under the main layout name
            if (_componentLayouts.TryGetValue(mainName, out var mainComponent) &&
                mainComponent is Document directDoc)
            {
                doc = directDoc;
            }
            else
            {
                // Fall back to parsing the main layout string from the config
                if (_config.Layout.Count == 0)
                    throw new InvalidOperationException(
                        "At least one layout must be added before building.");

                var layout = _config.Layout.FirstOrDefault(l => l.Name == mainName)
                    ?? _config.Layout[0];

                if (string.IsNullOrEmpty(layout.Content))
                    throw new InvalidOperationException(
                        $"Layout '{layout.Name}' has no inline content. " +
                        "Remote URLs require the factory path (no direct objects).");

                doc = ParseDocument(layout.Content, layout.Source ?? "");
            }

            // Expose all named components as _templates so the main layout can reference
            // partials via {{_templates["myPartial"]}}
            if (_componentLayouts.Count > 0)
                doc.Params["_templates"] = _componentLayouts;

            // Apply string CSS from config as HTMLStyle elements
            if (doc is Scryber.Html.Components.HTMLDocument htmlDoc && _config.Style.Count > 0)
            {
                htmlDoc.Head ??= new Scryber.Html.Components.HTMLHead();
                for (int i = 0; i < _config.Style.Count; i++)
                {
                    var item = _config.Style[i];
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        htmlDoc.Head.Contents.Add(new Scryber.Html.Components.HTMLStyle
                        {
                            Contents = item.Content,
                            ID = item.Name
                        });
                    }
                }
            }

            // Apply direct StyleGroups
            foreach (var (_, group) in _directStyleGroups)
                doc.Styles.Add(group);

            // Apply data from config (JSON strings)
            foreach (var item in _config.Data)
            {
                var value = (item.Content ?? "").Trim();
                if (string.IsNullOrEmpty(value)) continue;

                if (value.StartsWith("{{") && value.EndsWith("}}"))
                    doc.Params[item.Name] = value[1..^1];
                else if (value.StartsWith("[[") && value.EndsWith("]]"))
                    doc.Params[item.Name] = value[1..^1];
                else if ((value.StartsWith("{") && value.EndsWith("}")) ||
                         (value.StartsWith("[") && value.EndsWith("]")))
                    doc.Params[item.Name] = JsonDocument.Parse(value).RootElement.Clone();
                else
                    doc.Params[item.Name] = value;
            }

            // Apply direct data objects
            foreach (var (name, value) in _directDataObjects)
                doc.Params[name] = value;

            // Apply parameters as fields dictionary
            var fields = new Dictionary<string, object>();
            if (_config.Fields != null)
                foreach (var p in _config.Fields)
                    fields[p.ID] = p.Value;
            foreach (var o in _overrides)
                fields[o.Id] = o.Value;
            doc.Params["fields"] = fields;

            // Render
            using var ms = new MemoryStream();
            try
            {
                if (_renderOptions.UseAsync)
                    await doc.SaveAsPDFAsync(ms);
                else
                    doc.SaveAsPDF(ms);
            }
            catch (Exception ex)
            {
                return new PaperworkResult
                {
                    Success = false,
                    Message = $"Document render failed: {ex.Message}",
                    Error = ex.ToString(),
                    Progress = 1.0
                };
            }

            var bytes = ms.ToArray();
            return new PaperworkResult
            {
                Success = true,
                ResultCode = 200,
                Message = "Success",
                Progress = 1.0,
                Document = new PaperworkDocumentResult(bytes, "application/pdf", "text/plain", bytes.Length)
            };
        }

        private static Document ParseDocument(string content, string sourcePath)
        {
            // IMPORTANT: Document.ParseHtmlDocument(string) treats the argument as a FILE PATH.
            // Always use the TextReader overload for inline HTML content.
            // Detect XHTML by presence of xmlns on the <html> element
            bool isXhtml = false;
            var htmlStart = content.IndexOf("<html ", StringComparison.OrdinalIgnoreCase);
            if (htmlStart >= 0)
            {
                var tagEnd = content.IndexOf('>', htmlStart);
                if (tagEnd > htmlStart)
                {
                    var tag = content.Substring(htmlStart, tagEnd - htmlStart);
                    isXhtml = tag.IndexOf("xmlns", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            var sourceType = string.IsNullOrEmpty(sourcePath)
                ? Scryber.ParseSourceType.DynamicContent
                : Scryber.ParseSourceType.RemoteFile;

            using var reader = new StringReader(content);
            return isXhtml
                ? Document.ParseDocument(reader, sourcePath, sourceType)
                : Document.ParseHtmlDocument(reader, sourcePath, sourceType);
        }
    }
}
