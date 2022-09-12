# Shashlik.Eventbus.Dashboard

Usage

```c#
    builder.Services.AddEventBus()
    .AddMySql<DataContext>()
    .AddMemoryQueue()
    .AddShashlikDashboard();

    
    // after var app = builder.Build(); app.UseRouting();
    app.UseEventBusDashboard();
```
