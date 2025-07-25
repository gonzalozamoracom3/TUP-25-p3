namespace cliente.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; } = 0;
        public int StockDisponible { get; set; }
        public int Cantidad { get; set; } = 0; 
        public string ImagenUrl { get; set; } = string.Empty;

        public ICollection<ItemCompra>? ItemsCompra { get; set; }
    }
}