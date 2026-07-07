using System;
namespace Paperwork.Generation.v1
{
	public class PDFGeneratorFactoryV1 : IPaperworkGeneratorFactory
	{
		public PDFGeneratorFactoryV1()
		{
		}

        public Version MinVersion { get { return PDFGeneratorV1.MyVersion; } }

        public Version MaxVersion { get { return PDFGeneratorV1.MyVersion; } }

        public string ResultMimeType { get { return PDFGeneratorV1.PDFMimeType; } }

        public IPaperworkGenerator CreateGeneratorInstance(IPaperworkAuthService authService, IPaperworkTracingService tracingService, IPaperworkRemoteFileRequestService fileRequestService)
        {
            return new PDFGeneratorV1(authService, tracingService, fileRequestService);
        }
    }
}

