
using tp6.Data;
using tp6.Models;               // Para acceder a las clases dentro de tp6.Models
using Microsoft.EntityFrameworkCore;  // Para trabajar con Entity Framework Core (EF Core)
using System;                   // Para tipos básicos como Guid, DateTime
using System.Collections.Generic;  // Para trabajar con colecciones como List

/*
var builder = WebApplication.CreateBuilder(args);

// Agregar servicios CORS para permitir solicitudes desde el cliente
builder.Services.AddCors(options => {
    options.AddPolicy("AllowClientApp", policy => {
        policy.WithOrigins("http://localhost:5177", "https://localhost:7221")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Agregar controladores si es necesario
builder.Services.AddControllers();

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

// Usar CORS con la política definida
app.UseCors("AllowClientApp");

// Mapear rutas básicas
app.MapGet("/", () => "Servidor API está en funcionamiento");

// Ejemplo de endpoint de API
app.MapGet("/api/datos", () => new { Mensaje = "Datos desde el servidor", Fecha = DateTime.Now });
*/




//comienzo del codigo mio

var builder = WebApplication.CreateBuilder(args);
 
// Configurar SQLite y EF Core
builder.Services.AddDbContext<TiendaDbContext>(opt =>
    opt.UseSqlite("Data Source=tienda.db"));

var app = builder.Build();
app.MapGet("/",()=>"servidor api esta funcionando :D");
// Asegura creación de base de datos y datos iniciales
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
    db.Database.EnsureCreated();
}

// Endpoints

// GET /productos
app.MapGet("/productos", async (TiendaDbContext db, string? q) =>
{
    var productos = db.Productos.AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
        productos = productos.Where(p => p.Nombre.Contains(q));

    return await productos.ToListAsync();
});

// POST /carritos
app.MapPost("/carritos", async (TiendaDbContext db) =>
{
    var carrito = new Carrito { CarritoId = Guid.NewGuid(), CarritoItems = new List<CarritoItem>() };
    db.Carritos.Add(carrito);
    await db.SaveChangesAsync();
    return Results.Ok(carrito.CarritoId);
});

// GET /carritos/{carritoId}
app.MapGet("/carritos/{carritoId}", async (Guid carritoId, TiendaDbContext db) =>
{
    var items = await db.CarritoItems
        .Where(ci => ci.CarritoId == carritoId)
        .Include(ci => ci.Producto)
        .ToListAsync();

    return items.Select(ci => new
    {
        ci.ProductoId,
        ci.Producto.Nombre,
        ci.Producto.Precio,
        ci.Cantidad,
        Subtotal = ci.Cantidad * ci.Producto.Precio
    });
});

// DELETE /carritos/{carritoId}
app.MapDelete("/carritos/{carritoId}", async (Guid carritoId, TiendaDbContext db) =>
{
    var items = db.CarritoItems.Where(ci => ci.CarritoId == carritoId);
    db.CarritoItems.RemoveRange(items);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// PUT /carritos/{carritoId}/{productoId}
app.MapPut("/carritos/{carritoId}/{productoId}", async (Guid carritoId, int productoId, int cantidad, TiendaDbContext db) =>
{
    var producto = await db.Productos.FindAsync(productoId);
    if (producto == null || producto.Stock < cantidad)
        return Results.BadRequest("Stock insuficiente");

    var item = await db.CarritoItems
        .FirstOrDefaultAsync(ci => ci.CarritoId == carritoId && ci.ProductoId == productoId);

    if (item == null)
    {
        item = new CarritoItem
        {
            CarritoId = carritoId,
            ProductoId = productoId,
            Cantidad = cantidad
        };
        db.CarritoItems.Add(item);
    }
    else
    {
        item.Cantidad = cantidad;
    }

    await db.SaveChangesAsync();
    return Results.Ok();
});

// DELETE /carritos/{carritoId}/{productoId}
app.MapDelete("/carritos/{carritoId}/{productoId}", async (Guid carritoId, int productoId, TiendaDbContext db) =>
{
    var item = await db.CarritoItems
        .FirstOrDefaultAsync(ci => ci.CarritoId == carritoId && ci.ProductoId == productoId);

    if (item != null)
    {
        db.CarritoItems.Remove(item);
        await db.SaveChangesAsync();
    }

    return Results.NoContent();
});

// PUT /carritos/{carritoId}/confirmar
app.MapPut("/carritos/{carritoId}/confirmar", async (Guid carritoId, TiendaDbContext db) =>
{
    var items = await db.CarritoItems
        .Where(ci => ci.CarritoId == carritoId)
        .Include(ci => ci.Producto)
        .ToListAsync();

    if (!items.Any())
        return Results.BadRequest("Carrito vacío");

    foreach (var item in items)
    {
        if (item.Producto.Stock < item.Cantidad)
            return Results.BadRequest($"Stock insuficiente para {item.Producto.Nombre}");
        item.Producto.Stock -= item.Cantidad;
    }

    var compra = new Compra
    {
        Id = Guid.NewGuid(),
        Fecha = DateTime.UtcNow,
        Total = items.Sum(i => i.Producto.Precio * i.Cantidad),
        Items = items.Select(i => new ItemCompra
        {
            ProductoId = i.ProductoId,
            Cantidad = i.Cantidad,
            PrecioUnitario = i.Producto.Precio
        }).ToList()
    };

    db.Compras.Add(compra);
    db.CarritoItems.RemoveRange(items);
    await db.SaveChangesAsync();

    return Results.Ok("Compra confirmada");
});

app.Run();

