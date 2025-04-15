using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.Services
{
    public class EmailSettings
    {
        public string FromEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
    }
}