using System;
using Paperwork.Generation;

namespace Paperwork.Tracing
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

