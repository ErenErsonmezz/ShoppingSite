using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ShoppingSite.Data;
using ShoppingSite.Models;
using System.Text.Encodings.Web;
using System.Text;
using System.Drawing.Printing;

namespace ShoppingSite.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public CartController(ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
        {

            _context = context;
            _userManager = userManager;
            _emailSender=emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            ShoppingCartWithOrderModel model = new ShoppingCartWithOrderModel()
            {
                OrderHeader = new Models.OrderHeader(),
                Carts = _context.ShoppingCarts.Where(x => x.UserId == user.Id).Include(x => x.Product).ToList(),
            };
            model.OrderHeader.OrderTotal=0;
            model.OrderHeader.ApplicationUser =await _context.ApplicationUsers.FindAsync(user.Id);
            foreach (var item in model.Carts)
            {
                model.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                
            }
            return View(model);
        }
        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            var user =await _userManager.FindByNameAsync(User.Identity.Name);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code},
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            ModelState.AddModelError(string.Empty,"Email doğrulama kodu gönder...");
            return RedirectToAction("Success");
        }
        public IActionResult Success()
        {
            return View();
        }
        public IActionResult Delete(int cartId)
        {
            ShoppingCart cart=_context.ShoppingCarts.Find(cartId);
            _context.ShoppingCarts.Remove(cart);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Increase(int cartId)
        {
            ShoppingCart cart = _context.ShoppingCarts.Find(cartId);
            cart.Count += 1;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Decrease(int cartId)
        {
            ShoppingCart cart = _context.ShoppingCarts.Find(cartId);
            if(cart.Count == 1) 
            {
               _context.ShoppingCarts.Remove(cart);
                
                _context.SaveChanges();
                var count =(int)HttpContext.Session.GetInt32("Shopping Cart Session");
                HttpContext.Session.SetInt32("Shopping Cart Session",count-1);
                return RedirectToAction("Index");
            }
            cart.Count -= 1;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
