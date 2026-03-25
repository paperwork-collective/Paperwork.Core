using System;
using Paperwork.Services.Generation;

namespace Paperwork.Services
{
	public interface IPaperworkRemoteFileRequestService
	{
		public bool ShouldHandle(Scryber.RemoteFileRequest request, out IPaperworkRemoteFileRequestor requestor);
	}

	public interface IPaperworkRemoteFileRequestor
	{
		void HandleRequest(HttpClient client, Scryber.RemoteFileRequest request);
	}
}

