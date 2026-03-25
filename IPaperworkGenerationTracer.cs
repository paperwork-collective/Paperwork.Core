using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services
{
	

	public interface IPaperworkGenerationTracer
	{
		
		/// <summary>
		/// Gets the name of the current generation tracer log
		/// </summary>
		PaperworkGenerationStage Current { get; }

		/// <summary>
		/// Starts a new generation tracer log
		/// </summary>
		/// <param name="stage">The stage of the log to begin</param>
		/// <returns>A disposable instance that will record the duration of the stage</returns>
        IDisposable Begin(PaperworkGenerationStage stage);

		/// <summary>
		/// Registers a new inner request for a remote file.
		/// </summary>
		/// <param name="request"></param>
		void RegisterRequest(Scryber.RemoteFileRequest request);

		/// <summary>
		/// Returns the complete log of the tracer events
		/// </summary>
		/// <returns></returns>
        PaperworkGenerationLog GetLog();
	}

	


}

