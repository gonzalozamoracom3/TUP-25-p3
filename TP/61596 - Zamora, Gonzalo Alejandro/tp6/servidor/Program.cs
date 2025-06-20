using tp6.Data;
using tp6.Models;               // Para acceder a las clases dentro de tp6.Models
using Microsoft.EntityFrameworkCore;  // Para trabajar con Entity Framework Core (EF Core)
using System;                   // Para tipos básicos como Guid, DateTime
using System.Collections.Generic;  // Para trabajar con colecciones como List
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
var policyName = "AllowAll";

// CORS: permite llamadas desde el cliente Blazor
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: policyName,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configurar EF Core con SQLite
builder.Services.AddDbContext<TiendaDbContext>(opt =>
    opt.UseSqlite("Data Source=tienda.db"));

var app = builder.Build();

// Aplicar CORS
app.UseCors(policyName);

// Ruta de prueba
app.MapGet("/", () => "Servidor API funcionando :D");

// Asegura que la base de datos exista
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
    db.Database.EnsureCreated();
}

// ------------------- ENDPOINTS API -------------------

// POST /carrito - Crear nuevo carrito
app.MapPost("/carrito", async (TiendaDbContext db) =>
{
    var carrito = new Carrito { CarritoId = Guid.NewGuid(), CarritoItems = new List<CarritoItem>() };
    db.Carritos.Add(carrito);
    await db.SaveChangesAsync();
    return Results.Ok(carrito.CarritoId);
});
// GET /productos?q=busqueda
app.MapGet("/productos", async (TiendaDbContext db, string? q) =>
{
    var productos = db.Productos.AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
        productos = productos.Where(p => p.Nombre.ToLower().StartsWith(q.ToLower()));

    return await productos.ToListAsync();
});

// PUT /carrito/{carritoId}/{productoId}?cantidad=x - Agregar o modificar ítem en carrito
app.MapPut("/carrito/{carritoId:guid}/{productoId:int}", async (
    Guid carritoId,
    int productoId,
    [FromQuery] int cantidad,
    TiendaDbContext context) =>
{
    if (cantidad == 0)
        return Results.BadRequest("Cantidad inválida");

    var carrito = await context.Carritos
        .Include(c => c.CarritoItems)
        .FirstOrDefaultAsync(c => c.CarritoId == carritoId);

    if (carrito == null)
        return Results.NotFound("Carrito no encontrado");

    var producto = await context.Productos.FindAsync(productoId);
    if (producto == null)
        return Results.NotFound("Producto no encontrado");

    var itemExistente = carrito.CarritoItems.FirstOrDefault(i => i.ProductoId == productoId);
    int cantidadActual = itemExistente?.Cantidad ?? 0;
    int nuevaCantidad = cantidadActual + cantidad;

    if (nuevaCantidad > producto.Stock)
        return Results.BadRequest($"No hay suficiente stock para '{producto.Nombre}'");

    if (itemExistente != null)
    {
        itemExistente.Cantidad = nuevaCantidad;

        if (itemExistente.Cantidad <= 0)
            carrito.CarritoItems.Remove(itemExistente);
    }
    else if (cantidad > 0)
    {
        carrito.CarritoItems.Add(new CarritoItem
        {
            ProductoId = productoId,
            Cantidad = cantidad
        });
    }

    await context.SaveChangesAsync();
    return Results.Ok();
});
// GET /carrito/{carritoId} - Obtener ítems del carrito
app.MapGet("/carrito/{carritoId:guid}", async (Guid carritoId, TiendaDbContext db) =>
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

// GET /carrito/{carritoId}/items - Obtener ítems del carrito con detalles (nuevo endpoint)
app.MapGet("/carrito/{carritoId:guid}/items", async (Guid carritoId, TiendaDbContext db) =>
{
    var carrito = await db.Carritos
        .Include(c => c.CarritoItems)
        .ThenInclude(ci => ci.Producto)
        .FirstOrDefaultAsync(c => c.CarritoId == carritoId);

    if (carrito is null)
        return Results.NotFound();

    var resultado = carrito.CarritoItems.Select(ci => new
    {
        ci.ProductoId,
        Nombre = ci.Producto?.Nombre ?? "",
        Precio = ci.Producto?.Precio ?? 0,
        ci.Cantidad,
        Subtotal = (ci.Producto?.Precio ?? 0) * ci.Cantidad
    });

    return Results.Ok(resultado);
});

// DELETE /carrito/{carritoId} - Vaciar carrito
app.MapDelete("/carrito/{carritoId:guid}", async (Guid carritoId, TiendaDbContext db) =>
{
    var items = db.CarritoItems.Where(ci => ci.CarritoId == carritoId);
    db.CarritoItems.RemoveRange(items);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE /carrito/{carritoId}/{productoId} - Eliminar producto del carrito
app.MapDelete("/carrito/{carritoId:guid}/{productoId:int}", async (Guid carritoId, int productoId, TiendaDbContext db) =>
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

// PUT /carrito/{carritoId}/confirmar - Confirmar compra
app.MapPut("/carrito/{carritoId:guid}/confirmar", async (Guid carritoId, TiendaDbContext db) =>
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

// GET /carrito/{carritoId}/cantidad - Obtener cantidad total de ítems en el carrito
app.MapGet("/carrito/{carritoId:guid}/cantidad", (Guid carritoId, TiendaDbContext db) =>
{
    var cantidad = db.CarritoItems
        .Where(ci => ci.CarritoId == carritoId)
        .Sum(ci => ci.Cantidad);

    return Results.Ok(cantidad);
});

app.Run();