using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using To_doList.Data;
using To_doList.Models;

namespace To_doList.Controllers
{
    [Authorize]
    public class TagsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUserModel> _userManager;

        public TagsController(AppDbContext context, UserManager<AppUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tags
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            return View(await _context.Tags.Where(t => t.UserId == userId).ToListAsync());
        }

        // GET: Tags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tags/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TagName,Color")] Tag tag)
        {
            var userId = _userManager.GetUserId(User);
            tag.UserId = userId!;

            if (ModelState.IsValid)
            {
                _context.Add(tag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id && t.UserId == userId);
            
            if (tag == null) return NotFound();
            return View(tag);
        }

        // POST: Tags/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TagId,UserId,TagName,Color")] Tag tag)
        {
            if (id != tag.TagId) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (tag.UserId != userId) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tags.Any(t => t.TagId == tag.TagId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var tag = await _context.Tags.FirstOrDefaultAsync(m => m.TagId == id && m.UserId == userId);
            
            if (tag == null) return NotFound();

            return View(tag);
        }

        // POST: Tags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id && t.UserId == userId);
            
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
