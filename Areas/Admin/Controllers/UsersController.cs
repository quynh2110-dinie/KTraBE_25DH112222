
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PagedList;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    public class UsersController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // DANH SACH + TIM KIEM + PHAN TRANG
        public ActionResult Index(string search, int? page)
        {
            var users = db.User
                .Where(u => u.UserRole.Trim() == "A");

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(
                    u => u.Username.Contains(search));
            }

            ViewBag.Search = search;

            int pageNumber = page ?? 1;

            if (pageNumber < 1)
                pageNumber = 1;

            return View(users
                .OrderBy(u => u.Username)
                .ToPagedList(pageNumber, 10));
        }

        // CHI TIET
        public ActionResult Details(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            User user = db.User.Find(id);

            if (user == null ||
                (user.UserRole ?? "").Trim() != "A")
                return HttpNotFound();

            return View(user);
        }

        // THEM TAI KHOAN - GET
        public ActionResult Create()
        {
            return View();
        }

        // THEM TAI KHOAN - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "Username,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                if (db.User.Any(
                    u => u.Username == user.Username))
                {
                    ModelState.AddModelError(
                        "Username",
                        "Tên đăng nhập đã tồn tại.");
                }
                else if (string.IsNullOrWhiteSpace(
                    user.Password))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Vui lòng nhập mật khẩu.");
                }
                else if (user.Password.Length > 50)
                {
                    ModelState.AddModelError(
                        "Password",
                        "Mật khẩu không được quá 50 ký tự.");
                }
                else
                {
                    user.UserRole = "A";

                    db.User.Add(user);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(user);
        }

        // SUA TAI KHOAN - GET
        public ActionResult Edit(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            User user = db.User.Find(id);

            if (user == null ||
                (user.UserRole ?? "").Trim() != "A")
                return HttpNotFound();

            return View(user);
        }

        // SUA TAI KHOAN - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "Username,Password")] User model)
        {
            User user = db.User.Find(model.Username);

            if (user == null ||
                (user.UserRole ?? "").Trim() != "A")
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui lòng nhập mật khẩu.");
            }
            else if (model.Password.Length > 50)
            {
                ModelState.AddModelError(
                    "Password",
                    "Mật khẩu không được quá 50 ký tự.");
            }
            else
            {
                user.Password = model.Password;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            model.UserRole = "A";
            return View(model);
        }

        // XOA TAI KHOAN - GET
        public ActionResult Delete(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            User user = db.User.Find(id);

            if (user == null ||
                (user.UserRole ?? "").Trim() != "A")
                return HttpNotFound();

            return View(user);
        }

        // XOA TAI KHOAN - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            User user = db.User.Find(id);

            if (user == null ||
                (user.UserRole ?? "").Trim() != "A")
                return HttpNotFound();

            if (user.Username == User.Identity.Name)
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            if (user.Customer.Any())
            {
                TempData["Error"] =
                    "Tài khoản đang liên kết với khách hàng.";

                return RedirectToAction("Index");
            }

            db.User.Remove(user);
            db.SaveChanges();

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
