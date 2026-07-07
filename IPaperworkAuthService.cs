using System;
using Paperwork.Auth;

namespace Paperwork
{
	public interface IPaperworkAuthService
	{
		string Name { get; }
		
		bool CanFetch(string authName, string uri);

		Task<object> Fetch(HttpClient client, string authName, string uri, Auth.PaperworkAuthOptions options);
	}

	
}

