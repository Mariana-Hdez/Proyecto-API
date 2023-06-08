﻿using Microsoft.AspNetCore.Mvc;
using APIWebNWind.Data;
using APIWebNWind.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static NuGet.Packaging.PackagingConstants;

namespace APIWebNWind.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : Controller
    {
        private readonly NorthwindContext contexto;

        public ProductController(NorthwindContext context)
        {
            contexto = context;
        }

        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return contexto.Products.OrderBy(p => p.ProductName);

        }

        


        // Devuelve los mejores 5 productos del trimestre
        [HttpGet]
        [Route("GetCincoProductos/{year}")]
        public IEnumerable<object> GetCincoProductos(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = startDate.AddYears(1).AddDays(-1);

            var result = new List<object>();


            //iteración por los 4 trimestres del año
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var quarterlyResult = contexto.Products
                    .Join(contexto.Orderdetails,
                        p => p.ProductId,
                        od => od.ProductId,
                        (p, od) => new { Product = p, OrderDetail = od })
                    .Join(contexto.Orders,
                        join1 => join1.OrderDetail.OrderId,
                        o => o.OrderId,
                        (join1, o) => new { join1.Product, join1.OrderDetail, Order = o })
                    //Filtración de rango de fechas y trimestre actual
                    .Where(join2 => join2.Order.OrderDate.Value >= startDate &&
                                     join2.Order.OrderDate.Value <= endDate &&
                                     ((join2.Order.OrderDate.Value.Month - 1) / 3 + 1) == quarter)
                    .GroupBy(join2 => new { join2.Product.ProductName, Quarter = quarter })
                    .Select(group => new
                    {
                        Nombre = group.Key.ProductName,
                        Trimestre = group.Key.Quarter,
                        UnidadesVendidas = group.Sum(g => g.OrderDetail.Quantity)
                    })
                    .OrderByDescending(item => item.UnidadesVendidas)
                    .Take(5);

                result.AddRange(quarterlyResult);
            }

            return result;
        }

        //Devuelve una lista de productos no descontinuados
        [HttpGet]
        [Route("GetProductsNodiscontinued")]
        public IEnumerable<object> GetProductsNodiscontinued()
        {
            var result =
                from c in contexto.Categories
                join p in contexto.Products on c.CategoryId equals p.CategoryId
                where p.Discontinued == false
                select new
                {
                    Nombre=p.ProductName,
                    Categoria=c.CategoryName,
                    Existencias=p.UnitsInStock
                };
            return result;
        }

        [HttpGet]
        [Route("GetProductsNodiscontinued2")]
        public IEnumerable<object> GetProductsNodiscontinued2()
        {
            var result = contexto.Products
                .Where(p=>p.Discontinued==false)
                .Join(contexto.Categories,
                     (p) => p.CategoryId,
                     (c) => c.CategoryId,
                     (p, c) =>
                         new
                         {
                             Nombre = p.ProductName,
                             Categoria = c.CategoryName,
                             Existencias = p.UnitsInStock
                         }
                     );

            return result;

        }


        [HttpGet]
        [Route("GetNameAndPrice")]
        public IEnumerable<object> GetNameAndPrice()
        {
            IEnumerable<object> lista =
                from producto in contexto.Products
                select new
                {
                    Name = producto.ProductName,
                    Price = producto.UnitPrice,
                    Category=producto.Category.CategoryName
                };

            return lista;
        }

        [HttpGet]
        [Route("GetNameAndPrice2")]
        public IEnumerable<Product> GetNameAndPrice2()
        {
            Product p= new Product()
            {
                ProductName = "A",
                UnitPrice = 1
            };

            Product p1 = new Product();
            p1.ProductName = "A";
            p1.UnitPrice = 1;
            


            IEnumerable<Product> listaP =
                from producto in contexto.Products
                select new Product()
                {
                    ProductName = producto.ProductName,
                    UnitPrice = producto.UnitPrice
                };
            return listaP;
        }

        [HttpGet]
        [Route("GetInventarioCategoria")]
        public IEnumerable<object> GetInventarioCategoria()
        {
            IEnumerable<object> lista =
                contexto.Products.
                Join(contexto.Categories,
                (p)=>p.CategoryId,
                (c) => c.CategoryId,
                (p, c)=>
                    new
                    {
                        Categoria = c.CategoryName,
                        Existencia = p.UnitsInStock
                    }
                ).GroupBy(pc=>pc.Categoria)
                .Select(grupo=>
                    new { 
                        Categoria=grupo.Key,
                        Inventario=grupo.Sum(g=>g.Existencia)
                    }
                );

            return lista;
        }

        //Query de venta por año de un producto
        [HttpGet]
        [Route("SalesBreakdownByMonth")]
        public IEnumerable<SalesByMonth> GetSalesBreakdownByMonth(string productName, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = startDate.AddYears(1).AddDays(-1);
            var query = from o in contexto.Orders
                            join od in contexto.Orderdetails on o.OrderId equals od.OrderId
                            join p in contexto.Products on od.ProductId equals p.ProductId
                            where p.ProductName == productName && o.OrderDate >= startDate && o.OrderDate <= endDate
                            group od by new { ProductName = p.ProductName, Year = o.OrderDate.Value.Year, Month = o.OrderDate.Value.Month } into g
                            select new SalesByMonth
                            {
                                Year = g.Key.Year,
                                Month = g.Key.Month,
                                ProductName = g.Key.ProductName,
                                TotalSales = (decimal)g.Sum(od => od.Quantity * od.UnitPrice)
                            };

                return query.ToList();
        }

        [HttpGet]
        [Route("GetPeriodo/{startDate}/{endDate}")]
        public IEnumerable<object> GetPeriodo(string startDate, string endDate)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            var result = contexto.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .Join(contexto.Orderdetails,
                    order => order.OrderId,
                    orderDetail => orderDetail.OrderId,
                    (order, orderDetail) => new { order.OrderDate, orderDetail.UnitPrice, orderDetail.Quantity, orderDetail.ProductId })
                .Join(contexto.Products,
                    od => od.ProductId,
                    product => product.ProductId,
                    (od, product) => new { od.OrderDate, product.UnitPrice, od.Quantity })
                .GroupBy(o => new { Year = o.OrderDate.Value.Year, Month = o.OrderDate.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, TotalPrice = g.Sum(o => o.UnitPrice * o.Quantity) })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            return result;
        }

        public class SalesByMonth
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string ProductName { get; set; }
            public decimal TotalSales { get; set; }
        }


    }
}
