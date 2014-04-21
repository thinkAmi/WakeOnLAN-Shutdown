using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WakeOnLAN_Shutdown
{
    class CommandLineOption
    {
        [CommandLine.Option('u', "UpdateTargetFile", DefaultValue = false, HelpText = "Update Target Machine File")]
        public bool Update { get; set; }

        [CommandLine.Option('w', "WakeOnLAN", DefaultValue = false, HelpText = "Execute WakeOnLAN")]
        public bool WakeOnLan { get; set; }

        [CommandLine.Option('s', "Shutdown", DefaultValue = false, HelpText = "Execute Shutdown")]
        public bool Shutdown { get; set; }
    }
}
