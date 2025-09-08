namespace WearUp.Web.Controllers;

public class ProductController(IProductRepository _productRepositoy) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(FilterRequest request)
    {
        var response=await _productRepositoy.GetAllProductsAdminAsync(request);
        ViewData["SearchTerm"] = request.SearchTerm;
        ViewData["SortField"] = request.SortField;
        ViewData["SortOrder"] = request.SortOrder;
        return View(response);
    }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> LoadRecommendations(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var products=await _productRepositoy.GetRecommendationsProducts(userId!, cancellationToken);
        return PartialView("_RecommendationsPartial", products);
    }

    [HttpGet]
    public async Task<IActionResult> LoadDiscover(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? null;
        var discovers = await _productRepositoy.GetDiscoverProductsAsync(userId,cancellationToken);
        return PartialView("_DiscoverPartial", discovers);
    }
    [HttpGet]
    public async Task<IActionResult> LoadNewArrivals(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? null;
        var newArrivals=await _productRepositoy.GetNewArrivalProductsAsync(userId,cancellationToken);
        return PartialView("_NewArrivalPartial", newArrivals);
    }
    [HttpGet]
    public async Task<IActionResult> LoadBestSellers(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? null;
        var bestSellers = await _productRepositoy.GetBestSellingProductsAsync(userId,cancellationToken);
        return PartialView("_BestSellersPartial", bestSellers);
    }
    [HttpGet]
    public async Task<IActionResult> AllProductsBasedOnSort(string sortedBy,int pageNumber,CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? null;
        var products = await _productRepositoy.GetAllProductsSortedByAsync(userId,sortedBy,pageNumber,cancellationToken);
        return View(products);
    }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Discover(int id, CancellationToken cancellationToken)
    {
        var userId=User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _productRepositoy.GetDiscoverProductByIdAsync(userId!,id, cancellationToken);
        return View(product);
    }
    [HttpGet]
    public async Task<IActionResult> GetAllInCategory(int pageNumber,int categoryId, CancellationToken cancellationToken)
    {
        var products = await _productRepositoy.GetAllProductsInCategoryAsync(pageNumber,categoryId, cancellationToken);

        return View("AllProductsBasedOnSort", products);
    }

    [HttpGet]
    // create action for search products
    public async Task<IActionResult> Search(string searchTerm, CancellationToken cancellationToken)
    {
        var products = await _productRepositoy.SearchInProductsInHomeAsync(searchTerm, cancellationToken);
        return View("AllProductsBasedOnSort", products);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductVM model,CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result=await _productRepositoy.AddProductAsync(model, cancellationToken);
        if(result.IsT1)
        {
            TempData["SuccessMessage"] = "Data Created Successfully";
            return RedirectToAction("Index");
        }
        else
        {
            result.AsT0.ForEach(error => ModelState.AddModelError(error.PropertyName, error.ErrorMessage));
            TempData["ErrorMessage"] = "Data Not Created";
            return View(model);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product= await _productRepositoy.GetProductEditByIdAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Data Not Updated";
            return RedirectToAction(nameof(Index));
        }
        return View(product);

    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAsync(EditProductVM model,CancellationToken cancellationToken)
    {
        if(!ModelState.IsValid)
            return View(model);

        var result = await _productRepositoy.UpdateProductAsync(model, cancellationToken);

        if (result.IsT1)
        {
            TempData["SuccessMessage"] = "Data Updated Successfully";
            return RedirectToAction(nameof(Index));
        }

        var errors = result.AsT0;
        foreach (var error in errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        TempData["ErrorMessage"] = "Data Not Updated";
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result=await _productRepositoy.DeleteProductAsync(id);
        if(result)
            return Json(new { success = true, message = "Data Deleted Successfully" });
        else
            return Json(new { success = false, message = "Data Not Deleted" });
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var product = await _productRepositoy.GetProductDetailsByIdAsync(id, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Data Not Found";
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }
}
