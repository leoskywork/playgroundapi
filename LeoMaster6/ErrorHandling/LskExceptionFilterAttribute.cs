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
                    ReasonPhrase = context.Exception.Message
                };
            }
            else
            {
                context.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Oops! " + context.Exception.Message
                };

                //?? partial of the error message(Chinese) on postman are gibberish, not sure why
                //not working
                //context.Response.Headers.TransferEncoding.Add(new System.Net.Http.Headers.TransferCodingHeaderValue("utf-8"));
            }
        }
    }
}