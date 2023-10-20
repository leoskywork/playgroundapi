using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models.Helpers
{
    public class ValidationResult
    {
        public bool Valid { get; set; }
        public string Message { get; set; }

        public static ValidationResult Fail(string message = null)
        {
            return Create(false, message);
        }

        public static ValidationResult<T> Fail<T>(T? message = null) where T: struct
        {
            return Create(false, message);
        }

        public static ValidationResult Success(string message = null)
        {
            return Create(true, message);
        }

        public static ValidationResult<T> Success<T>(T? message = null) where T : struct
        {
            return Create(true, message);
        }

        public static ValidationResult Create(bool valid = false, string message = null)
        {
            return new ValidationResult() { Valid = valid, Message = message };
        }

        public static ValidationResult<T> Create<T>(bool valid = false, T? message = null) where T : struct
        {
            return new ValidationResult<T>() { Valid = valid, Message = message };
        }
    }

    public class ValidationResult<T> where T : struct
    {
        public bool Valid { get; set; }
        public T? Message { get; set; }

       
    }
}