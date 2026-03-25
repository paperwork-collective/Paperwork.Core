using System;
namespace Paperwork.Services.Auth
{
	public class PaperworkAuthOptions
	{
		public Dictionary<string, string> OAuthTokens { get; set; }

		public PaperworkAuthOptions()
			: this(new Dictionary<string, string>())
		{
		}

		public PaperworkAuthOptions(Dictionary<string, string> oAuthTokens)
		{
			this.OAuthTokens = oAuthTokens ?? throw new ArgumentNullException(nameof(oAuthTokens));
		}
	}
}

