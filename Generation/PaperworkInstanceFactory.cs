using System;
using System.Text;

namespace Paperwork.Services.Generation
{
    /// <summary>
    /// An implementation of the IPaperworkFactory that holds single instances of the IPaperworkGenerators. Best for single threaded operations.
    /// </summary>
	public class PaperworkInstanceFactory : IPaperworkFactory
	{

        #region event GenerationProgress + OnGenerationProgress

		/// <summary>
		/// Raised when the document generation progress changes
		/// </summary>
        public event EventHandler<GenerationProgressArgs>? GenerationProgress;

		/// <summary>
		/// Call when the there is a change in the document generation progress
		/// </summary>
		/// <param name="args"></param>
		protected virtual void OnGenerationProgress(GenerationProgressArgs args)
		{
			if (null != GenerationProgress)
				GenerationProgress(this, args);
		}


        #endregion

		//
		// ivars
		//

        System.Text.Json.JsonSerializerOptions _serializerOptions;
		List<IPaperworkGeneratorFactory> _knownGenerators;

        HttpClient _httpClient;

        IPaperworkAuthService _authService;
        IPaperworkTracingService _tracingService;
        IPaperworkRemoteFileRequestService _requestService;

        //
        // ctors
        //

        #region public PaperworkInstanceFactory()

        /// <summary>
        /// Creates a new instance of the PDFGeneratorFactory with default json serialization options
        /// </summary>
        public PaperworkInstanceFactory(HttpClient client)
			: this(new System.Text.Json.JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            }, client, new Auth.PaperworkAuthWrapperService(), new Tracing.PaperworkTracingService(), new EmptyFileRequestService())
		{
		}

        #endregion

        #region public PaperworkInstanceFactory(JsonSerializerOptions options)

