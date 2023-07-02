using Humanizer;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingSite.Data;
using ShoppingSite.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace ShoppingSite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger
            ,ApplicationDbContext context,
             UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var values=_context.Products.Where(x=>x.IsHome==true).ToList();
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                var count = _context.ShoppingCarts.Where(x => x.ApplicationUser == user).Count();
                HttpContext.Session.SetInt32("Shopping Cart Session", count);
            }
            return View(values);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var values=_context.Products.FirstOrDefault(x=>x.Id==id);
            ShoppingCart cart = new ShoppingCart()
            {
                ProductId = values.Id,
                Product = values
            };
           
            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart scart)
        {
            scart.Id = 0;
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            scart.UserId = user.Id;
            scart.ApplicationUser=_context.ApplicationUsers.Find(user.Id);
            scart.Product = _context.Products.FirstOrDefault(x => x.Id == scart.ProductId);
            if (ModelState.IsValid)
            {
               
                ShoppingCart cart = _context.ShoppingCarts.FirstOrDefault(
                    x=>x.UserId==user.Id && x.ProductId==scart.ProductId);
                if(cart==null)
                {
                    _context.ShoppingCarts.Add(scart);
                }
                else
                {
                    cart.Count = scart.Count;
                }
                _context.SaveChanges();
                var count=_context.ShoppingCarts.Where(x=>x.ApplicationUser==scart.ApplicationUser).ToList().Count();
                HttpContext.Session.SetInt32("Shopping Cart Session", count);
                return RedirectToAction("Index");
            }
            else
            {
               
                return View(scart);
            }
           
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var user=await _userManager.GetUserAsync(User);
            ShoppingCartWithOrderModel model = new ShoppingCartWithOrderModel()
            {
                Carts = _context.ShoppingCarts.Where(x => x.UserId == user.Id).Include(x => x.Product),
                OrderHeader=new OrderHeader()
            };
            foreach (var item in model.Carts)
            {
                model.OrderHeader.OrderTotal += item.Count * item.Product.Price;
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Summary(ShoppingCartWithOrderModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            model.Carts=_context.ShoppingCarts.Where(x=>x.UserId==user.Id).Include(x=>x.Product).ToList();
            model.OrderHeader.OrderDate = DateTime.Now;
            model.OrderHeader.OrderStatus = "Sipariş Onaylanmayı Bekliyor";
            model.OrderHeader.ApplicationUser = await _context.ApplicationUsers.FindAsync(user.Id);
            model.OrderHeader.UserId = user.Id;
            foreach (var item in model.Carts)
            {
                item.Price = _context.Products.FirstOrDefault(x=>x.Id==item.Product.Id).Price;
                OrderDetails orderDetails = new OrderDetails()
                {
                    OrderHeader = model.OrderHeader,
                    OrderId = model.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count,
                    ProductId=item.ProductId,
                    Product=item.Product
                };
                model.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                _context.OrderDetails.AddAsync(orderDetails);
            }
            var payment = PaymentProcessAsync(model);
            _context.OrderHeaders.AddAsync(model.OrderHeader);
            _context.ShoppingCarts.RemoveRange(model.Carts);
            _context.SaveChanges();
            HttpContext.Session.SetInt32("Shopping Cart Session", 0);
            return RedirectToAction("OrderSuccess");
        }

        private async Task<Payment> PaymentProcessAsync(ShoppingCartWithOrderModel model)
        {
            Options options = new Options();
            options.ApiKey = "sandbox-1Wj4gWHYMPLhPx7jBETz1S82Amje00s2";
            options.SecretKey = "sandbox-d0X7EZIsT1wMsbZTo7gfbY59KfkDucwA";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = new Random().Next(1111,9999).ToString();
            request.Price = model.OrderHeader.OrderTotal.ToString();
            request.PaidPrice = model.OrderHeader.OrderTotal.ToString();
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B67832";
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = model.OrderHeader.CartName;
            paymentCard.CardNumber = model.OrderHeader.CartName;
            paymentCard.ExpireMonth = model.OrderHeader.ExpirationMonth;
            paymentCard.ExpireYear = model.OrderHeader.ExpirationYear;
            paymentCard.Cvc = model.OrderHeader.Cvc;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer();
            buyer.Id = model.OrderHeader.Id.ToString();
            buyer.Name = model.OrderHeader.Name;
            buyer.Surname = model.OrderHeader.Surname;
            buyer.GsmNumber = model.OrderHeader.Phone;
            buyer.Email = model.OrderHeader.ApplicationUser.Email;
            buyer.IdentityNumber = "74300864791";
            buyer.LastLoginDate = "2015-10-05 12:43:35";
            buyer.RegistrationDate = "2013-04-21 15:12:09";
            buyer.RegistrationAddress = model.OrderHeader.Address;
            buyer.Ip = "85.34.78.112";
            buyer.City = model.OrderHeader.City;
            buyer.Country = "Turkey";
            buyer.ZipCode = model.OrderHeader.PostalCode;
            request.Buyer = buyer;

            Address shippingAddress = new Address();
            shippingAddress.ContactName = "Jane Doe";
            shippingAddress.City = "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            shippingAddress.ZipCode = "34742";
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address();
            billingAddress.ContactName = "Jane Doe";
            billingAddress.City = "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            billingAddress.ZipCode = "34742";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();
            var user = await _userManager.GetUserAsync(User);
            foreach (var item in model.Carts)
            {
                basketItems.Add(new BasketItem()
                {
                    Id = item.Id.ToString(),
                    Name = item.Product.Name,
                    Price = (item.Count * item.Product.Price).ToString(),
                    Category1 = item.Product.Category.ToString(),
                    ItemType=BasketItemType.PHYSICAL.ToString(),
                }) ;

            }
            
                
            

            request.BasketItems = basketItems;

            return Payment.Create(request, options);
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }
        public IActionResult CategoryDetails(int id)
        {
            var values=_context.Products.Where(x=>x.CategoryId== id).ToList();  
            return View(values);
        }
        public IActionResult Search(string q)
        {
            var values=_context.Products.Where(x=>x.Name.Contains(q) || x.Description.Contains(q));
            return View(values);
        }
    }
}