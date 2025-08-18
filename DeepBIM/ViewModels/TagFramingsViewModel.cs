using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.ViewModels
{
    public class TagFramingsViewModel
    {
        public string WelcomeMessage { get; set; }
        public string DueDate { get; set; }
        public string AdditionalInfo { get; set; }
        private Document doc;
        private UIApplication uiapp;
        private UIDocument uidoc;

        public TagFramingsViewModel(UIApplication uiapp, UIDocument uidoc,Document doc)
        {
            this.doc = doc;
            this.uiapp = uiapp;
            this.uidoc = uidoc;

            WelcomeMessage = "Welcome to DeepBIM!";
            DueDate = "Due Date: 30/12/2023";
            AdditionalInfo = "Please ensure all tasks are completed by the due date.";
            TaskDialog.Show("Welcome", $"{WelcomeMessage}\n{DueDate}\n{AdditionalInfo}");
        }
    }
}
