using System;
using System.Collections.Generic;
using System.Text;

namespace Nephila
{
    public class NullLogger : ILogger
    {
        public void Log(string message)
        {
            return;
        }
    }
}
