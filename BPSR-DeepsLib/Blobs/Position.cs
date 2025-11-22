using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs
{
    public class Position(BlobReader blob) : BlobType(ref blob)
    {
        public float? X;
        public float? Y;
        public float? Z;
        public float? Dir;

        public override bool ParseField(int index, ref BlobReader blob)
        {
            switch (index)
            {
                case Zproto.Position.XFieldNumber:
                    X = blob.ReadFloat();
                    return true;
                case Zproto.Position.YFieldNumber:
                    Y = blob.ReadFloat();
                    return true;
                case Zproto.Position.ZFieldNumber:
                    Z = blob.ReadFloat();
                    return true;
                case Zproto.Position.DirFieldNumber:
                    Dir = blob.ReadFloat();
                    return true;
                default:
                    return false;
            }
        }
    }
}
