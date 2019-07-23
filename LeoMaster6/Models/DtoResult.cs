using System;
using System.Dynamic;
using System.Web.Http;

namespace LeoMaster6.Models
{

    public static class DtoResult
    {
        private class ResultWrap : IResultWrap
        {
            public dynamic Result { get; private set; }

            public ResultWrap(object result)
            {
                this.Result = result;
            }
        }

        public static IResultWrap Success(string message = null)
        {
            return Create(true, message, default(object));
        }

        public static IResultWrap Success<T>(T data = null, string message = null) where T : class
        {
            return Create(true, message, data);
        }

        public static IResultWrap Success<T>(T? data = null, string message = null) where T : struct
        {
            return Create(true, message, data);
        }

        public static IResultWrap Fail(string message = null)
        {
            return Create(false, message, default(object));
        }

        private static IResultWrap Create<T>(bool success, string message, T data)
        {
            dynamic result = new ExpandoObject();
            result.success = success;

            if (!string.IsNullOrEmpty(message))
            {
                result.message = message;
            }

            if (data != null)
            {
                result.data = data;
            }

            return new ResultWrap(result);
        }

    }

    public interface IResultWrap
    {
        dynamic Result { get; }
    }

    public static class ResultWrapExtension
    {
        public static IHttpActionResult To(this IResultWrap wrap, Func<object, IHttpActionResult> mapper)
        {
            return mapper(wrap.Result);
        }
    }

    //new versions(preferred)
    public static class DtoResultV5
    {
        public static IHttpActionResult Success(Func<object, IHttpActionResult> mapper, string message = null)
        {
            return Create(mapper, true, message, default(object));
        }

        public static IHttpActionResult Success<T>(Func<object, IHttpActionResult> mapper, T data = null, string message = null) where T : class
        {
            return Create(mapper, true, message, data);
        }

        public static IHttpActionResult Success<T>(Func<object, IHttpActionResult> mapper, T? data = null, string message = null) where T : struct
        {
            return Create(mapper, true, message, data);
        }

        public static IHttpActionResult Fail(Func<object, IHttpActionResult> mapper, string message = null)
        {
            return Create(mapper, false, message, default(object));
        }

        private static IHttpActionResult Create<T>(Func<object, IHttpActionResult> mapper, bool success, string message, T data)
        {
            dynamic result = new ExpandoObject();
            result.success = success;

            if (!string.IsNullOrEmpty(message))
            {
                result.message = message;
            }

            if (data != null)
            {
                result.data = data;
            }

            return mapper(result);
        }

    }

    //old versions
    public class DtoResultV1
    {
        public static object Success(string message = null)
        {
            return new
            {
                success = true,
                message
            };
        }

        public static object Success<T>(T data = null, string message = null) where T : class
        {
            return new
            {
                success = true,
                data,
                message
            };
        }

        public static object Success<T>(T? data = null, string message = null) where T : struct
        {
            return new
            {
                success = true,
                data,
                message
            };
        }

        public static object Fail(string message = null)
        {
            return new { success = false, message };
        }

    }


}