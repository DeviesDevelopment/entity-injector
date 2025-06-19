using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityInjector.Samples.PostgresTest.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("name")] 
    public string Name { get; set; } = default!;
    
    [Column("age")] 
    public int Age { get; set; }
}