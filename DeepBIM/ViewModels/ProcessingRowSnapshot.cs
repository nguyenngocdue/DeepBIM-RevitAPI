using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.ViewModels
{
    public  sealed class ProcessingRowSnapshot
    {
        public ElementId CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Amount { get; set; }
    }
}
