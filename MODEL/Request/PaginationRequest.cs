using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class PaginationRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? FilterText { get; set; }
        public string? FilterType { get; set; }
        public bool FilterFlag { get; set; } = false;
    }
}
