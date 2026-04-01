using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using To_doList.Data;
using To_doList.Models;
using To_doList.ViewModels;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace To_doList.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUserModel> _userManager;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<AppUserModel> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return View("Landing");
            }
            
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var now = DateTime.Now;
            var today = now.Date;

            // Tasks that belong to me
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .ToListAsync();

            // Assigned to me
            var assignedTasks = await _context.TaskAssignments
                .Include(a => a.Task)
                .Where(a => a.UserId == userId && !a.Task.IsDeleted)
                .Select(a => a.Task)
                .ToListAsync();

            var allMyTasks = tasks.Concat(assignedTasks).GroupBy(t => t.TaskId).Select(g => g.First()).ToList();

            var vm = new HomeDashboardViewModel();
            vm.UserName = user?.UserName ?? "Thành viên";

            vm.TasksDueToday = allMyTasks.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date == today);
            vm.TasksOverdue = allMyTasks.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date < today);
            
            var aWeekAgo = today.AddDays(-6);
            vm.TasksCompletedThisWeek = allMyTasks.Count(t => t.IsCompleted && t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= aWeekAgo);

            // Chart data
            for (int i = 6; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                vm.ChartLabels.Add(d.ToString("dd/MM"));
                vm.ChartData.Add(allMyTasks.Count(t => t.IsCompleted && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == d));
            }

            // Focus Tasks: Due today or Priority High (1), not completed
            vm.FocusTasks = allMyTasks.Where(t => !t.IsCompleted && ((t.DueDate.HasValue && t.DueDate.Value.Date == today) || t.Priority == 1))
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(5)
                .ToList();

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
