using System;


//Tested with IL code in https://sharplab.io

Point p = new Point(1, 1);

//boxing
Console.WriteLine(p);

//boxing
Console.WriteLine(p.GetType());

//string version
Console.WriteLine(p.ToString());

object o = p;
//boxing
Console.WriteLine(o);

((Point) o).Change(3, 3);
//writes 1 1 because o not changed, point is in stack frame not in heap
Console.WriteLine(o);

((IPoint) p).Change(4, 4);
//writes 1 1 because p is in stack frame, p boxed because of interface
Console.WriteLine(p);

((IPoint) o).Change(5, 5);
//writes 5 5 because o is in heap, o not boxed and data changed by ref in heap
Console.WriteLine(o);


interface IPoint
{
    void Change(int x, int y);
}
public struct Point : IPoint
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    
    public void Change(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"{X.ToString()} {Y.ToString()}";
    }
}