using FraudDetectionWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<FraudWorker>();

var host = builder.Build();

await host.RunAsync(); // worker will run until the application is stopped
                       //current estimation is that the worker will run for 
                       //every 24 hours, but this is subject to change based on 
                       //the workload and system performance.
