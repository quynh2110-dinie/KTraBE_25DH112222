
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private BookStoreEntities db = new BookStoreEntities();

        // DANH SACH - TIM KIEM - SAP XEP - PHAN TRANG
        public ActionResult Index(string search, int? categoryId,
            string sortOrder, int? page)
        {
            var products = db.Product.Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(search));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryID == categoryId.Value);
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

                case "newest":
                    products = products.OrderByDescending(
                        p => p.ProductID);
                    break;

                default:
                    products = products.OrderBy(
                        p => p.ProductName);
                    break;
            }

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.SortOrder = sortOrder;

            ViewBag.Categories = new SelectList(
                db.Category.OrderBy(c => c.CategoryName).ToList(),
                "CategoryID", "CategoryName", categoryId);

            int pageNumber = page.GetValueOrDefault(1);
            if (pageNumber < 1)
                pageNumber = 1;

            int pageSize = 10;

            return View(products.ToPagedList(pageNumber, pageSize));
        }

        // CHI TIET SAN PHAM
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Product product = db.Product
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id.Value);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // HIEN THI FORM THEM
        public ActionResult Create()
        {
            LoadCategories();
            return View(new Product());
        }

        // THEM SAN PHAM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include =
                "CategoryID,ProductName,ProductDecription,ProductPrice,Quantity")]
            Product product,
            HttpPostedFileBase imageFile)
        {
            string error = ValidateImage(imageFile);

            if (imageFile == null || imageFile.ContentLength == 0)
            {
                ModelState.AddModelError(
                    "imageFile", "Vui lòng chọn ảnh sản phẩm.");
            }
            else if (error != null)
            {
                ModelState.AddModelError("imageFile", error);
            }

            if (product.Quantity < 0)
            {
                ModelState.AddModelError(
                    "Quantity", "Số lượng không được âm.");
            }

            if (product.ProductPrice < 0)
            {
                ModelState.AddModelError(
                    "ProductPrice", "Giá không được âm.");
            }

            // Anh duoc gan sau khi upload thanh cong
            ModelState.Remove("ProductImage");

            if (ModelState.IsValid)
            {
                string savedFile = null;

                try
                {
                    savedFile = SaveImage(imageFile);

                    product.ProductImage = savedFile;
                    product.SoldQuantity = 0;

                    db.Product.Add(product);
                    db.SaveChanges();

                    TempData["Success"] =
                        "Thêm sản phẩm thành công!";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    DeleteImageFile(savedFile);
                    ModelState.AddModelError(
                        "", "Không thể lưu sản phẩm. Vui lòng kiểm tra dữ liệu.");
                }
            }

            LoadCategories(product.CategoryID);
            return View(product);
        }

        // HIEN THI FORM SUA
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Product product = db.Product.Find(id);

            if (product == null)
                return HttpNotFound();

            LoadCategories(product.CategoryID);
            return View(product);
        }

        // CAP NHAT SAN PHAM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
                "ProductID,CategoryID,ProductName,ProductDecription,ProductPrice,Quantity")]
            Product product,
            HttpPostedFileBase imageFile)
        {
            Product existing = db.Product.Find(product.ProductID);

            if (existing == null)
                return HttpNotFound();

            string error = ValidateImage(imageFile);

            if (error != null)
                ModelState.AddModelError("imageFile", error);

            if (product.Quantity < 0)
                ModelState.AddModelError(
                    "Quantity", "Số lượng không được âm.");

            if (product.ProductPrice < 0)
                ModelState.AddModelError(
                    "ProductPrice", "Giá không được âm.");

            // Khong cho form sua ghi de ten anh va so luong da ban
            ModelState.Remove("ProductImage");
            ModelState.Remove("SoldQuantity");

            if (ModelState.IsValid)
            {
                string newFile = null;

                try
                {
                    if (imageFile != null &&
                        imageFile.ContentLength > 0)
                    {
                        newFile = SaveImage(imageFile);
                    }

                    existing.CategoryID = product.CategoryID;
                    existing.ProductName = product.ProductName;
                    existing.ProductDecription =
                        product.ProductDecription;
                    existing.ProductPrice = product.ProductPrice;
                    existing.Quantity = product.Quantity;

                    if (newFile != null)
                        existing.ProductImage = newFile;

                    db.SaveChanges();

                    TempData["Success"] =
                        "Cập nhật sản phẩm thành công!";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    DeleteImageFile(newFile);
                    ModelState.AddModelError(
                        "", "Không thể cập nhật sản phẩm.");
                }
            }

            // Hien thi lai anh cu khi form co loi
            product.ProductImage = existing.ProductImage;
            product.SoldQuantity = existing.SoldQuantity;

            LoadCategories(product.CategoryID);
            return View(product);
        }

        // XAC NHAN XOA
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Product product = db.Product.Find(id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // XOA SAN PHAM
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Product.Find(id);

            if (product == null)
                return HttpNotFound();

            try
            {
                db.Product.Remove(product);
                db.SaveChanges();

                TempData["Success"] =
                    "Xóa sản phẩm thành công!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa sản phẩm vì đã có dữ liệu liên quan.";
            }

            return RedirectToAction("Index");
        }

        // DANH SACH DANH MUC CHO COMBOBOX
        private void LoadCategories(int? selectedId = null)
        {
            ViewBag.CategoryID = new SelectList(
                db.Category.OrderBy(c => c.CategoryName).ToList(),
                "CategoryID",
                "CategoryName",
                selectedId);
        }

        // KIEM TRA FILE ANH
        private string ValidateImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            if (file.ContentLength > 5 * 1024 * 1024)
                return "Ảnh không được vượt quá 5MB.";

            string extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            string[] allowed = {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowed.Contains(extension))
                return "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.";

            return null;
        }

        // LUU FILE ANH
        private string SaveImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            string error = ValidateImage(file);

            if (error != null)
                throw new InvalidOperationException(error);

            string extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string folder =
                Server.MapPath("~/Content/images/");

            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, fileName);

            file.SaveAs(path);

            return fileName;
        }

        private void DeleteImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            string path = Path.Combine(
                Server.MapPath("~/Content/images/"),
                Path.GetFileName(fileName));

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
