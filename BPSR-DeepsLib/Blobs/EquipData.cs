using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class EquipData : BlobType
{
    public List<EquipNine>? EquipInfos;

    public EquipData()
    {
    }

    public EquipData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.EquipData.EquipInfosFieldNumber:
                EquipInfos = blob.ReadList<EquipNine>();
                return true;
            default:
                return false;
        }
    }
}
