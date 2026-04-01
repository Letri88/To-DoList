using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using To_doList.Data;
using To_doList.Models;
using System.Security.Claims; 
using Microsoft.AspNetCore.SignalR;
using To_doList.Hubs;

namespace To_doList.Controllers
{
    [Authorize] 
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<TaskHub> _hubContext;

        public TasksController(
            AppDbContext context,
            UserManager<AppUserModel> userManager,
            IWebHostEnvironment env,
            IHubContext<TaskHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _hubContext = hubContext;
        }


        // GET: Tasks
        public async Task<IActionResult> Index(
            string sortOrder,
            string searchString,
            int? priorityFilter,
            int? tagFilter,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber)
        {
            var userId = _userManager.GetUserId(User);

            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["PrioritySortParm"] = sortOrder == "Priority" ? "priority_desc" : "Priority";

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentPriority"] = priorityFilter;
            ViewData["CurrentTag"] = tagFilter;
            ViewData["CurrentStatus"] = statusFilter;
            ViewData["CurrentStartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentEndDate"] = endDate?.ToString("yyyy-MM-dd");

            // Populate Tags for Dropdown
            // We need to fetch tags belonging to the user to populate the dropdown in the View
             var userTags = await _context.Tags.Where(t => t.UserId == userId).ToListAsync();
             ViewBag.Tags = userTags;


            // Filter by current User and NOT deleted
            var tasks = from t in _context.Tasks.Include(t => t.User)
                                              .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
                                              .Include(t => t.Assignments)
                                              .Include(t => t.SubTasks)
                        where (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)) && !t.IsDeleted
                        select t;

            if (!String.IsNullOrEmpty(searchString))
            {
                tasks = tasks.Where(s => s.Title.Contains(searchString) || s.Description.Contains(searchString));
            }

            if (priorityFilter.HasValue)
            {
                tasks = tasks.Where(t => t.Priority == priorityFilter.Value);
            }

            if (tagFilter.HasValue)
            {
                tasks = tasks.Where(t => t.TaskTags.Any(tt => tt.TagId == tagFilter.Value));
            }

