using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Application.Dto.UnitDto
{
    public class UpdateUnitDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string SubjectId { get; set; } = null!;
    }
}
