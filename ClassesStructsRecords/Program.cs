using ClassesStructsRecords;


var record = new MyDeconstructRecord2("5", "6");
var (value, name) = record;
Console.WriteLine(value + " " + name);

var record2 = new MyDeconstructRecord2("5", "6");
//true
Console.WriteLine(record == record2);

var myRecord3 = record2 with
{
    Value = "6"
};

var myStruct = new MyStruct("5");
var myStruct2 = new MyStruct("5");

//true no boxing
Console.WriteLine(myStruct.Equals(myStruct2));


var myRecordStruct = new MyRecordStruct("5");
var myRecordStruct2 = new MyRecordStruct("5");
var myRecordStruct3 = myRecordStruct with
{
    Value = "6"
};

//true
Console.WriteLine(myRecordStruct == myRecordStruct2);
//true no boxing
Console.WriteLine(myRecordStruct.Equals(myRecordStruct2));



