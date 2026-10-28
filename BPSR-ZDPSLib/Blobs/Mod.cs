using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class Mod : BlobType
{
    public Dictionary<int, long>? ModSlots;
    public Dictionary<long, ModInfo>? ModInfos;

    public Mod()
    {
    }

    public Mod(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.Mod.ModSlotsFieldNumber:
                ModSlots = blob.ReadHashMap<int, long>();
                return true;
            case Zproto.Mod.ModInfosFieldNumber:
                ModInfos = blob.ReadHashMap<long, ModInfo>();
                return true;
            default:
                return false;
        }
    }
}
