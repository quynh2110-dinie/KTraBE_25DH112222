
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PagedList;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    public class CustomersController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // DANH SACH - TIM KIEM - SAP XEP - PHAN TRANG
        public ActionResult Index(string search, string sortOrder, int? page)
        {
            var customers = db.Customer
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                customers = customers.Where(c =>
                    c.CustomerName.Contains(search) ||
                    c.CustomerPhone.Contains(search) ||
                    c.Username.Contains(search) ||
                    (c.CustomerEmail != null &&
                     c.CustomerEmail.Contains(search)));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    customers = customers.OrderByDescending(
                        c => c.CustomerName);
                    break;

                case "newest":
                    customers = customers.OrderByDescending(
                        c => c.CustomerID);
                    break;

                case "oldest":
                    customers = customers.OrderBy(
                        c => c.CustomerID);
                    break;

                default:
                    customers = customers.OrderBy(
                        c => c.CustomerName);
                    break;
            }

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;

            int pageNumber = page.GetValueOrDefault(1);

            if (pageNumber < 1)
                pageNumber = 1;

            int pageSize = 10;

            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        // XEM CHI TIET
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Customer customer = db.Customer
                .Include(c => c.User)
                .FirstOrDefault(c => c.CustomerID == id.Value);

            if (customer == null)
                return HttpNotFound();

            return View(customer);
        }

        // HIEN THI FORM THEM
        public ActionResult Create()
        {
            LoadUsers();
            return View();
        }

        // THEM KHACH HANG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include =
                "CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username")]
            Customer customer)
        {
            if (!db.User.Any(u => u.Username == customer.Username))
            {
                ModelState.AddModelError(
                    "Username", "Tài khoản không tồn tại.");
            }

            if (ModelState.IsValid)
            {
                db.Customer.Add(customer);
                db.SaveChanges();

                TempData["Success"] =
                    "Thêm khách hàng thành công!";

                return RedirectToAction("Index");
            }

            LoadUsers(customer.Username);
            return View(customer);
        }

        // HIEN THI FORM SUA
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Customer customer = db.Customer.Find(id);

            if (customer == null)
                return HttpNotFound();

            LoadUsers(customer.Username);
            return View(customer);
        }

        // CAP NHAT KHACH HANG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
                "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress")]
            Customer customer)
        {
            Customer existing = db.Customer.Find(customer.CustomerID);

            if (existing == null)
                return HttpNotFound();

            // Username khong thay doi tai form sua thong tin
            ModelState.Remove("Username");

            if (ModelState.IsValid)
            {
                existing.CustomerName = customer.CustomerName;
                existing.CustomerPhone = customer.CustomerPhone;
                existing.CustomerEmail = customer.CustomerEmail;
                existing.CustomerAddress = customer.CustomerAddress;

                db.SaveChanges();

                TempData["Success"] =
                    "Cập nhật khách hàng thành công!";

                return RedirectToAction("Index");
            }

            customer.Username = existing.Username;
            LoadUsers(existing.Username);

            return View(customer);
        }

        // HIEN THI XAC NHAN XOA
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Customer customer = db.Customer.Find(id);

            if (customer == null)
                return HttpNotFound();

            return View(customer);
        }

        // XOA KHACH HANG
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Customer customer = db.Customer.Find(id);

            if (customer == null)
                return HttpNotFound();

            try
            {
                db.Customer.Remove(customer);
                db.SaveChanges();

                TempData["Success"] =
                    "Xóa khách hàng thành công!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa khách hàng vì đã có đơn hàng hoặc dữ liệu liên quan.";
            }

            return RedirectToAction("Index");
        }

        // DANH SACH TAI KHOAN
        private void LoadUsers(string selectedUsername = null)
        {
            ViewBag.Username = new SelectList(
                db.User.OrderBy(u => u.Username).ToList(),
                "Username",
                "Username",
                selectedUsername);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
