// See https://aka.ms/new-console-template for more information

using System.Collections;

var array = new int[] {1, 3};
var found = ~Array.BinarySearch(array, 0, 2, 0);
var list = new SortedList<int, int>();
list.Add(1, 1);
list.Add(3, 2);
list.Add(2, 2);
Console.WriteLine("Hello, World!");