using System;
namespace Paperwork.Services
{
	public interface IPaperworkAuthService
	{
		public bool CanFetch(string authName, Uri uri);

		public Task<string> Fetch(HttpClient client, string authName, Uri uri, Auth.PaperworkAuthOptions options);
	}

	
}

