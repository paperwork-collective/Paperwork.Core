using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services
{
	public interface IPaperworkFactory
	{

		event EventHandler<GenerationProgressArgs> GenerationProgress;

		Task<string> Generate(string content);

		Task<PaperworkResult> Generate(PaperworkRequest result);
	}
}

