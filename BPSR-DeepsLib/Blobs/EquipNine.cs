using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class EquipNine : BlobType
{
    public int? Slot;
    public int? EquipId;

    public EquipNine()
    {
    }

    public EquipNine(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.EquipNine.SlotFieldNumber:
                Slot = blob.ReadInt();
                return true;
            case Zproto.EquipNine.EquipIDFieldNumber:
                EquipId = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}
