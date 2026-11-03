using System;
using System.Diagnostics.CodeAnalysis;
using Scryber;

namespace Paperwork.Generation
{
	public class EmptyFileRequestService : IPaperworkRemoteFileRequestService
	{
		public EmptyFileRequestService()
		{
		}

        public bool ShouldHandle(RemoteFileRequest request, [NotNullWhen(true)] out IPaperworkRemoteFileRequestor? requestor)
        {
            requestor = null;
            return false;
        }
    }
}

