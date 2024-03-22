using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using weSell.DataAccess.Repository.IRepository;
using weSell.Models;
using weSell.Models.ViewModels;
using weSell.Utility;

namespace weSellWeb.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)] 
    public class CategoryController : Controller
        {
            private readonly IUnitOfWork _unitOfWork;
            public CategoryController(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }
            public IActionResult Index()
            {
                List<Category> objCategoryList = _unitOfWork.Category.GetAll().ToList();
                return View(objCategoryList);
            }

            public IActionResult Create()
            {
                return View();
            }
            [HttpPost]
            public IActionResult Create(Category obj)
            {
                if (obj.Name == obj.DisplayOrder.ToString())
                {
                    ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
                }

                if (ModelState.IsValid)
                {
                    _unitOfWork.Category.Add(obj);
                    _unitOfWork.Save();
                    TempData["success"] = "Category created successfully";
                    return RedirectToAction("Index");
                }
                return View();

            }

            public IActionResult Edit(int? id)
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }
                Category? categoryFromDb = _unitOfWork.Category.Get(u => u.Id == id);
                //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id);
                //Category? categoryFromDb2 = _db.Categories.Where(u=>u.Id==id).FirstOrDefault();

                if (categoryFromDb == null)
                {
                    return NotFound();
                }
                return View(categoryFromDb);
            }
            [HttpPost]
            public IActionResult Edit(Category obj)
            {
                if (ModelState.IsValid)
                {
                    _unitOfWork.Category.Update(obj);
                    _unitOfWork.Save();
                    TempData["success"] = "Category updated successfully";
                    return RedirectToAction("Index");
                }
                return View();

            }

        public IActionResult Upsert(int? id)
        {
           
            if (id == null || id == 0)
            {
                //create
                return View();

            }
            else
            {
                //update
                Category? categoryFromDb = _unitOfWork.Category.Get(u => u.Id == id);
                return View(categoryFromDb);
            }

        }
        [HttpPost]
        public IActionResult Upsert(Category obj)
        {
            if (ModelState.IsValid)
            {
                
                if (obj.Id == 0)
                {
                    _unitOfWork.Category.Add(obj);
                    _unitOfWork.Save();
                    TempData["success"] = "Category created successfully";
                }
                else
                {
                    _unitOfWork.Category.Update(obj);
                    _unitOfWork.Save();
                    TempData["success"] = "Category Updated successfully";
                }
               
                return RedirectToAction("Index");
            }
            else
            {
                return View(obj);
            }
        }



        public IActionResult Delete(int? id)
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }
                Category? categoryFromDb = _unitOfWork.Category.Get(u => u.Id == id);

                if (categoryFromDb == null)
                {
                    return NotFound();
                }
                return View(categoryFromDb);
            }
            [HttpPost, ActionName("Delete")]
            public IActionResult DeletePOST(int? id)
            {
                Category? obj = _unitOfWork.Category.Get(u => u.Id == id);
                if (obj == null)
                {
                    return NotFound();
                }
                _unitOfWork.Category.Remove(obj);
                _unitOfWork.Save();
                TempData["success"] = "Category deleted successfully";
                return RedirectToAction("Index");
            }
        }
    
}
