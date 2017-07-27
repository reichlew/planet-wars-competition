using System.Collections.Generic;

namespace PlanetWars.Shared
{
    public class BaseResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string[] Errors { get; set; }

        public static BaseResult<T> Succeed()
        {
            return new BaseResult<T>()
            {
                Success = true,
                Message = "Success"
            };
        }

        public static BaseResult<T> Fail(string message = "Failure", string[] errors = null)
        {
            return new BaseResult<T>()
            {
                Success = false,
                Message = message,
                Errors = errors != null ? errors : null
            };
        }
    }
}
