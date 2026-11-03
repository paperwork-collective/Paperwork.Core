using System;
using Paperwork.Generation;
using Scryber;

namespace Paperwork
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

