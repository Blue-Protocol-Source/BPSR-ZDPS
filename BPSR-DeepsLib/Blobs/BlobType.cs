using System.Diagnostics;

namespace BPSR_DeepsLib.Blobs;

public class BlobType
{
    public virtual string DebugName => null;

    public BlobType()
    {
        
    }
    
    public BlobType(BlobReader blob)
    {
        Read(ref blob);
    }

    public void Read(ref BlobReader blob)
    {
        var tag = blob.ReadInt();
        if (tag != -2)
            return;
        
        var size = blob.ReadInt();
        if (size == -3)
            return;
        if (size < 0)
            return;
        var offset = blob.Offset;
        var index = blob.ReadInt();
        while (0 < index) {
            Debug.WriteLine($"Parsing field {index} at {blob.Offset}");
            if (!ParseField(index, ref blob)) {
                blob.Offset = offset + size;
            }
            
            index = blob.ReadInt();
        }

        if (index != -3) {
            Debug.WriteLine($"Invalid end tag {index} at {blob.Offset}");
        }
    }

    public virtual bool ParseField(int index, ref BlobReader blob)
    {
        return false;
    }
    
    /*private string GetFieldPath(int index)
    {
        var lastParent = Parent;
        List<string> parts = [];
        do {
            parts.Add(lastParent?.DebugName ?? $"{index}");
            lastParent = lastParent.Parent;
        } while (lastParent != null);

        parts.Reverse();
        var path = string.Join(".", parts);
        return path;
    }*/
}