        /// <summary>
        /// Creates a new instan ce of the PDFGeneratorFactory with specific json serialization options
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PaperworkInstanceFactory(System.Text.Json.JsonSerializerOptions options, HttpClient client, IPaperworkAuthService authService, IPaperworkTracingService tracingService, IPaperworkRemoteFileRequestService fileRequestService)
		{
			this._serializerOptions = options ?? throw new ArgumentNullException(nameof(options));
			this._knownGenerators = new List<IPaperworkGeneratorFactory>();

            this._httpClient = client ?? throw new ArgumentNullException(nameof(client));
            this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this._tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));

			this.FillKnownGenerators(_knownGenerators);
		}

        #endregion

		//
		// public methods
		//

        #region public async Task<string> Generate(string content)

        /// <summary>
        /// Performs the creation of a document form the request content and return the result as a string.
        /// Both content and result are JSON formatted objects that can be passed to and from other services 
        /// </summary>
        /// <param name="content">The paperwork request content as a json string</param>
        /// <returns>The paperwork result as a json string</returns>
        public async Task<string> Generate(string content)
		{
            return await this.DoGenerateDocument(content);
		}

        #endregion

        #region public async Task<PaperworkResult> Generate(PaperworkRequest request)

        /// <summary>
        /// Performs the creatio of a document from the request, and returns the resulting data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PaperworkResult> Generate(PaperworkRequest request)
        {
            return await DoGenerateDocument(request);
        }

        #endregion

        //
        // implementation
        //


        /// <summary>
        /// Checks the content as a PaperworkRequest, and then calls the DoGenerateDocument with the request.
        /// If not found, then returns PaperworkResult JSON string with the error.
        /// </summary>
        /// <param name="content">The json encoded PaperworkRequest</param>
        /// <returns>The json encoded PaperworkResult</returns>
        protected async virtual Task<string> DoGenerateDocument(string content)
		{

            
            PaperworkResult result = CreateErrorResponse(-100, "A fatal error occurred");
            PaperworkRequest request = null;

            try
            {
                request = System.Text.Json.JsonSerializer.Deserialize<PaperworkRequest>(content, _serializerOptions);
            }
            catch (Exception ex)
            {
                request = null;
            }

            if (null == request)
            {
                result = CreateErrorResponse(GenerationErrors.RequestNotValidCode, GenerationErrors.RequestNotValidMessage);
            }
            else
            {
	            if (request.Parameters != null)
		            Console.WriteLine("Deserialized request with " + request.Parameters.Count + " parameters");
	            else
					Console.WriteLine("The deserialized request had no parameters on it");
	            
                result = await DoGenerateDocument(request);
            }

            //if (null == result.Log)
            //    result.Log = new PaperworkGenerationLog();

            //result.Log.AddEntry(new PaperworkGenerationTraceLogEntry(7, "Finished", "All done", 0, 0));

            return System.Text.Json.JsonSerializer.Serialize<PaperworkResult>(result);
		}

        /// <summary>
        /// looks up the actual factory from the list of known factories
        /// and calls the relevant IPaperworkGenerator based on mime type and version.
        /// If not found, then returns PaperworkResult with the error.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected async virtual Task<PaperworkResult> DoGenerateDocument(PaperworkRequest request)
        {
            PaperworkResult result = CreateErrorResponse(-100, "A fatal error occurred");

            var vers = new Version(request.MajorVersion, request.MinorVersion);
            var mime = request.Output ?? "application/pdf";

            using (var generator = GetGeneratorForRequest(vers, mime))
            {
                if (null == generator)
                {
                    var versions = GetSupportedVersions();
                    result = CreateErrorResponse(GenerationErrors.NoGeneratorFoundCode, string.Format(GenerationErrors.NoGeneratorFoundMessage, vers, mime, versions));
                }
                else
                {
                    try
                    {
                        Console.WriteLine("Starting execution with request start time of " + request.EpochStartOffset);

                        generator.GenerationProgress += GeneratorProgressUpdate;
                        result = await generator.GenerateDocument(request, _httpClient);
                    }
                    catch (Exception ex)
                    {
                        result = CreateErrorResponse(GenerationErrors.ErrorDuringProcessingCode, string.Format(GenerationErrors.ErrorDuringProcessingMessage, ex.Message));
                    }
                    finally
                    {
                        generator.GenerationProgress -= GeneratorProgressUpdate;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Capture the progress event from a generator and raise this factories on progress event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GeneratorProgressUpdate(object? sender, GenerationProgressArgs e)
        {
			this.OnGenerationProgress(e);
        }

        /// <summary>
        /// Creates a new PaperworkResult instance with the provided code and message as an error.
        /// </summary>
        /// <param name="code">The code associated with the error</param>
        /// <param name="message">A message for more information</param>
        /// <returns>The new instance</returns>
        protected virtual PaperworkResult CreateErrorResponse(int code, string message)
		{
            return new PaperworkResult()
            {
                ResultCode = code,
                Success = false,
                Message = message,
                Progress = 1.0,
                Log = null
			};
		}

        /// <summary>
        /// Adds any know IGenerator instances to the factories list. Inheritors can override to add their own.
        /// </summary>
        /// <param name="generators"></param>
		protected virtual void FillKnownGenerators(List<IPaperworkGeneratorFactory> generators)
		{
			generators.Add(new v1.PDFGeneratorFactory());
		}

        /// <summary>
        /// Checks the known list of IGenerators and finds the matching instance for the requestVersion and mimeType.
        /// If a match is not found then it returns null
        /// </summary>
        /// <param name="requestVersion">The request content version</param>
        /// <param name="outputMimeType">The mime-type of the required output</param>
        /// <returns>A metching IGenerator instance or null</returns>
		protected virtual IPaperworkGenerator GetGeneratorForRequest(Version requestVersion, string outputMimeType)
		{
            for (int i = this._knownGenerators.Count - 1; i >= 0; i--)
            {
                var factory = this._knownGenerators[i];
                if (factory.MinVersion <= requestVersion
                    && factory.MaxVersion >= requestVersion
                    && factory.ResultMimeType == outputMimeType)

                    return factory.CreateGeneratorInstance(_authService, _tracingService, _requestService);
            }

			return null;
		}

        private string GetSupportedVersions()
        {
            StringBuilder sb = new StringBuilder();
            for (int t = this._knownGenerators.Count - 1; t >= 0; t--)
            {
                var one = this._knownGenerators[t];
                if (sb.Length > 0)
                    sb.Append(", ");
                if (one.MinVersion == one.MaxVersion)
                {
                    sb.Append(one.MinVersion);
                }
                else
                {
                    sb.Append(one.MinVersion);
                    sb.Append(" - ");
                    sb.Append(one.MaxVersion);
                }
            }

            return sb.ToString();
        }
	}
}

