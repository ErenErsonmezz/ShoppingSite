using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingSite.Data;
using ShoppingSite.Models;
using System.Data;

namespace ShoppingSite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRole.Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users=_context.ApplicationUsers.ToList();
            var role=_context.Roles.ToList();
            var userRole=_context.UserRoles.ToList();
            foreach (var user in users)
            {
                var roleId = userRole.FirstOrDefault(x => x.UserId==user.Id).RoleId;
                user.Role=role.FirstOrDefault(x=>x.Id==roleId).Name;
            }

            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(String id)
        {
            var user =await _context.Users.FindAsync(id);
            return View(user);
        }
        [HttpPost,ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
