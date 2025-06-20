

using System.Net.Http.Json;
using Microsoft.JSInterop;
using tp6.Models; // Asegurate de tener este using para Carrito y CarritoItem
using Client.Services;
using System.Text.Json;

namespace Client.Services
{
    public class ApiService 
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private Guid carritoId;

        public ApiService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }
public async Task<List<Producto>> ObtenerProductosAsync(string? busqueda = null)
        {
            try
            {
                string url = "https://localhost:5177"; 

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    url += $"?busqueda={Uri.EscapeDataString(busqueda)}";
                }

                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var productosJson = await response.Content.ReadAsStringAsync();
                var productos = JsonSerializer.Deserialize<List<Producto>>(productosJson);

                return productos ?? new List<Producto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener productos: {ex.Message}");
                return new List<Producto>();
            }
        }
        // Obtener o crear carrito y guardarlo en localStorage
        public async Task<Guid> ObtenerOCrearCarritoAsync()
        {
            if (carritoId != Guid.Empty)
                return carritoId;

            var response = await _http.PostAsync($"/carrito", null);
            response.EnsureSuccessStatusCode();
            carritoId = await response.Content.ReadFromJsonAsync<Guid>();
            return carritoId;
        }

        // Obtener carrito completo (con sus CarritoItems)
        public async Task<Carrito> ObtenerCarritoCompletoAsync()
        {
            var id = await ObtenerOCrearCarritoAsync();
            return await _http.GetFromJsonAsync<Carrito>($"/carrito/{id}");
        }

        // Obtener cantidad total de items del carrito
        public async Task<int> ObtenerCantidadCarritoAsync()
        {
            await ObtenerOCrearCarritoAsync();
            return await _http.GetFromJsonAsync<int>($"/carrito/{carritoId}/cantidad");
        }

        // Agregar producto al carrito
        public async Task<bool> AgregarAlCarritoAsync(Guid carritoId, int ProductoId, int cantidad)
        {
            var response = await _http.PutAsync($"/carrito/{carritoId}/{ProductoId}?cantidad={cantidad}", null);
            return response.IsSuccessStatusCode;
        }

        // Obtener ítems del carrito
        public async Task<List<CarritoItem>> ObtenerCarritoItemsAsync()
        {
            await ObtenerOCrearCarritoAsync();
            var carrito = await _http.GetFromJsonAsync<Carrito>($"/carrito/{carritoId}");
            return carrito?.CarritoItems ?? new List<CarritoItem>();
        }

        // Vaciar el carrito
        public async Task VaciarCarritoAsync()
        {
            await ObtenerOCrearCarritoAsync();
            await _http.DeleteAsync($"/carrito/{carritoId}");
        }

        // Confirmar la compra
        public async Task<bool> ConfirmarCompraAsync()
        {
            await ObtenerOCrearCarritoAsync();
            var response = await _http.PutAsync($"/carrito/{carritoId}/confirmar", null);
            return response.IsSuccessStatusCode;
        }

        // Obtener todos los productos del catálogo
        public async Task<List<Producto>> GetProductosAsync()
        {
            return await _http.GetFromJsonAsync<List<Producto>>("/producto") ?? new List<Producto>();
        }
        
   }}