using Microsoft.AspNetCore.Mvc;
using ShoppingSite.Data;

namespace ShoppingSite.ViewComponents
{
    public class CategoryList:ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryList(ApplicationDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var values= _context.Categories.ToList();
            return View(values);
        }
    }
}
