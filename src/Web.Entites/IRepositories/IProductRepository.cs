namespace Web.Entites.IRepositories;
public interface IProductRepository
{
    Task<OneOf<List<ValidationError>, bool>> AddProductAsync(CreateProductVM model, CancellationToken cancellationToken = default);
    Task<PaginatedList<ProductReponseForAdmin>> GetAllProductsAdminAsync(FilterRequest request, CancellationToken cancellationToken = default);
    Task<DiscoverProductVM> GetDiscoverProductByIdAsync(string userId, int id, CancellationToken cancellationToken = default);
    Task<NewArrivalProductsVM> GetNewArrivalProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<NewArrivalProductsVM>> GetNewArrivalProductsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<BestSellingProductVM>> GetBestSellingProductsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<DiscoverProductVM>> GetDiscoverProductsAsync(string userId, CancellationToken cancellationToken = default);
    Task<PaginatedList<DiscoverProductVM>> GetAllProductsSortedByAsync(string userId,string sortedBy, int pageNumber, CancellationToken cancellationToken = default);
    Task<PaginatedList<DiscoverProductVM>> GetAllProductsInCategoryAsync(int pageNumber, int categoryId, CancellationToken cancellationToken = default);
    Task<EditProductVM?> GetProductEditByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductReponseForAdmin?> GetProductDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OneOf<List<ValidationError>, bool>> UpdateProductAsync(EditProductVM model, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task<PaginatedList<DiscoverProductVM>> SearchInProductsInHomeAsync(string query, CancellationToken cancellationToken = default);
    Task<List<DiscoverProductVM>> GetRecommendationsProducts(string userId, CancellationToken cancellationToken = default);

    Task RemoveKeys(CancellationToken cancellationToken = default);
}