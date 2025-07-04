using servidor.Models;
using servidor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowClientApp", policy => {
        policy.WithOrigins("http://localhost:5177", "https://localhost:7221")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Entity Framework Core - SQLite
builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseSqlite("Data Source=tienda.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseCors("AllowClientApp");

// Migrar DB y poblar datos iniciales si necesario
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
    db.Database.Migrate();

    // Poblar la base de datos si está vacía
    if (!db.Productos.Any())
    {
        db.Productos.AddRange(new List<Producto>
        {
            new Producto { Nombre = "Tubos", Descripcion = "Tubos de ensayo", Precio = 20000, Stock = 10, ImagenUrl = "tubos.jpg" },
            new Producto { Nombre = "Balanza", Descripcion = "Balanza", Precio = 25000, Stock = 10, ImagenUrl = "balanza.jpg" },
            new Producto { Nombre = "Manta", Descripcion = "Manta de Calefaccion", Precio = 20000, Stock = 15, ImagenUrl = "manta2.jpg" },
            new Producto { Nombre = "Matraz", Descripcion = "Matraz", Precio = 20000, Stock = 15, ImagenUrl = "matraz2.jpg" },
            new Producto { Nombre = "Piseta", Descripcion = "Piseta", Precio = 20000, Stock = 15, ImagenUrl = "piseta.jpg" },
            new Producto { Nombre = "Probeta", Descripcion = "Probeta", Precio = 20000, Stock = 15, ImagenUrl = "probeta.jpg" },
            new Producto { Nombre = "Termometro", Descripcion = "Mide la temperatura", Precio = 100000, Stock = 3, ImagenUrl = "termometro.jpg" },
        });
        db.SaveChanges();
    }
}

// =========== ENDPOINTS ===========

// Carritos EN MEMORIA
var carritos = new Dictionary<Guid, List<ItemCarrito>>();

// Ruta raíz
app.MapGet("/", () => "Servidor API en funcionamiento");

// --- PRODUCTOS ---

app.MapGet("/api/productos", async ([FromQuery] string? q, TiendaDbContext db) => {
    var query = db.Productos.AsQueryable();

    if (!string.IsNullOrEmpty(q))
    {
        var qLower = q.ToLower();
        query = query.Where(p =>
            p.Nombre.ToLower().Contains(qLower) ||
            p.Descripcion.ToLower().Contains(qLower)
        );
    }

    var productos = await query.ToListAsync();
    return Results.Ok(productos);
});

app.MapPost("/api/productos/{id}/agregar-stock", async ([FromRoute] int id, [FromQuery] int cantidad, TiendaDbContext db) =>
{
    var producto = await db.Productos.FindAsync(id);
    if (producto is null)
        return Results.NotFound("Producto no encontrado");

    producto.Stock += cantidad;
    await db.SaveChangesAsync();
    return Results.Ok(producto);
});

// --- CARRITOS EN MEMORIA ---

// POST /api/carritos → crea un nuevo carrito
app.MapPost("/api/carritos", () => {
    var id = Guid.NewGuid();
    carritos[id] = new List<ItemCarrito>();
    return Results.Ok(id);
});

// GET /api/carritos/{id}
app.MapGet("/api/carritos/{id}", (Guid id) => {
    if (!carritos.ContainsKey(id))
        return Results.NotFound("Carrito no encontrado");

    return Results.Ok(carritos[id]);
});

// PUT /api/carritos/{id}/{productoId}
app.MapPut("/api/carritos/{id}/{productoId}", async (Guid id, int productoId, TiendaDbContext db) => {
    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null)
        return Results.NotFound("Producto no encontrado");

    if (producto.Stock < 1)
        return Results.BadRequest("Sin stock");

    if (!carritos.ContainsKey(id))
        return Results.NotFound("Carrito no encontrado");

    var carrito = carritos[id];
    var item = carrito.FirstOrDefault(i => i.ProductoId == productoId);
    if (item is null) {
        carrito.Add(new ItemCarrito {
            ProductoId = productoId,
            Cantidad = 1,
            PrecioUnitario = producto.Precio
        });
    } else {
        item.Cantidad++;
    }

    producto.Stock--;
    await db.SaveChangesAsync(); // Solo el stock persiste

    return Results.Ok(carrito);
});

// DELETE /api/carritos/{id}/{productoId}
app.MapDelete("/api/carritos/{id}/{productoId}", async (Guid id, int productoId, TiendaDbContext db) => {
    if (!carritos.ContainsKey(id))
        return Results.NotFound("Carrito no encontrado");

    var carrito = carritos[id];
    var item = carrito.FirstOrDefault(i => i.ProductoId == productoId);
    if (item is null)
        return Results.NotFound("Producto no está en el carrito");

    if (item.Cantidad > 1) {
        item.Cantidad--;
    } else {
        carrito.Remove(item);
    }

    var producto = await db.Productos.FindAsync(productoId);
    if (producto is not null)
        producto.Stock++;

    await db.SaveChangesAsync();
    return Results.Ok(carrito);
});

// DELETE /api/carritos/{id}
app.MapDelete("/api/carritos/{id}", async (Guid id, TiendaDbContext db) => {
    if (!carritos.ContainsKey(id))
        return Results.NotFound("Carrito no encontrado");

    var carrito = carritos[id];
    foreach (var item in carrito)
    {
        var producto = await db.Productos.FindAsync(item.ProductoId);
        if (producto is not null)
            producto.Stock += item.Cantidad;
    }

    carrito.Clear();
    await db.SaveChangesAsync();
    return Results.Ok();
});

// --- COMPRAS ---

app.MapPost("/api/compras", async ([FromQuery] Guid carritoId, [FromBody] Cliente cliente, TiendaDbContext db) => {
    if (!carritos.ContainsKey(carritoId))
        return Results.NotFound("Carrito no encontrado");

    var items = carritos[carritoId];
    if (!items.Any())
        return Results.BadRequest("El carrito está vacío");

    // Buscar o crear cliente
    var clienteDb = db.Clientes.FirstOrDefault(c =>
        c.Nombre == cliente.Nombre &&
        c.Apellido == cliente.Apellido &&
        c.Telefono == cliente.Telefono);

    if (clienteDb == null)
    {
        clienteDb = new Cliente
        {
            Nombre = cliente.Nombre,
            Apellido = cliente.Apellido,
            Telefono = cliente.Telefono
        };
        db.Clientes.Add(clienteDb);
        await db.SaveChangesAsync();
    }

    var compra = new Compra
    {
        Id = Guid.NewGuid(),
        ClienteId = clienteDb.ClienteId,
        Items = items.Select(i => new ItemCarrito
        {
            ProductoId = i.ProductoId,
            Cantidad = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario
        }).ToList(),
        Fecha = DateTime.Now
    };

    db.Compras.Add(compra);
    await db.SaveChangesAsync();

    // Vaciar el carrito
    carritos[carritoId] = new List<ItemCarrito>();

    return Results.Ok(compra);
});

// GET /api/compras
app.MapGet("/api/compras", async (TiendaDbContext db) => {
    var compras = await db.Compras
        .Include(c => c.Cliente)
        .Include(c => c.Items)
        .ToListAsync();

    return Results.Ok(compras);
});

app.UseStaticFiles();
app.Run();