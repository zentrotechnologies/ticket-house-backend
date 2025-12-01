using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class TestimonialRequest
    {
        public int TestimonialId { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Description { get; set; }
        public int Active { get; set; } = 1;
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public IFormFile ProfileImage { get; set; }
    }

    public class UpdateTestimonialStatusRequest
    {
        public int TestimonialId { get; set; }
        public int Status { get; set; }
        public string UpdatedBy { get; set; }
    }
}
