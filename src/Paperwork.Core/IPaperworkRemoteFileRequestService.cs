using System;
using System.Diagnostics.CodeAnalysis;
using Paperwork.Generation;
using Scryber;

namespace Paperwork
{
	public interface IPaperworkRemoteFileRequestService
	{
		public bool ShouldHandle(Scryber.RemoteFileRequest request, [NotNullWhen(true)] out IPaperworkRemoteFileRequestor? requestor);
	}

	public interface IPaperworkRemoteFileRequestor
	{
		void HandleRequest(HttpClient client, Scryber.RemoteFileRequest request);
	}
}

