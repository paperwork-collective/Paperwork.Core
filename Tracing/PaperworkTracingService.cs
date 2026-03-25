using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services.Tracing
{
	public class PaperworkTracingService : IPaperworkTracingService
	{
		public PaperworkTracingService()
		{
		}

        public IPaperworkGenerationTracer Init(PaperworkRequest request)
        {
            return new PaperworkGenerationTracer(request.EpochStartOffset);
        }
    }
}

