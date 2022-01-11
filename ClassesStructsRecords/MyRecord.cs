using System.Runtime.CompilerServices;
using System.Text;

namespace ClassesStructsRecords;


//equal to this public record MyRecord
public class MyRecord : IEquatable<MyRecord>
{
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return typeof(MyRecord);
        }
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("MyRecord");
        stringBuilder.Append(" { ");
        if (PrintMembers(stringBuilder))
        {
            stringBuilder.Append(" ");
        }
        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return false;
    }
    
    public static bool operator !=(MyRecord left, MyRecord right)
    {
        return !(left == right);
    }
    
    public static bool operator ==(MyRecord left, MyRecord right)
    {
        return (object)left == right || ((object)left != null && left.Equals(right));
    }

    public override int GetHashCode()
    {
        return EqualityComparer<Type>.Default.GetHashCode(EqualityContract);
    }
    
    public override bool Equals(object obj)
    {
        return Equals(obj as MyRecord);
    }
    
    public virtual bool Equals(MyRecord other)
    {
        return (object)this == other || ((object)other != null && EqualityContract == other.EqualityContract);
    }

    public virtual MyRecord Clone()
    {
        return new MyRecord(this);
    }

    protected MyRecord(MyRecord original)
    {
    }

    public MyRecord()
    {
    }
}