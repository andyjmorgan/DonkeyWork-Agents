# Orleans.Persistence.SeaweedFs

Custom Orleans grain storage provider backed by SeaweedFS Filer HTTP API.

## Architecture

- `IGrainStorage` implementation that persists grain state as JSON files via SeaweedFS Filer
- Uses HTTP GET/PUT/DELETE for read/write/clear operations
- ETag-based optimistic concurrency from SeaweedFS response headers
- State path format: `{BasePath}/{grainType}/{grainKey}/{stateName}.json`

## Dependencies

- `Microsoft.Orleans.Runtime` — for `IGrainStorage`, `GrainId`, `IGrainState<T>`
- `Microsoft.Extensions.Http` — for `IHttpClientFactory`

## Configuration

```csharp
siloBuilder.AddSeaweedFsGrainStorage("SeaweedFs", options =>
{
    options.BaseUrl = "http://localhost:8888";
    options.BasePath = "/orleans/grain-state";
});
```
