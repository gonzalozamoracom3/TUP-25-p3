namespace servidor.Modelos
{
    public class ItemCarrito
    {
        public int Id { get; set; }
        public string CarritoId { get; set; }
        public Carrito Carrito { get; set; }
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
        public int Cantidad { get; set; }
    }
}