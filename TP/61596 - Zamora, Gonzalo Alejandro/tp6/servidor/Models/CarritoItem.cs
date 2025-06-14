using tp6.Models;               // Para acceder a las clases dentro de tp6.Models
using Microsoft.EntityFrameworkCore;  // Para trabajar con Entity Framework Core (EF Core)
using System;                   // Para tipos básicos como Guid, DateTime
using System.Collections.Generic;  // Para trabajar con colecciones como List
namespace tp6.Models
{
    
   
       public class CarritoItem
       {
           // Claves primarias compuestas: CarritoId + ProductoId
           public Guid CarritoId { get; set; }
           public Carrito Carrito { get; set; }

           public int ProductoId { get; set; }
           public Producto Producto { get; set; }

           public int Cantidad { get; set; }

           // La clave primaria compuesta
           public override bool Equals(object obj)
           {
               return obj is CarritoItem item &&
                      CarritoId.Equals(item.CarritoId) &&
                      ProductoId == item.ProductoId;
           }

           public override int GetHashCode()
           {
               return HashCode.Combine(CarritoId, ProductoId);
           }
       
   }
   
   }
   