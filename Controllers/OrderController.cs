
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class OrderController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        private Customer CurrentCustomer()
        {
            if (!Request.IsAuthenticated)
                return null;

            return db.Customer.FirstOrDefault(
                c => c.Username == User.Identity.Name);
        }

        public ActionResult Checkout()
        {
            var customer = CurrentCustomer();

            if (customer == null)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Checkout", "Order") });

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            ViewBag.Customer = customer;
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(
            string addressDelivery,
            string paymentMethod,
            string deliveryMethod)
        {
            var customer = CurrentCustomer();

            if (customer == null)
                return RedirectToAction("Login", "Account");

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            if (string.IsNullOrWhiteSpace(addressDelivery) ||
                string.IsNullOrWhiteSpace(paymentMethod) ||
                string.IsNullOrWhiteSpace(deliveryMethod))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Checkout");
            }

            if (paymentMethod != "COD")
            {
                TempData["Error"] = "Hình thức thanh toán không hợp lệ.";
                return RedirectToAction("Checkout");
            }

            if (deliveryMethod != "Standard" &&
                deliveryMethod != "Express")
            {
                TempData["Error"] = "Hình thức giao hàng không hợp lệ.";
                return RedirectToAction("Checkout");
            }

            using (var transaction = db.Database.BeginTransaction(
                System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var orderItems = new List<OrderDetail>();
                    decimal total = 0;

                    foreach (var item in cart)
                    {
                        Product product = db.Product.Find(item.ProductID);

                        if (product == null ||
                            item.Quantity <= 0 ||
                            product.Quantity < item.Quantity)
                        {
                            transaction.Rollback();
                            TempData["Error"] =
                                "Một số sản phẩm đã hết hàng hoặc không đủ số lượng.";
                            return RedirectToAction("Index", "Cart");
                        }

                        decimal subTotal =
                            product.ProductPrice * item.Quantity;

                        total += subTotal;

                        orderItems.Add(new OrderDetail
                        {
                            ProductID = product.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = product.ProductPrice,
                            SubTotal = subTotal
                        });

                        product.Quantity -= item.Quantity;
                        product.SoldQuantity += item.Quantity;
                    }

                    var order = new Order
                    {
                        CustomerID = customer.CustomerID,
                        OrderDate = DateTime.Now,
                        TotalAmount = total,
                        PaymentStatus = "Chua thanh toan",
                        PaymentMethod = paymentMethod,
                        DeliveryMethod = deliveryMethod,
                        OrderStatus = "Cho xac nhan",
                        AddressDelivery = addressDelivery.Trim()
                    };

                    db.Order.Add(order);
                    db.SaveChanges();

                    foreach (var detail in orderItems)
                    {
                        detail.OrderID = order.OrderID;
                        db.OrderDetail.Add(detail);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    Session["Cart"] = new List<CartItem>();

                    return RedirectToAction(
                        "OrderSuccess", new { id = order.OrderID });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] =
                        "Không thể đặt hàng. Vui lòng thử lại.";
                    return RedirectToAction("Checkout");
                }
            }
        }

        public ActionResult OrderSuccess(int? id)
        {
            var customer = CurrentCustomer();

            if (customer == null)
                return RedirectToAction("Login", "Account");

            if (!id.HasValue)
                return RedirectToAction("OrderHistory");

            var order = db.Order
                .Include(o => o.OrderDetail.Select(d => d.Product))
                .FirstOrDefault(o =>
                    o.OrderID == id.Value &&
                    o.CustomerID == customer.CustomerID);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        public ActionResult OrderHistory()
        {
            var customer = CurrentCustomer();

            if (customer == null)
                return RedirectToAction("Login", "Account");

            var orders = db.Order
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
