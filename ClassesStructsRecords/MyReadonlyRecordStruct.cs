namespace ClassesStructsRecords;

public readonly record struct MyReadonlyRecordStruct(string Value);

public struct MyRefStruct
{
    public int Value { get; init; }
}