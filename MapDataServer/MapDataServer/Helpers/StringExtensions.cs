using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class StringExtensions
    {
        public static bool IsFalseNoBlankOrNull(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return true;
            return (str.ToLower() == "false") || (str.ToLower() == "no");
        }
    }
}
