using System;
namespace Paperwork.Auth
{
	public class PaperworkAuthOptions
	{
		public const string PAPERWORK_SECTION = "paperwork";
		public const string CURRENT_USER_ID = PAPERWORK_SECTION + ".userid";
		public const string TEMPLATE_ID = PAPERWORK_SECTION + ".templateid";
		public const string OWNER_ID = PAPERWORK_SECTION + ".ownerid";

		public string CurrentUserId
		{
			get
			{
				if (this.OAuthTokens.TryGetValue(CURRENT_USER_ID, out var userId))
					return userId;
				
				return string.Empty;

			}
		}

		public string TemplateId
		{
			get
			{
				if(this.OAuthTokens.TryGetValue(TEMPLATE_ID, out var token))
					return token;
				
				return string.Empty;
			}
		}

		public string OwnerId
		{
			get
			{
				if(this.OAuthTokens.TryGetValue(OWNER_ID, out var token))	
					return token;
				return string.Empty;
			}
		}


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

