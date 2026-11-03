using System;
using Paperwork.Generation;

namespace Paperwork
{
	/// <summary>
	/// Handles the recoring of the actions and logging within the Paperwork execution.
	/// </summary>
	public interface IPaperworkTracingService
	{

		IPaperworkGenerationTracer Init(PaperworkRequest request);
	}

}

