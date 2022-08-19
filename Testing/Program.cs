// See https://aka.ms/new-console-template for more information
using Jupiter.VMU;
using Testing;

void TestAndForget(SourceClass s)
{
    ListenerClass? listener = new(s);
    s.ValueToSet = true;
    s.ValueToSet = false;
    listener = null;
}

Console.WriteLine("Hello, World!");

SourceClass s = new();
s.SubscribeUnsafe(p => p.ValueToSet, v => Console.WriteLine($"Value is now {v}"));

// We've to use a method to test weak references as he seems to still have a reference on the stack
TestAndForget(s);

GC.Collect();

s.ValueToSet = true;
s.ValueToSet = false;

Console.ReadKey();

