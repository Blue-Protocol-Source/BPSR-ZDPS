using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class DungeonAffixData : BlobType
{
    public List<uint> AffixData;

    public DungeonAffixData()
    {
    }

    public DungeonAffixData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonAffixData.AffixDataFieldNumber:
                AffixData = blob.ReadList<uint>();
                return true;
            default:
                return false;
        }
    }
}