             if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "completed")
                {
                    tasks = tasks.Where(t => t.IsCompleted);
                }
                else if (statusFilter == "pending")
                {
                    tasks = tasks.Where(t => !t.IsCompleted);
                }
            }

            if (startDate.HasValue)
            {
                // Assuming filtering by DueDate.
                // You could also filter given a range [startDate, endDate]
                 tasks = tasks.Where(t => t.DueDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                 // Include the whole end date
                 tasks = tasks.Where(t => t.DueDate <= endDate.Value.AddDays(1).AddTicks(-1));
            }

            switch (sortOrder)
            {
                case "Date":
                    tasks = tasks.OrderBy(s => s.DueDate);
                    break;
                case "date_desc":
                    tasks = tasks.OrderByDescending(s => s.DueDate);
                    break;
                case "Priority":
                    tasks = tasks.OrderBy(s => s.Priority);
                    break;
                 case "priority_desc":
                    tasks = tasks.OrderByDescending(s => s.Priority);
                    break;
                default:
                    tasks = tasks.OrderByDescending(s => s.CreatedAt); // Newest first default
                    break;
            }

            int pageSize = 5; // Show 5 tasks per page
            return View(await PaginatedList<To_doList.Models.TodoTask>.CreateAsync(tasks.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Tasks/Trash
        public async Task<IActionResult> Trash()
        {
            var userId = _userManager.GetUserId(User);
            var deletedTasks = await _context.Tasks
                .Where(t => t.UserId == userId && t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(deletedTasks);
        }

        // POST: Tasks/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && t.UserId == userId);
            
            if (task != null)
            {
                task.IsDeleted = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Trash));
        }

        // GET: Tasks/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            ViewData["Tags"] = await _context.Tags.Where(t => t.UserId == userId).ToListAsync();
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Priority,DueDate,StartDate")] TodoTask todoTask,
            int[] selectedTags,
            List<IFormFile> files)
        {
            var userId = _userManager.GetUserId(User);
            todoTask.UserId = userId!;

            if (!ModelState.IsValid)
            {
                ViewData["Tags"] = await _context.Tags
                    .Where(t => t.UserId == userId)
                    .ToListAsync();
                return View(todoTask);
            }

            // 1️⃣ Lưu Task trước để có TaskId
            _context.Tasks.Add(todoTask);
            await _context.SaveChangesAsync();

            // 2️⃣ Lưu Tags
            if (selectedTags != null && selectedTags.Length > 0)
            {
                foreach (var tagId in selectedTags)
                {
                    _context.TaskTags.Add(new TaskTag
                    {
                        TaskId = todoTask.TaskId,
                        TagId = tagId
                    });
                }
            }

            // 3️⃣ Upload & lưu File Attachments
            if (files != null && files.Any())
            {
                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "tasks");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };

                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExts.Contains(ext)) continue; // bỏ file không hợp lệ

                    // (tuỳ chọn) giới hạn 5MB
                    if (file.Length > 5 * 1024 * 1024) continue;

                    var newFileName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(uploadFolder, newFileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.TaskAttachments.Add(new TaskAttachment
                    {
                        TaskId = todoTask.TaskId,
                        FileName = file.FileName,
                        FilePath = "/uploads/tasks/" + newFileName,
                        ContentType = file.ContentType,
                        FileSize = file.Length
                    });
                }
            }

            // 4️⃣ Save tất cả (Tags + Files)
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/QuickAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Title required");
            var userId = _userManager.GetUserId(User);
            
            var task = new TodoTask
            {
                Title = title,
                UserId = userId,
                CreatedAt = DateTime.Now,
                DueDate = DateTime.Now // Tự động set deadline hôm nay để focus
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, taskId = task.TaskId, title = task.Title });
        }

        // POST: Tasks/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == dto.Id && t.UserId == userId);
            if (task == null) return NotFound();

            task.IsCompleted = dto.IsCompleted;
            if (task.IsCompleted)
            {
                task.CompletedAt = DateTime.Now;
            }
            else
            {
                task.CompletedAt = null;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class UpdateStatusDto
        {
            public int Id { get; set; }
            public bool IsCompleted { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var userId = _userManager.GetUserId(User);

            var file = await _context.TaskAttachments
                .Include(f => f.TodoTask)
                .FirstOrDefaultAsync(f => f.Id == id && f.TodoTask.UserId == userId);

            if (file == null) return NotFound();

            var physicalPath = Path.Combine(
                _env.WebRootPath,
                file.FilePath.TrimStart('/')
            );

            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);

            _context.TaskAttachments.Remove(file);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Tasks/ToggleComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var todoTask = await _context.Tasks
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.TaskId == id && (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)));

            if (todoTask != null)
            {
                todoTask.IsCompleted = !todoTask.IsCompleted;
                todoTask.CompletedAt = todoTask.IsCompleted ? DateTime.Now : null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var todoTask = await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.TaskTags)
                .Include(t => t.SubTasks)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.TaskId == id && (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)));
            
            if (todoTask == null)
            {
                return NotFound();
            }

            ViewData["Tags"] = await _context.Tags.Where(t => t.UserId == userId).ToListAsync();
            ViewData["SelectedTags"] = todoTask.TaskTags.Select(tt => tt.TagId).ToList();

            return View(todoTask);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TaskId,UserId,Title,Description,Priority,DueDate,StartDate,IsCompleted,IsDeleted,CreatedAt")] TodoTask todoTask, int[] selectedTags, List<IFormFile> files)
        {
            if (id != todoTask.TaskId)
            {
                return NotFound();
            }
            
            // Verify ownership or assignment
            var userId = _userManager.GetUserId(User);
            var isAssigned = await _context.TaskAssignments.AnyAsync(a => a.TaskId == id && a.UserId == userId);
            if(todoTask.UserId != userId && !isAssigned) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(todoTask);
                    await _context.SaveChangesAsync();

                    // Update Tags
                    var existingTags = _context.TaskTags.Where(tt => tt.TaskId == id);
                    _context.TaskTags.RemoveRange(existingTags);
                    
                    if (selectedTags != null)
                    {
                        foreach (var tagId in selectedTags)
                        {
                            _context.TaskTags.Add(new TaskTag { TaskId = id, TagId = tagId });
                        }
                    }

                    // Handle File Attachments
                    if (files != null && files.Any())
                    {
                        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "tasks");
                        if (!Directory.Exists(uploadFolder))
                            Directory.CreateDirectory(uploadFolder);

                        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };

                        foreach (var file in files)
                        {
                            if (file.Length == 0) continue;

                            var ext = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedExts.Contains(ext)) continue;

                            if (file.Length > 5 * 1024 * 1024) continue;

                            var newFileName = $"{Guid.NewGuid()}{ext}";
                            var savePath = Path.Combine(uploadFolder, newFileName);

                            using (var stream = new FileStream(savePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            _context.TaskAttachments.Add(new TaskAttachment
                            {
                                TaskId = todoTask.TaskId,
                                FileName = file.FileName,
                                FilePath = "/uploads/tasks/" + newFileName,
                                ContentType = file.ContentType,
                                FileSize = file.Length
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TodoTaskExists(todoTask.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Tags"] = await _context.Tags.Where(t => t.UserId == userId).ToListAsync();
            return View(todoTask);
        }


        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
             var userId = _userManager.GetUserId(User);
            var todoTask = await _context.Tasks
                .FirstOrDefaultAsync(m => m.TaskId == id && m.UserId == userId);
            if (todoTask == null) return NotFound();

            return View(todoTask);
        }


        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var todoTask = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && t.UserId == userId);
            if (todoTask != null)
            {
                // Soft Delete
                todoTask.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/DeletePermanent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            var userId = _userManager.GetUserId(User);
            var todoTask = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && t.UserId == userId);
            if (todoTask != null)
            {
                // Hard Delete
                _context.Tasks.Remove(todoTask);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Trash));
        }

        // GET: Tasks/Kanban
        public async Task<IActionResult> Kanban()
        {
            var userId = _userManager.GetUserId(User);
            var tasks = await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.Assignments)
                .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
                .Where(t => (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)) && !t.IsDeleted) // Filter deleted
                .OrderBy(t => t.Priority)
                .ToListAsync();

            return View(tasks);
        }

        // POST: Tasks/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.TaskId == id && (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)));

            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = request.IsCompleted;
            task.CompletedAt = request.IsCompleted ? DateTime.Now : null;
            
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // GET: Tasks/Calendar
        public IActionResult Calendar()
        {
            return View();
        }

        // GET: Tasks/GetTasksForCalendar
        [HttpGet]
        public async Task<IActionResult> GetTasksForCalendar()
        {
            var userId = _userManager.GetUserId(User);
            var tasks = await _context.Tasks
                .Where(t => (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)) && t.DueDate != null && !t.IsDeleted)
                .Select(t => new 
                {
                    id = t.TaskId,
                    title = t.Title,
                    start = t.DueDate,
                    allDay = true, // Simple due date assumes all day
                    url = "/Tasks/Edit/" + t.TaskId,
                    color = t.IsCompleted ? "#10b981" : (t.Priority == 1 ? "#ef4444" : t.Priority == 2 ? "#eab308" : "#3b82f6"), // Green if done, else priority colors
                    extendedProps = new { description = t.Description }
                })
                .ToListAsync();

            return Ok(tasks);
        }


        // POST: Tasks/AddSubTask
        [HttpPost]
        public async Task<IActionResult> AddSubTask(int taskId, string title, DateTime? startDate, DateTime? dueDate)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == userId);
            if (task == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Vui lòng nhập tiêu đề Subtask.");

            if (startDate.HasValue && task.StartDate.HasValue && startDate.Value.Date < task.StartDate.Value.Date)
                return BadRequest($"Ngày bắt đầu không được sớm hơn Task chính ({task.StartDate.Value.ToString("dd/MM/yyyy")}).");
            
            if (dueDate.HasValue && task.DueDate.HasValue && dueDate.Value.Date > task.DueDate.Value.Date)
                return BadRequest($"Ngày kết thúc không được trễ hơn Task chính ({task.DueDate.Value.ToString("dd/MM/yyyy")}).");

            if (startDate.HasValue && dueDate.HasValue && startDate.Value.Date > dueDate.Value.Date)
                return BadRequest("Ngày bắt đầu không được sau ngày kết thúc.");

            var subTask = new SubTask { TaskId = taskId, Title = title, StartDate = startDate, DueDate = dueDate };
            _context.SubTasks.Add(subTask);
            await _context.SaveChangesAsync();

            return Json(new { subTaskId = subTask.SubTaskId, title = subTask.Title, isCompleted = subTask.IsCompleted, startDate = subTask.StartDate, dueDate = subTask.DueDate });
        }

        // POST: Tasks/ToggleSubTask
        [HttpPost]
        public async Task<IActionResult> ToggleSubTask(int subTaskId)
        {
             var userId = _userManager.GetUserId(User);
             var subTask = await _context.SubTasks.Include(st => st.TodoTask)
                                                  .FirstOrDefaultAsync(st => st.SubTaskId == subTaskId && st.TodoTask.UserId == userId);
             
             if (subTask == null) return NotFound();

             subTask.IsCompleted = !subTask.IsCompleted;
             await _context.SaveChangesAsync();
             return Json(new { success = true, isCompleted = subTask.IsCompleted });
        }

        // POST: Tasks/DeleteSubTask
        [HttpPost]
        public async Task<IActionResult> DeleteSubTask(int subTaskId)
        {
             var userId = _userManager.GetUserId(User);
             var subTask = await _context.SubTasks.Include(st => st.TodoTask)
                                                  .FirstOrDefaultAsync(st => st.SubTaskId == subTaskId && st.TodoTask.UserId == userId);
             
             if (subTask == null) return NotFound();

             _context.SubTasks.Remove(subTask);
             await _context.SaveChangesAsync();
             return Json(new { success = true });
        }

        public class UpdateStatusRequest
        {
            public bool IsCompleted { get; set; }
        }

        private bool TodoTaskExists(int id)
        {
            return _context.Tasks.Any(e => e.TaskId == id);
        }

        // --- COLLABORATION APIs ---

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(new List<object>());
            var currentUserId = _userManager.GetUserId(User);
            var dbusers = await _context.Users
                .Where(u => u.Id != currentUserId && (u.UserName.Contains(query) || (u.Email != null && u.Email.Contains(query))))
                .Take(5)
                .Select(u => new { id = u.Id, userName = u.UserName, email = u.Email, avatar = u.Avatar })
                .ToListAsync();
            return Json(dbusers);
        }

        [HttpPost]
        public async Task<IActionResult> AddAssignee(int taskId, string assigneeId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == currentUserId);
            if (task == null) return NotFound("Task not found or you don't have permission.");

            if (!await _context.TaskAssignments.AnyAsync(a => a.TaskId == taskId && a.UserId == assigneeId))
            {
                _context.TaskAssignments.Add(new TaskAssignment { TaskId = taskId, UserId = assigneeId });
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAssignee(int taskId, string assigneeId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == currentUserId);
            if (task == null) return NotFound("Task not found or you don't have permission.");

            var assignment = await _context.TaskAssignments.FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == assigneeId);
            if (assignment != null)
            {
                _context.TaskAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int taskId, [FromForm] string content)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.TaskId == taskId && (t.UserId == userId || t.Assignments.Any(a => a.UserId == userId)));
            
            if (task == null) return NotFound();
            if (string.IsNullOrWhiteSpace(content)) return BadRequest("Content cannot be empty.");

            var comment = new Comment
            {
                TaskId = taskId,
                UserId = userId!,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);

            var commentData = new { 
                commentId = comment.CommentId, 
                content = comment.Content, 
                createdAt = comment.CreatedAt.ToString("g"),
                userName = user?.UserName,
                avatar = user?.Avatar,
                userId = userId
            };

            await _hubContext.Clients.Group($"Task_{taskId}").SendAsync("ReceiveComment", commentData);

            return Json(new { success = true });
        }
    }
}
