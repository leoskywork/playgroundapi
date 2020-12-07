using System;
using System.Net.Http;
using System.Web.Http.Filters;

namespace LeoMaster6.ErrorHandling
{
    public class LskExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            System.Diagnostics.Debug.WriteLine(context.Exception.ToString());

            //if (!System.Diagnostics.EventLog.SourceExists("leotest"))
            //{
            //    System.Diagnostics.EventLog.CreateEventSource("leotest", "leotestlog");
            //}

            //System.Diagnostics.EventLog.WriteEntry("leotest",actionExecutedContext.Exception.ToString());

            //new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);


            if (context.Exception is LskExcepiton) //intended throw
            {
                context.Response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = EnsureSafePhrase(context.Exception.Message)
                };
            }
            else
            {
                context.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Oops! " + EnsureSafePhrase(context.Exception.Message) + EnsureSafePhrase(context.Response?.ReasonPhrase)
                };

                //?? partial of the error message(Chinese) on postman are gibberish, not sure why
                //not working
                //context.Response.Headers.TransferEncoding.Add(new System.Net.Http.Headers.TransferCodingHeaderValue("utf-8"));
            }
        }

        private static string EnsureSafePhrase(string message)
        {
            if (message == null) return "(input param is null)";

            var newMessage = message.Replace(Environment.NewLine, " -- ");
            return newMessage.Substring(0, Math.Min(500, newMessage.Length));
        }
    }
}