using System;
namespace Paperwork
{
    public delegate Task<object> LoadActionAsync(string path, string authenticationProvider, string requestResultType);
}

