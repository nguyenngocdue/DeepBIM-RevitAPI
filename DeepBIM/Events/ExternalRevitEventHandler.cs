using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace DeepBIM.Events
{
    public class ExternalRevitEventHandler : IExternalEventHandler
    {
        private Action<UIApplication> _action;
        private UIApplication _uiApp;

        public void Execute(UIApplication app)
        {
            _uiApp = app;
            _action?.Invoke(app);
        }

        public string GetName()
        {
            return "External Revit Event Handler (DeepBIM EventHandler)";
        }

        public void SetAction(Action<UIApplication> action)
        {
            _action = action;
        }
    }
}