using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Turbulence.REST
{
    public class NotAuthorizedException : Exception
    {
        public NotAuthorizedException() : base() { }
        public NotAuthorizedException(string message) : base(message) { }
        public NotAuthorizedException(string message, Exception inner) : base(message, inner) { }
    }
}