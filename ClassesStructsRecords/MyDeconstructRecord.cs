using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ClassesStructsRecords;

//equal to this public record MyDeconstructRecord(string Value)
public record MyDeconstructRecord2(string Value, string Name);
public class MyDeconstructRecord : IEquatable<MyDeconstructRecord>
{
    [CompilerGenerated]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string Valuek__BackingField;

    [CompilerGenerated]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string Namek__BackingField;
    
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return typeof(MyDeconstructRecord);
        }
    }

    public string Value
    {
        [CompilerGenerated]
        get
        {
            return Valuek__BackingField;
        }
        [CompilerGenerated]
        init
        {
            Valuek__BackingField = value;
        }
    }

    public string Name
    {
        [CompilerGenerated]
        get
        {
            return Namek__BackingField;
        }
        [CompilerGenerated]
        init
        {
            Namek__BackingField = value;
        }
    }

    public MyDeconstructRecord(string Value, string Name)
    {
        Valuek__BackingField = Value;
        Namek__BackingField = Name;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("MyDeconstructRecord");
        stringBuilder.Append(" { ");
        if (PrintMembers(stringBuilder))
        {
            stringBuilder.Append(' ');
        }
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("Value = ");
        builder.Append((object)Value);
        builder.Append(", Name = ");
        builder.Append((object)Name);
        return true;
    }
    public static bool operator !=(MyDeconstructRecord left, MyDeconstructRecord right)
    {
        return !(left == right);
    }

    public static bool operator ==(MyDeconstructRecord left, MyDeconstructRecord right)
    {
        return (object)left == right || ((object)left != null && left.Equals(right));
    }

    public override int GetHashCode()
    {
        return (EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Valuek__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namek__BackingField);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as MyDeconstructRecord);
    }
    public virtual bool Equals(MyDeconstructRecord other)
    {
        return (object)this == other || ((object)other != null && EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(Valuek__BackingField, other.Valuek__BackingField) && EqualityComparer<string>.Default.Equals(Namek__BackingField, other.Namek__BackingField));
    }
    public virtual MyDeconstructRecord Clone()
    {
        return new MyDeconstructRecord(this);
    }

    protected MyDeconstructRecord(MyDeconstructRecord original)
    {
        Valuek__BackingField = original.Valuek__BackingField;
        Namek__BackingField = original.Namek__BackingField;
    }

    public void Deconstruct(out string Value, out string Name)
    {
        Value = this.Value;
        Name = this.Name;
    }
}
