using Microsoft.EntityFrameworkCore;
using servidor.Modelos;
public class TiendaContext : DbContext {
    public TiendaContext(DbContextOptions<TiendaContext> options) : base(options) { }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Compra> Compras { get; set; }
    public DbSet<Item> ItemsCompra { get; set; }
    public DbSet<Carrito> Carritos { get; set; }
    public DbSet<ItemCarrito> ItemCarritos { get; set; }
}