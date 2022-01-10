namespace ClassesStructsRecords;

public readonly struct ReadonlyStruct
{
    public int Value { get; init; }

    public ReadonlyStruct(int value)
    {
        Value = value;
    }
}

public struct PartialReadonlyStruct
{
    public int Value { get; private set; }

    public PartialReadonlyStruct(int value)
    {
        Value = value;
    }

    public readonly void ChangeValue()
    {
        
    }
}

public ref struct RefStruct
{
    public int Value { get; init; }

    public RefStruct(int value)
    {
        Value = value;
    }
}