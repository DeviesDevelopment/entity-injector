using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityInjector.Samples.PostgresTest.Models;

public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }  // Primary key
    [Column("name")] 
    public string Name { get; set; } = "";
    [Column("price")] 
    public decimal Price { get; set; }
}
