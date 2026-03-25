using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services
{
	/// <summary>
	/// Handles the recoring of the actions and logging within the Paperwork execution.
	/// </summary>
	public interface IPaperworkTracingService
	{

		IPaperworkGenerationTracer Init(PaperworkRequest request);
	}

}

