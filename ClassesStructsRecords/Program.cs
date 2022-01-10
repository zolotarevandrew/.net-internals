using System.Reflection;
using ClassesStructsRecords;

int a = 5;
ReadonlyStruct readonlyStruct = new ReadonlyStruct(5);
Console.WriteLine(readonlyStruct.Value);

PartialReadonlyStruct partialReadonlyStruct = new PartialReadonlyStruct(5);
Console.WriteLine(partialReadonlyStruct.Value);

ReadonlyStruct readonlyStructCopy = readonlyStruct with { Value = 3 };
Console.WriteLine(readonlyStructCopy.Value);
Console.WriteLine(readonlyStructCopy.GetHashCode());
Console.WriteLine(readonlyStruct.GetHashCode());

RefStruct refStruct = new RefStruct(5);
ShowRefStruct(readonlyStructCopy);

void ShowRefStruct(ReadonlyStruct refStruct)
{
    Console.WriteLine(refStruct.Value);
}



