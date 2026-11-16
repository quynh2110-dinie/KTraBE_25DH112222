
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class CartController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        private List<CartItem> GetCart()
        {
            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }

            return cart;
        }

        public ActionResult Index()
        {
            return View(GetCart());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            Product product = db.Product.Find(id);

            if (product == null)
                return HttpNotFound();

            if (quantity < 1)
                quantity = 1;

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == id);

            int currentQuantity = item == null ? 0 : item.Quantity;

            if (currentQuantity + quantity > product.Quantity)
            {
                TempData["Error"] = "Số lượng vượt quá tồn kho.";
                return RedirectToAction(
                    "ProductDetail", "Home", new { id = id });
            }

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductImage = product.ProductImage,
                    ProductPrice = product.ProductPrice,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == id);

            if (item == null)
                return RedirectToAction("Index");

            Product product = db.Product.Find(id);

            if (product == null)
            {
                cart.Remove(item);
                return RedirectToAction("Index");
            }

            if (quantity < 1 || quantity > product.Quantity)
            {
                TempData["Error"] = "Số lượng không hợp lệ.";
                return RedirectToAction("Index");
            }

            item.Quantity = quantity;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == id);

            if (item != null)
                cart.Remove(item);

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
