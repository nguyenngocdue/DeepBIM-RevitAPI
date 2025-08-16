using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.Models
{
    public class CommandItem
    {
        public CommandType CommandType { get; set; }
        public RevitCommandId RevitCommandId { get; set; }
        public string CommandId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public override string ToString() => DisplayName;
    }
}
