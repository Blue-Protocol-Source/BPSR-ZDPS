using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class DungeonVarAll : BlobType
{
    public Dictionary<long, DungeonVar>? DungeonVarAllMap;

    public DungeonVarAll()
    {
    }

    public DungeonVarAll(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonVarAll.DungeonVarAllMapFieldNumber:
                DungeonVarAllMap = blob.ReadHashMap<long, DungeonVar>();
                return true;
            default:
                return false;
        }
    }

    public static implicit operator Zproto.DungeonVarAll(DungeonVarAll dungeonVarAll)
    {
        var varAll = new Zproto.DungeonVarAll();
        foreach (var item in dungeonVarAll.DungeonVarAllMap)
        {
            varAll.DungeonVarAllMap.Add(item.Key, item.Value);
        }
        return varAll;
    }
}
