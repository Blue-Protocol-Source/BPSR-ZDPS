using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this Zproto.Vec3 vec3)
        {
            return new Vector3(vec3.X, vec3.Y, vec3.Z);
        }
    }
}
