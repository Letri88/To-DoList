using To_doList.Models;
using System.Collections.Generic;

namespace To_doList.ViewModels
{
    public class HomeDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int TasksDueToday { get; set; }
        public int TasksOverdue { get; set; }
        public int TasksCompletedThisWeek { get; set; }
        
        // Data for Chart.js (Last 7 days completed tasks)
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<int> ChartData { get; set; } = new List<int>();

        // Focus Tasks (Due today or High Priority - Not Completed)
        public List<TodoTask> FocusTasks { get; set; } = new List<TodoTask>();
    }
}
