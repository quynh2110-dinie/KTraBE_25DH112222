
using System;
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using PagedList;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // TRANG CHU
        public ActionResult Index(string search)
        {
            var products = db.Product
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(search));
            }

            ViewBag.Search = search;

            return View(products
                .OrderByDescending(p => p.ProductID)
                .Take(20)
                .ToList());
        }

        // DANH SACH SAN PHAM
        public ActionResult ProductList(
            int? categoryId,
            string search,
            string sortOrder,
            int? page)
        {
            var products = db.Product
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(search));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(
                        p => p.ProductName);
                    break;

                case "price_asc":
                    products = products.OrderBy(
                        p => p.ProductPrice);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(
                        p => p.ProductPrice);
                    break;

                default:
                    products = products.OrderBy(
                        p => p.ProductName);
                    break;
            }

            ViewBag.Categories = db.Category
                .OrderBy(c => c.CategoryName)
                .ToList();

            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;

            int pageNumber = page ?? 1;

            if (pageNumber < 1)
                pageNumber = 1;

            return View(products.ToPagedList(pageNumber, 9));
        }

        // CHI TIET SAN PHAM
        public ActionResult ProductDetail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Product product = db.Product
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id.Value);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
