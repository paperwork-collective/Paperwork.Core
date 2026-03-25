using System;
namespace Paperwork.Services.Generation.v1
{
	public class PDFGeneratorFactory : IPaperworkGeneratorFactory
	{
		public PDFGeneratorFactory()
		{
		}

        public Version MinVersion { get { return PDFGenerator.MyVersion; } }

        public Version MaxVersion { get { return PDFGenerator.MyVersion; } }

        public string ResultMimeType { get { return PDFGenerator.PDFMimeType; } }

        public IPaperworkGenerator CreateGeneratorInstance(IPaperworkAuthService authService, IPaperworkTracingService tracingService, IPaperworkRemoteFileRequestService fileRequestService)
        {
            return new PDFGenerator(authService, tracingService, fileRequestService);
        }
    }
}

