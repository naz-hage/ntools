using System.Collections.Generic;

namespace launcher
{
    public class Result
    {
        public static readonly int Success = 0;
        public static readonly int InvalidParameter = -1;
        public static readonly int FileNotFound = -3;
        public static readonly int Exception = int.MaxValue;

        /// <summary>
        /// 0 indicate success; otherwise failure 
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// A collection of lines displayed output
        /// </summary>
        public List<string> Output {get;set;}

        public Result()
        {
            // default return code
            Code = Exception;
            Output = new List<string>();
        }
    }
}
