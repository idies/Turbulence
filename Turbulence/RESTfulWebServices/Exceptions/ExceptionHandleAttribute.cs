using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Filters;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SciServer.Logging;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Turbulence.REST
{
    public class ExceptionHandleAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            Logger log = (HttpContext.Current.ApplicationInstance as MvcApplication).Log;

            System.Text.Encoding tCode = System.Text.Encoding.UTF8;
            String responseType = "application/json";
            
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            String reasonPhrase = "";
            HttpStatusCode errorCode = HttpStatusCode.InternalServerError;

            if (context.Exception is NotAuthorizedException)
            {
                errorCode = HttpStatusCode.Unauthorized;
                reasonPhrase = errorCode.ToString();
            }
            else if (context.Exception is NotFoundException)
            {
                errorCode = HttpStatusCode.NotFound;
                reasonPhrase = errorCode.ToString();
            }
            else if (context.Exception is Exception)
            {
                errorCode = HttpStatusCode.InternalServerError;
                reasonPhrase = errorCode.ToString();
            }

            Message msg = log.CreateErrorMessage(context.Exception);
            msg.UserId = (string)context.Exception.Data["UserId"];
            log.SendMessage(msg);

            string errorMessage = context.Exception.Message +
                ((context.Exception.InnerException != null) ? (": " + context.Exception.InnerException.Message) : "");

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Error Code");
                writer.WriteValue((int)errorCode);
                writer.WritePropertyName("Error Type");
                writer.WriteValue(errorCode.ToString());
                writer.WritePropertyName("Error Message");
                writer.WriteValue(errorMessage);
                writer.WritePropertyName("LogMessageID");
                writer.WriteValue(msg.MessageId);
            }

            HttpResponseMessage resp = new HttpResponseMessage(errorCode)
            {
                Content = new StringContent(sb.ToString(), tCode, responseType),
                ReasonPhrase = reasonPhrase
            };

            throw new HttpResponseException(resp);
        }
    }
}