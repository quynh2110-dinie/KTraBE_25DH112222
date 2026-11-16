
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PagedList;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // DANH SACH DON HANG - TIM KIEM - LOC - PHAN TRANG
        public ActionResult Index(
            string search,
            string paymentStatus,
            DateTime? orderDate,
            string sortOrder,
            int? page)
        {
            var orders = db.Order
                .Include(o => o.Customer)
                .AsQueryable();

            // Tim theo ten khach hang
            if (!string.IsNullOrWhiteSpace(search))
            {
                orders = orders.Where(o =>
                    o.Customer.CustomerName.Contains(search));
            }

            // Loc trang thai thanh toan
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                orders = orders.Where(o =>
                    o.PaymentStatus == paymentStatus);
            }

            // Loc ngay dat hang
            if (orderDate.HasValue)
            {
                DateTime start = orderDate.Value.Date;
                DateTime end = start.AddDays(1);

                orders = orders.Where(o =>
                    o.OrderDate >= start &&
                    o.OrderDate < end);
            }

            // Sap xep
            switch (sortOrder)
            {
                case "date_asc":
                    orders = orders.OrderBy(o => o.OrderDate)
                                   .ThenBy(o => o.OrderID);
                    break;

                case "total_asc":
                    orders = orders.OrderBy(o => o.TotalAmount)
                                   .ThenBy(o => o.OrderID);
                    break;

                case "total_desc":
                    orders = orders.OrderByDescending(
                        o => o.TotalAmount)
                        .ThenByDescending(o => o.OrderID);
                    break;

                default:
                    orders = orders.OrderByDescending(
                        o => o.OrderDate)
                        .ThenByDescending(o => o.OrderID);
                    break;
            }

            ViewBag.Search = search;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.OrderDate = orderDate.HasValue
                ? orderDate.Value.ToString("yyyy-MM-dd")
                : "";
            ViewBag.SortOrder = sortOrder;

            ViewBag.PaymentStatuses = db.Order
                .Where(o => o.PaymentStatus != null)
                .Select(o => o.PaymentStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            int pageNumber = page.GetValueOrDefault(1);

            if (pageNumber < 1)
                pageNumber = 1;

            int pageSize = 15;

            return View(orders.ToPagedList(pageNumber, pageSize));
        }

        // CHI TIET DON HANG
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            // Tai khach hang, chi tiet don hang va san pham
            Order order = db.Order
                .Include(o => o.Customer)
                .Include(o => o.OrderDetail.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id.Value);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // CAP NHAT TRANG THAI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(
            int id,
            string orderStatus,
            string paymentStatus)
        {
            Order order = db.Order.Find(id);

            if (order == null)
                return HttpNotFound();

            string[] allowedOrderStatuses =
            {
                "Pending",
                "Confirmed",
                "Shipping",
                "Completed",
                "Cancelled"
            };

            string[] allowedPaymentStatuses =
            {
                "Pending",
                "Paid",
                "Failed",
                "Refunded"
            };

            if (!allowedOrderStatuses.Contains(orderStatus) ||
                !allowedPaymentStatuses.Contains(paymentStatus))
            {
                TempData["Error"] =
                    "Trạng thái không hợp lệ.";

                return RedirectToAction("Details",
                    new { id = id });
            }

            order.OrderStatus = orderStatus;
            order.PaymentStatus = paymentStatus;

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật trạng thái đơn hàng thành công!";

            return RedirectToAction("Details",
                new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
