using System;
using Autodesk.Revit.UI;

namespace DeepBIM.Services
{
    public class TagExternalEventHandler : IExternalEventHandler
    {
        public Action<UIApplication> DoWork { get; set; }
        public void Execute(UIApplication app)
        {
            try { DoWork?.Invoke(app); }
            finally { DoWork = null; }
        }
        public string GetName() => "DeepBIM - TagElements ExternalEvent";
    }
}
