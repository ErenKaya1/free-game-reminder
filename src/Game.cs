using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src
{
    [Table("Game")]
    public class Game
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string GameName { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }
    }
}