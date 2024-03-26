# Shashlik.Eventbus.Dashboard

Usage

```c#
    builder.Services.AddEventBus()
    .AddMySql<DataContext>()
    .AddMemoryQueue()
    .AddDashboard();

    
    // after var app = builder.Build(); app.UseRouting();
    // before app.MapControllers(); or app.UseEndpoints();
    app.UseEventBusDashboard();
```
注意：由于`app.UseEventBusDashboard();`中使用了`app.UseEndpoints();`，所以`app.UseEventBusDashboard();`应该尽量写在`app.MapControllers();`或者`app.UseEndpoints();`前面。