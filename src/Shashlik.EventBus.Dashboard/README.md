# Shashlik.Eventbus.Dashboard

Usage

```c#
    builder.Services.AddEventBus()
    .AddMySql<DataContext>()
    .AddMemoryQueue()
    .AddDashboard();

    
    // after var app = builder.Build(); app.UseRouting();
    app.UseEventBusDashboard();
```
