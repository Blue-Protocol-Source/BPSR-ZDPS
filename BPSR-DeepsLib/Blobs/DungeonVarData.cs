using System.Text;
using Zproto;

namespace BPSR_DeepsLib.Blobs;

public class DungeonVarData : BlobType
{
    public Dictionary<string, int> kvp = new();

    private string lastName = "";

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
                int length = blob.ReadInt();
                string name = Encoding.UTF8.GetString(blob.ReadBytes(length));
                lastName = name;

                //System.Diagnostics.Debug.WriteLine($"DungeonVarData.Name={name}");
                return true;
            case Zproto.DungeonVarData.ValueFieldNumber:
                int value = blob.ReadInt();
                kvp[lastName] = value;

                //System.Diagnostics.Debug.WriteLine($"DungeonVarData.Value={value}");
                return true;
            default:
                return false;
        }
    }
}