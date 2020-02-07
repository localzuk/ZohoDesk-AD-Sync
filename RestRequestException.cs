using System;
using System.Net;
using System.Runtime.Serialization;

namespace Zoho_Desk_AD_Sync
{
    [Serializable]
    internal class RestRequestException : Exception
    {

        public HttpStatusCode HttpStatusCode { get; set; }
        public RestSharp.ResponseStatus ResponseStatus { get; set; }
        public RestRequestException()
        {
        }

        public RestRequestException(string message) : base(message)
        {
        }

        public RestRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RestRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}