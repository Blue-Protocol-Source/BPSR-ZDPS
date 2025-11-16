using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class SocialData : BlobType
{
    public long? CharId;
    public string? AccountId;

    public SocialData()
    {
    }

    public SocialData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.SocialData.CharIdFieldNumber:
                CharId = blob.ReadLong();
                return true;
            case Zproto.SocialData.AccountIdFieldNumber:
                AccountId = blob.ReadString();
                return true;
            default:
                return false;
        }
    }
}
