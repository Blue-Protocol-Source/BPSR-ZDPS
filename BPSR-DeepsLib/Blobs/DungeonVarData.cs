using System.Text;
using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonVarData : BlobType
{
    public string Name = "";
    public int Value;


    public DungeonVarData()
    {
    }

    public DungeonVarData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.DungeonVarData.NameFieldNumber:
                Name = blob.ReadString();

                //System.Diagnostics.Debug.WriteLine($"DungeonVarData.Name={name}");
                return true;
            case Zproto.DungeonVarData.ValueFieldNumber:
                //int value = blob.ReadInt();
                //kvp[lastName] = value;

                Value = blob.ReadInt();

                //System.Diagnostics.Debug.WriteLine($"DungeonVarData.Value={value}");
                return true;
            default:
                return false;
        }
    }
}