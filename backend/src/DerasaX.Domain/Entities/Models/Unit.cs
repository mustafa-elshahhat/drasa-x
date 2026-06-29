using DerasaX.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class Unit :BaseEntity<string>
    {
        public string Title { get; set; } = null!;

        [ForeignKey("Subject")]
        public string SubjectId { get; set; } = null!;
        public Subject Subject { get; set; } = null!;

        public ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    }
}
