using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(new ClassLibrary1.Class1().GetType().Name);
            Console.WriteLine(new ClassLibrary2.Class1().GetType().Name);
            Console.WriteLine(new ClassLibrary3.Class1().GetType().Name);
        }
    }
}