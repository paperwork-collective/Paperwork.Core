using System;
using Scryber;

namespace Paperwork.Generation
{
	public class EmptyFileRequestService : IPaperworkRemoteFileRequestService
	{
		public EmptyFileRequestService()
		{
		}

        public bool ShouldHandle(RemoteFileRequest request, out IPaperworkRemoteFileRequestor requestor)
        {
            requestor = null;
            return false;
        }
    }
}

