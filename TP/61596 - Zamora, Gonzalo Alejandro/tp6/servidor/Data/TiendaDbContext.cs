using tp6.Models;               // Para acceder a las clases dentro de tp6.Models
using Microsoft.EntityFrameworkCore;  // Para trabajar con Entity Framework Core (EF Core)
using System;                   // Para tipos básicos como Guid, DateTime
using System.Collections.Generic;  // Para trabajar con colecciones como List

namespace tp6.Data
{
    public class TiendaDbContext : DbContext
    {
        public TiendaDbContext(DbContextOptions<TiendaDbContext> options) : base(options) { }

        // Define las propiedades DbSet para las entidades
        public DbSet<Producto> Productos { get; set; }      // Define Productos
        public DbSet<Carrito> Carritos { get; set; }        // Define Carritos
        public DbSet<CarritoItem> CarritoItems { get; set; } // Define CarritoItems
        public DbSet<Compra> Compras { get; set; }          // Define Compras

        // Configuración del modelo
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configurar la relación entre Compra y ItemCompra (1:n)
    modelBuilder.Entity<ItemCompra>()
        .HasOne(ic => ic.Compra)              // Cada ItemCompra está relacionado con una Compra
        .WithMany(c => c.Items)               // Una Compra tiene muchos ItemCompra
        .HasForeignKey(ic => ic.CompraId);    // La clave foránea en ItemCompra que se refiere a Compra

    // Configuración de la relación entre Producto e ItemCompra
    modelBuilder.Entity<ItemCompra>()
        .HasOne(ic => ic.Producto)            // Cada ItemCompra está relacionado con un Producto
        .WithMany()                           // No necesitas especificar el lado "con muchos" aquí
        .HasForeignKey(ic => ic.ProductoId);  // La clave foránea en ItemCompra que se refiere a Producto

    // Configuración de la clave primaria compuesta en CarritoItem
    modelBuilder.Entity<CarritoItem>()
        .HasKey(ci => new { ci.CarritoId, ci.ProductoId });  // Clave primaria compuesta

    // Configurar las relaciones para CarritoItem
    modelBuilder.Entity<CarritoItem>()
        .HasOne(ci => ci.Carrito)
        .WithMany(c => c.CarritoItems)
        .HasForeignKey(ci => ci.CarritoId);

    // Cargar datos iniciales para los productos
    modelBuilder.Entity<Producto>().HasData(
        new Producto { Id = 1, Nombre = "Smartphone Galaxy Z", Descripcion = "Smartphone con pantalla plegable de 7.3\"", Precio = 1200.00m, Stock = 50, ImagenUrl = "https://example.com/smartphone.jpg" },
        new Producto { Id = 2, Nombre = "Laptop Dell XPS 13", Descripcion = "Laptop ultra delgada con procesador Intel i7", Precio = 999.99m, Stock = 30, ImagenUrl = "https://example.com/laptop-dell-xps.jpg" },
        new Producto { Id = 3, Nombre = "Auriculares Sony WH-1000XM4", Descripcion = "Auriculares inalámbricos con cancelación de ruido", Precio = 350.00m, Stock = 100, ImagenUrl = "https://example.com/sony-headphones.jpg" },
        new Producto { Id = 4, Nombre = "Reloj Inteligente Apple Watch Series 7", Descripcion = "Reloj inteligente con monitor de salud y pantalla siempre encendida", Precio = 399.99m, Stock = 80, ImagenUrl = "https://example.com/apple-watch.jpg" },
        new Producto { Id = 5, Nombre = "Cámara Canon EOS 80D", Descripcion = "Cámara DSLR de 24.2 megapíxeles para fotografía avanzada", Precio = 850.00m, Stock = 40, ImagenUrl = "https://example.com/canon-eos.jpg" },
        new Producto { Id = 6, Nombre = "Teclado Mecánico Razer Huntsman", Descripcion = "Teclado mecánico con switches ópticos para gamers", Precio = 129.99m, Stock = 60, ImagenUrl = "https://example.com/razer-keyboard.jpg" },
        new Producto { Id = 7, Nombre = "Monitor UltraWide LG 34WN80C-B", Descripcion = "Monitor curvo de 34 pulgadas con resolución 2560x1080", Precio = 400.00m, Stock = 25, ImagenUrl = "https://example.com/lg-monitor.jpg" },
        new Producto { Id = 8, Nombre = "Consola PlayStation 5", Descripcion = "Consola de videojuegos con soporte para 4K y ray tracing", Precio = 499.99m, Stock = 15, ImagenUrl = "https://example.com/playstation5.jpg" },
        new Producto { Id = 9, Nombre = "Nintendo Switch OLED", Descripcion = "Consola portátil con pantalla OLED y soporte para juegos en 4K", Precio = 349.00m, Stock = 50, ImagenUrl = "https://example.com/nintendo-switch.jpg" },
        new Producto { Id = 10, Nombre = "Silla Gamer DXRacer Formula Series", Descripcion = "Silla ergonómica con soporte lumbar y reposabrazos ajustables", Precio = 299.00m, Stock = 10, ImagenUrl = "https://example.com/dxracer-chair.jpg" }
    );
}

    }
}