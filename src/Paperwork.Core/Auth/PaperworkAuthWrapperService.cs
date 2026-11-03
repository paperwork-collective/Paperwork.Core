using System;

namespace Paperwork.Auth
{
    public class PaperworkAuthWrapperService : IPaperworkAuthService
    {

        public string Name
        {
            get { return "PaperworkAuthWrapperService: " + this._instances.Count.ToString(); }
        }
        
        private List<IPaperworkAuthService> _instances;

        /// <summary>
        /// Creates an empty wrapper with no auth handlers registered.
        /// Use <see cref="PaperworkAuthWrapperService(IEnumerable{IPaperworkAuthService})"/>
        /// or register handlers via <see cref="PaperworkFactory.WithAuth"/> instead.
        /// </summary>
        public PaperworkAuthWrapperService()
        {
            _instances = new List<IPaperworkAuthService>();
        }

        /// <summary>
        /// Creates a wrapper with a specific set of auth services.
        /// </summary>
        public PaperworkAuthWrapperService(IEnumerable<IPaperworkAuthService> services)
        {
            _instances = new List<IPaperworkAuthService>(
                services ?? throw new ArgumentNullException(nameof(services)));
        }

        public bool AddAuth(IPaperworkAuthService service)
        {
            _instances.Add(service);
            return true;
        }

        public bool CanFetch(string authName, string toUri)
        {
            var service = GetWrappedService(authName, toUri, false);
            return null != service;
        }

        public async Task<object> Fetch(HttpClient client, string authName, string url, PaperworkAuthOptions options)
        {
            var service = GetWrappedService(authName, url, true);
            return await service.Fetch(client, authName, url, options);
        }

        protected IPaperworkAuthService GetWrappedService(string authName, string url, bool throwNotFound)
        {
            for (var i = 0; i < _instances.Count; i++)
            {
                var one = _instances[i];
                if (one.CanFetch(authName, url))
                    return one;
            }

            if (throwNotFound)
                throw new ArgumentOutOfRangeException(nameof(authName),
                    "No service within the wrapper can fetch results for the authenticated feed '" +
                    authName + "' to url " + url.ToString());
            else
                return null;
        }
    }
}
