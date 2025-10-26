using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public class D3D11Exception : Exception
    {
        public D3D11Exception(ResultCode code) : base((Marshal.GetExceptionForHR((int)code) ?? new Exception("Unable to get exception from HRESULT")).Message)
        {
        }
    }
}
