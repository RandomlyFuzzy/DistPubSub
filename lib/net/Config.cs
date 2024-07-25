using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net
{
    public static class Config
    {
        public static bool IsServer { get; set; } = false;
        public static NetClient CoreClient { get; set; } = null;
        public static NetServer CoreServer { get; set; } = null;
    }
}
