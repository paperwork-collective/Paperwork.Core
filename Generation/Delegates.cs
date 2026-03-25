using System;
namespace Paperwork.Services.Generation
{
    public delegate Task<object> LoadActionAsync(string path, string authenticationProvider, string requestResultType);
}

