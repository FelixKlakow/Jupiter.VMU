# Jupiter.VMU

Provides methods to subscribe to specific `INotifyPropertyChanged` values.
The helper uses weak references to avoid memory leaks but requires `PropertyChanged` to implement a weak event by itself if required.

## Sample

```C#
public void Sample(SampleClass sample)
{
    sample.Subscribe(p => p.SampleIntProperty, HandleSampleIntChanged)
          .SubscribeUnsafe(p => p.SampleBoolProperty, v => Console.WriteLine($"Sample bool: {v}"));
}

private void HandleSampleIntChanged(int v)
{
    Console.WriteLine($"Sample int: {v}");
}
```


Author:
Felix Klakow
