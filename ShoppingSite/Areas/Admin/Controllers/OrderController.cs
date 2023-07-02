using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingSite.Data;
using ShoppingSite.Models;

namespace ShoppingSite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user=await _userManager.GetUserAsync(User);
            IEnumerable<OrderHeader> orders;
            if (User.IsInRole(ApplicationRole.Admin))
            {
                orders = _context.OrderHeaders.ToList();
            }
            else
            {
                orders= _context.OrderHeaders.Where(x=>x.UserId==user.Id).Include(x=>x.ApplicationUser);
            }

            return View(orders);
        }
        [HttpGet]
        public IActionResult OrderDetails(int id)
        {
            OrderHeader orderHeader= _context.OrderHeaders.FirstOrDefault(x=>x.Id==id);
            IEnumerable<OrderDetails> orderDetails= _context.OrderDetails.Where(x=>x.OrderId==id).Include(x=>x.Product);
            OrderDetailsViewModel model = new OrderDetailsViewModel()
            {
                OrderDetails = orderDetails
            };
            model.OrderHeader = orderHeader;
            return View(model);
        }
        public async Task<IActionResult> StatusNotconfirmed()
        {
            var user = await _userManager.GetUserAsync(User);
            IEnumerable<OrderHeader> orders;
            if (User.IsInRole(ApplicationRole.Admin))
            {
                orders = _context.OrderHeaders.Where(x => x.OrderStatus == "Sipariş Onaylanmayı Bekliyor").ToList();
            }
            else
            {
                orders = _context.OrderHeaders.Where(x => x.UserId == user.Id).Where(x => x.OrderStatus == "Sipariş Onaylanmayı Bekliyor").Include(x => x.ApplicationUser);
            }

            return View(orders);
        }
        public async Task<IActionResult> StatusConfirmed()
        {
            var user = await _userManager.GetUserAsync(User);
            IEnumerable<OrderHeader> orders;
            if (User.IsInRole(ApplicationRole.Admin))
            {
                orders = _context.OrderHeaders.Where(x => x.OrderStatus == "Sipariş Hazırlanıyor").ToList();
            }
            else
            {
                orders = _context.OrderHeaders.Where(x => x.UserId == user.Id).Where(x => x.OrderStatus == "Sipariş Hazırlanıyor").Include(x => x.ApplicationUser);
            }

            return View(orders);
        }
        public async Task<IActionResult> StatusShipped()
        {
            var user = await _userManager.GetUserAsync(User);
            IEnumerable<OrderHeader> orders;
            if (User.IsInRole(ApplicationRole.Admin))
            {
                orders = _context.OrderHeaders.Where(x => x.OrderStatus == "Sipariş Kargolandı").ToList();
            }
            else
            {
                orders = _context.OrderHeaders.Where(x => x.UserId == user.Id).Where(x => x.OrderStatus == "Sipariş Kargolandı").Include(x => x.ApplicationUser);
            }

            return View(orders);
        }
        public IActionResult Approve(int id)
        {
            OrderHeader order = _context.OrderHeaders.Find(id);
            order.OrderStatus = "Sipariş Hazırlanıyor";
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Shipped(int id)
        {
            OrderHeader order = _context.OrderHeaders.Find(id);
            order.OrderStatus = "Sipariş Kargolandı";
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
