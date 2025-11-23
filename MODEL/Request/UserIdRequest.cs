using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class UserIdRequest: PaginationRequest
    {
        public string user_id { get; set; } // Using string for UUID
    }
}
