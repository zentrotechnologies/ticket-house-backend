using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class CommonResponseModel<T>
    {
        public string? Status { get; set; }
        public bool? Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public T? Data { get; set; }
    }

    public class CommonStatusResponse
    {
        public CommonStatusResponse()
        {
            Response = new CommonResponseModel<string>();
        }
        public CommonResponseModel<string> Response { get; set; }
    }
}
