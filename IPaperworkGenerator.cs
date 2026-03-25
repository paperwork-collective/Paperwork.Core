using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services
{
	public interface IPaperworkGenerator : IDisposable
	{

		public event EventHandler<GenerationProgressArgs>? GenerationProgress;

		public Version MinVersion { get; }

		public Version MaxVersion { get; }

		public string ResultMimeType { get; }

		public Task<PaperworkResult> GenerateDocument(PaperworkRequest request, HttpClient client);
	}

	public interface IPaperworkGeneratorFactory
	{
        public Version MinVersion { get; }

        public Version MaxVersion { get; }

        public string ResultMimeType { get; }

		public IPaperworkGenerator CreateGeneratorInstance(IPaperworkAuthService authService, IPaperworkTracingService tracingService, IPaperworkRemoteFileRequestService fileRequestService);
    }
}

