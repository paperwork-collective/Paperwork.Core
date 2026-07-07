using System;
using Paperwork.Generation;

namespace Paperwork
{
	public interface IPaperworkFactory : IDisposable
	{

		event EventHandler<GenerationProgressArgs> GenerationProgress;

		Task<string> Generate(string content);

		Task<PaperworkResult> Generate(PaperworkRequest result);
	}
}

