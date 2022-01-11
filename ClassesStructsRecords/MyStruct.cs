using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassesStructsRecords;

[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct MyStruct
{
    public string Value { get; init; }

    public MyStruct(string value)
    {
        Value = value;
    }
}

public record struct MyRecordStruct
{
    public string Value { get; init; }

    public MyRecordStruct(string value)
    {
        Value = value;
    }
}

public struct MyRecordStruct2 : IEquatable<MyRecordStruct2>
{
    private string Valuek__BackingField;

    public string Value
    {
        get
        {
            return Valuek__BackingField;
        }
        init
        {
            Valuek__BackingField = value;
        }
    }

    public MyRecordStruct2(string value)
    {
        Valuek__BackingField = value;
    }
    
    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("MyRecordStruct");
        stringBuilder.Append(" { ");
        if (PrintMembers(stringBuilder))
        {
            stringBuilder.Append(' ');
        }
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
    
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Value = ");
        builder.Append((object)Value);
        return true;
    }

    public static bool operator !=(MyRecordStruct2 left, MyRecordStruct2 right)
    {
        return !(left == right);
    }

    public static bool operator ==(MyRecordStruct2 left, MyRecordStruct2 right)
    {
        return left.Equals(right);
    }
    
    public override int GetHashCode()
    {
        return Valuek__BackingField.GetHashCode();
    }
    
    public override bool Equals(object obj)
    {
        return obj is MyRecordStruct2 other && Equals(other);
    }

    public bool Equals(MyRecordStruct2 other)
    {
        return EqualityComparer<string>.Default.Equals(Valuek__BackingField, other.Valuek__BackingField);
    }
}