using System;

namespace Paperwork.Generation
{
    /// <summary>
    /// Decides whether a document being generated may fetch something from a url -
    /// an image, a font, a stylesheet, or one of the template's own remote data,
    /// layout or style sources.
    /// </summary>
    public interface IPaperworkRemoteAccessPolicy
    {
        /// <summary>
        /// True when the url may be fetched. When false, <paramref name="reason"/>
        /// says why, in words fit for the document's log and an error message.
        /// </summary>
        bool IsAllowed(Uri url, out string reason);
    }

    /// <summary>
    /// Where the policy for the current process is held. Nothing is set by default,
    /// which allows everything: Paperwork.Core on its own has no notion of a portal.
    /// Paperwork.Core.Extensions installs one that applies the portal's allowed and
    /// blocked domains (PaperworkServiceProviderSetup.EnsureRegistered).
    /// </summary>
    public static class PaperworkRemoteAccess
    {
        public static IPaperworkRemoteAccessPolicy? Policy { get; set; }

        /// <summary>
        /// Applies the current policy to a path a document has asked for. Only
        /// absolute http and https urls are subject to it - a relative path, a
        /// data: url, or one of Paperwork's own "$assets/..." / "$maps/..."
        /// references is not a fetch from an arbitrary host.
        /// </summary>
        public static bool IsAllowed(string? path, out string reason)
        {
            reason = string.Empty;

            var policy = Policy;
            if (policy == null || string.IsNullOrWhiteSpace(path))
                return true;

            if (!Uri.TryCreate(path, UriKind.Absolute, out var url))
                return true;

            if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                return true;

            return policy.IsAllowed(url, out reason);
        }
    }

    /// <summary>
    /// Raised when a document asks for something from a host the policy refuses.
    /// </summary>
    public class PaperworkRemoteAccessDeniedException : InvalidOperationException
    {
        public string Url { get; }

        public PaperworkRemoteAccessDeniedException(string url, string reason)
            : base(string.IsNullOrEmpty(reason) ? "Requests to '" + url + "' are not allowed." : reason)
        {
            this.Url = url;
        }
    }
}
