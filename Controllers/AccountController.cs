
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // DANG KY
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            if (username.Length > 255 ||
                model.Password.Length > 50 ||
                model.CustomerPhone.Length > 15)
            {
                ModelState.AddModelError("",
                    "Tên đăng nhập, mật khẩu hoặc số điện thoại quá dài.");
                return View(model);
            }

            if (db.User.Any(u => u.Username == username))
            {
                ModelState.AddModelError("Username",
                    "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var user = new User
                    {
                        Username = username,
                        Password = model.Password,
                        UserRole = "C"
                    };

                    db.User.Add(user);
                    db.SaveChanges();

                    var customer = new Customer
                    {
                        Username = username,
                        CustomerName = model.CustomerName,
                        CustomerPhone = model.CustomerPhone,
                        CustomerEmail = model.CustomerEmail,
                        CustomerAddress = model.CustomerAddress
                    };

                    db.Customer.Add(customer);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Success"] =
                        "Đăng ký thành công. Vui lòng đăng nhập.";

                    return RedirectToAction("Login");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError("",
                        "Đăng ký không thành công. Vui lòng thử lại.");

                    return View(model);
                }
            }
        }

        // DANG NHAP
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(
            CustomerLoginVM model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            var user = db.User.FirstOrDefault(
                u => u.Username == username);

            if (user == null ||
                user.Password.Trim() != model.Password ||
                user.UserRole.Trim() != "C")
            {
                ModelState.AddModelError("",
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!db.Customer.Any(c => c.Username == username))
            {
                ModelState.AddModelError("",
                    "Không tìm thấy thông tin khách hàng.");
                return View(model);
            }

            FormsAuthentication.SetAuthCookie(username, false);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // DANG XUAT
        [HttpGet]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Remove("Cart");

            return RedirectToAction("Index", "Home");
        }

        // THONG TIN TAI KHOAN
        [Authorize]
        [HttpGet]
        public ActionResult Edit()
        {
            var customer = db.Customer.FirstOrDefault(
                c => c.Username == User.Identity.Name);

            if (customer == null)
                return HttpNotFound();

            var model = new CustomerEditVM
            {
                CustomerID = customer.CustomerID,
                Username = customer.Username,
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CustomerEditVM model)
        {
            var customer = db.Customer.FirstOrDefault(
                c => c.Username == User.Identity.Name);

            if (customer == null)
                return HttpNotFound();

            model.CustomerID = customer.CustomerID;
            model.Username = customer.Username;

            if (!ModelState.IsValid)
                return View(model);

            if (model.CustomerPhone.Length > 15)
            {
                ModelState.AddModelError("CustomerPhone",
                    "Số điện thoại không được quá 15 ký tự.");
                return View(model);
            }

            customer.CustomerName = model.CustomerName;
            customer.CustomerPhone = model.CustomerPhone;
            customer.CustomerEmail = model.CustomerEmail;
            customer.CustomerAddress = model.CustomerAddress;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật thông tin thành công.";

            return RedirectToAction("Edit");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
