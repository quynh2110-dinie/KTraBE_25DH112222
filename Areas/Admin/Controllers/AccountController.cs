
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        BookStoreEntities db = new BookStoreEntities();

        // Đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            var user = db.User.FirstOrDefault(
                u => u.Username == username);

            if (user != null &&
                user.Password.TrimEnd() == password &&
                user.UserRole.Trim() == "A")
            {
                FormsAuthentication.SetAuthCookie(username, false);

                return RedirectToAction(
                    "Index", "Products",
                    new { area = "Admin" });
            }

            ViewBag.Error = "Sai tài khoản, mật khẩu hoặc không phải Admin!";
            return View();
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
