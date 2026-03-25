using Paperwork.Services;
using Paperwork.Services.Auth;
using Paperwork.Services.Generation;
using Paperwork.Services.Tracing;

namespace Paperwork
{
    /// <summary>
    /// Fluent builder that constructs a configured <see cref="PaperworkInstanceFactory"/>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// // Minimal — no auth, default tracing, no remote file handler
    /// var factory = PaperworkFactory.Create(httpClient).Build();
    ///
    /// // With explicit auth services
    /// var factory = PaperworkFactory.Create(httpClient)
    ///     .WithAuth(new MyConfluenceAuth())
    ///     .WithAuth(new MyApiKeyAuth())
    ///     .WithTracing(myTracingService)
    ///     .WithFileRequests(myFileRequestService)
    ///     .Build();
    /// </code>
    /// </remarks>
    public sealed class PaperworkFactory
    {
        private readonly HttpClient _httpClient;
        private readonly List<IPaperworkAuthService> _authServices = new();

        private IPaperworkTracingService? _tracingService;
        private IPaperworkRemoteFileRequestService? _fileRequestService;
        private System.Text.Json.JsonSerializerOptions? _serializerOptions;

        private PaperworkFactory(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ── Entry point ───────────────────────────────────────────────────────

        /// <summary>Creates a new builder targeting the supplied <see cref="HttpClient"/>.</summary>
        public static PaperworkFactory Create(HttpClient httpClient)
            => new(httpClient);

        // ── Auth ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an auth service. Call multiple times to register several services;
        /// they are tried in registration order.
        /// </summary>
        public PaperworkFactory WithAuth(IPaperworkAuthService service)
        {
            _authServices.Add(service ?? throw new ArgumentNullException(nameof(service)));
            return this;
        }

        // ── Tracing / logging ─────────────────────────────────────────────────

        /// <summary>
        /// Replaces the default <see cref="PaperworkTracingService"/> with a custom implementation.
        /// </summary>
        public PaperworkFactory WithTracing(IPaperworkTracingService tracingService)
        {
            _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
            return this;
        }

        // ── Remote file requests ──────────────────────────────────────────────

        /// <summary>
        /// Replaces the default no-op file-request handler with a custom implementation.
        /// Use this to intercept or proxy remote template/data URLs before Paperwork fetches them.
        /// </summary>
        public PaperworkFactory WithFileRequests(IPaperworkRemoteFileRequestService fileRequestService)
        {
            _fileRequestService = fileRequestService ?? throw new ArgumentNullException(nameof(fileRequestService));
            return this;
        }

        // ── JSON serialiser ───────────────────────────────────────────────────

        /// <summary>
        /// Overrides the <see cref="System.Text.Json.JsonSerializerOptions"/> used when deserialising
        /// incoming request strings.  Defaults to trailing-comma-tolerant, case-insensitive settings.
        /// </summary>
        public PaperworkFactory WithSerializerOptions(System.Text.Json.JsonSerializerOptions options)
        {
            _serializerOptions = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        // ── Terminal ──────────────────────────────────────────────────────────

        /// <summary>Constructs and returns the configured <see cref="PaperworkInstanceFactory"/>.</summary>
        public PaperworkInstanceFactory Build()
        {
            var auth = new PaperworkAuthWrapperService(_authServices);
            var tracing = _tracingService ?? new PaperworkTracingService();
            var fileRequests = _fileRequestService ?? new EmptyFileRequestService();
            var serializerOptions = _serializerOptions ?? new System.Text.Json.JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };

            return new PaperworkInstanceFactory(serializerOptions, _httpClient, auth, tracing, fileRequests);
        }

        /// <summary>
        /// Shortcut: returns a <see cref="IDocumentBuilder"/> ready to start building a document,
        /// equivalent to <c>Build().NewDocument()</c>.
        /// </summary>
        public IDocumentBuilder NewDocument() => Build().NewDocument();
    }
}
