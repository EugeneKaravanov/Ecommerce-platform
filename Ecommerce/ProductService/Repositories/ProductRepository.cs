using ProductServiceGRPC;
using ProductService.Models;
using Dapper;
using Npgsql;
using ProductService.Utilities;
using Azure.Core;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Collections.Generic;

namespace ProductService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Page<ProductWithId>> GetProductsAsync(GetProductsRequest request, CancellationToken cancellationToken = default)
        {
            int chosenPageNumber;
            string sqlStringToCreateFiltredProductsCTE = @"WITH filtred_products AS (
                                                            SELECT * FROM Products 
                                                            WHERE (@NameFilter IS null OR Name ILIKE @NameFilter) 
                                                            AND (@MinPriceFilter IS null OR Price >= @MinPriceFilter) 
                                                            AND (@MaxPriceFilter IS null OR Price <= @MaxPriceFilter))";
            string sqlStringToGetFiltredProductsCount = sqlStringToCreateFiltredProductsCTE + "SELECT COUNT(*) FROM filtred_products;";
            string sqlStringToGetProductsOnPage = sqlStringToCreateFiltredProductsCTE;
            await using var connection = new NpgsqlConnection(_connectionString);
            string nameFilter = request.NameFilter == null ? null : $"%{request.NameFilter}%";

            await connection.OpenAsync(cancellationToken);

            int totalElementsCount =  await connection.QuerySingleAsync<int>(sqlStringToGetFiltredProductsCount, 
                new 
                {
                    NameFilter = nameFilter,
                    MinPriceFilter = (int?)request.MinPriceFilter, 
                    MaxPriceFilter = (int?)request.MaxPriceFilter 
                });
            int elementsOnPageCount = request.ElementsOnPageCount > 0 ? request.ElementsOnPageCount : 1;
            int totalPagesCount = (int)Math.Ceiling(totalElementsCount / (double)elementsOnPageCount);

            if (request.ChoosenPageNumber < 1)
                chosenPageNumber = 1;
            else if (request.ChoosenPageNumber > totalPagesCount)
                chosenPageNumber = totalPagesCount;
            else chosenPageNumber = request.ChoosenPageNumber;

            sqlStringToGetProductsOnPage = FormSqlStringToGetProductsOnPage(sqlStringToGetProductsOnPage, request.SortArgument, request.IsReverseSort);

            var tempProducts = await connection.QueryAsync<ProductWithId>(sqlStringToGetProductsOnPage, 
                new 
                { 
                        NameFilter = nameFilter, 
                        MinPriceFilter = (int?)request.MinPriceFilter, 
                        MaxPriceFilter = (int?)request.MaxPriceFilter,
                        SkipCount = elementsOnPageCount * (chosenPageNumber - 1), 
                        Count = elementsOnPageCount
                });
            List<ProductWithId> products = tempProducts.ToList();

            return new Page<ProductWithId>(totalElementsCount, totalPagesCount, chosenPageNumber, elementsOnPageCount, products);
        }

        public async Task<ResultWithValue<ProductWithId>> GetProductAsync(int id, CancellationToken cancellationToken = default)
        {
            ResultWithValue<ProductWithId> result = new();
            ProductWithId productWithId;
            string sqlString = "SELECT * FROM Products WHERE id = @Id LIMIT 1";
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);
            productWithId = await connection.QuerySingleOrDefaultAsync<ProductWithId>(sqlString, new {Id = id});

            if (productWithId != null)
            {
                result.Status = Models.Status.Success;
                result.Value = productWithId;

                return result;
            }
            else
            {
                result.Status = Models.Status.NotFound;
                result.Message = $"Продукт c ID {id} отсутствует в базе данных!";

                return result;
            }
        }

        public async Task<Result> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            Result result = new();
            string sqlString = @"
                                WITH insert_result AS 
                                (
                                    INSERT INTO Products (name, description, price, stock)
                                    VALUES (@Name, @Description, @Price, @Stock)
                                    ON CONFLICT (name) DO NOTHING
                                    RETURNING id
                                )
                                SELECT id FROM insert_result";

            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);
            int? insertId = await connection.QuerySingleOrDefaultAsync<int?>(sqlString, product);

            if (insertId != null)
            {
                result.Status = Models.Status.Success;
                result.Message = "Продукт успешно добавлен!";

                return result;
            }
            else
            {
                result.Status = Models.Status.Failure;
                result.Message = "Не удалось добавить продукт, так как его имя уже используется!";

                return result;
            }
        }

        public async Task<Result> UpdateProductAsync(int id, Product product, CancellationToken cancellationToken = default)
        {
            Result result = new();
            ProductWithId productWithId = Mapper.TansferProductAndIdToProductWithId(id, product);
            string sqlString = @"WITH update_result AS
                                    (
                                    UPDATE Products SET
                                        Name = @Name,
                                        Description = @Description,
                                        Price = @Price,
                                        Stock = @Stock
                                        WHERE Id = @Id
                                        RETURNING Id
                                     )
                                SELECT CASE 
                                    WHEN EXISTS (SELECT 1 FROM update_result) THEN 'SUCCESS'
                                END;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            try
            {
                string? updateStatus = await connection.QuerySingleOrDefaultAsync<string?>(sqlString, productWithId);

                if (updateStatus == "SUCCESS")
                {
                    result.Status = Models.Status.Success;
                    result.Message = "Продукт успешно добавлен!";

                    return result;
                }
                else
                {
                    result.Status = Models.Status.NotFound;
                    result.Message = $"Продукт c ID {id} отсутствует в базе данных!";

                    return result;
                }
            }
            catch (NpgsqlException ex) when (ex.SqlState == "23505")
            {
                result.Status = Models.Status.Failure;
                result.Message = "Не удалось обновить продукт, так как его имя уже используется!";

                return result;
            }
        }

        public async Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
        {
            Result result = new();
            string sqlString = @"
                                WITH delete_result AS 
                                (
                                    DELETE FROM Products
                                    WHERE Id = @Id
                                    RETURNING Id
                                )
                                SELECT id FROM delete_result";

            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);
            int? deleteId = await connection.QuerySingleOrDefaultAsync<int?>(sqlString, new {Id = id});

            if (deleteId != null)
            {
                result.Status = Models.Status.Success;
                result.Message = "Продукт успешно удален!";

                return result;
            }
            else
            {
                result.Status = Models.Status.Failure;
                result.Message = $"Не удалось удалить продукт, так как продукта с ID {id} несуществует!";

                return result;
            }
        }

        public async Task<ResultWithValue<List<OutputOrderProduct>>> TakeProducts(TakeProductsRequest request, CancellationToken cancellationToken = default)
        {
            ResultWithValue<List<OutputOrderProduct>> result = new();
            List<InputOrderProduct> takingProducts = Mapper.TransferTakeProductsRequestToIncomingOrderProductList(request);
            result.Value = new List<OutputOrderProduct>();
            await using var connection = new NpgsqlConnection(_connectionString);
            string sqlStringForGetProductAndBlockString = @"SELECT * FROM Products
                                                           WHERE id = @Id
                                                           FOR UPDATE LIMIT 1;";
            string sqlStringForChangeStockProuct = @"UPDATE Products SET
                                                    stock = stock - @Quantity
                                                    WHERE id = @Id;";

            await connection.OpenAsync(cancellationToken);

            using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            foreach (InputOrderProduct product in takingProducts)
            {
                ProductWithId productWithId = await connection.QuerySingleOrDefaultAsync<ProductWithId>(sqlStringForGetProductAndBlockString, new { Id = product.ProductId });

                if (productWithId == null)
                {
                    await transaction.RollbackAsync();
                    result.Status = Models.Status.Failure;
                    result.Message = "Не удалось сформировать заказа, так как некоторые продукты отсустствуют в базе данных!";
                    result.Value.Clear();

                    return result;
                }

                if (productWithId.Stock < product.Quantity)
                {
                    await transaction.RollbackAsync();
                    result.Status = Models.Status.Failure;
                    result.Message = "Не удалось сформировать заказа, так как остатков некоторых продуктов недостаточно для формирования заказа!";
                    result.Value.Clear();

                    return result;
                }

                await connection.ExecuteAsync(sqlStringForChangeStockProuct, new { Id = product.ProductId, Quantity = product.Quantity});
                result.Value.Add(new OutputOrderProduct { ProductId = product.ProductId, Quantity = product.Quantity, UnitPrice = productWithId.Price });
            }

            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            result.Status = Models.Status.Success;

            return result;
        }

        private static string FormSqlStringToGetProductsOnPage(string baseString, string sortArgument, bool isReverseSort)
        {
            if (sortArgument != "Name" && sortArgument != "Price")
            {
                return baseString + @"SELECT * FROM filtred_products OFFSET @SkipCount LIMIT @Count;";
            }
            else
            {
                if (isReverseSort == false)
                    return baseString + $"SELECT * FROM filtred_products ORDER BY {sortArgument} OFFSET @SkipCount LIMIT @Count;";
                else
                    return baseString + $"SELECT * FROM filtred_products ORDER BY {sortArgument} DESC OFFSET @SkipCount LIMIT @Count;";
            }
        }
    }
}