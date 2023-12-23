# GpuLinq

## Install (UPM)

by Unity PackageManager

```
https://github.com/wallstudio/TechArticleSamples.git?path=GpuLinq/Packages/GpuLinq
```

## Usage

```csharp
var gpu = source
    .AsGpuEnumerable() // switch GPGPU
    .Where(x => (uint)x % 2 == 0) // unorderd
    .Select(x => x * 2)
    .ToArray();
```