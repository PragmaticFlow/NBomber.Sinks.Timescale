﻿using NBomber.CSharp;
using NBomber.Sinks.Timescale;

new TimescaleDBReportingExample().Run();

public class TimescaleDBReportingExample
{
    private readonly TimescaleDbSink _timescaleDbSink = new();
    
    public void Run()
    {
        var scenario = Scenario.Create("user_flow_scenario", async context =>
        {
            var step1 = await Step.Run("login", context, async () =>
            {
                await Task.Delay(500);
                return Response.Ok(sizeBytes: 10, statusCode: "200");
            });

            var step2 = await Step.Run("get_product", context, async () =>
            {
                await Task.Delay(1000);
                return Response.Ok(sizeBytes: 20, statusCode: "200", message: "huy");
            });

            var step3 = await Step.Run("buy_product", context, async () =>
            {
                await Task.Delay(2000);
                return Response.Ok(sizeBytes: 30, statusCode: "200", message: "my");
            });

            return Response.Ok(statusCode: "201", message: "hey");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .LoadInfraConfig("infra-config.json")
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .WithReportingSinks(_timescaleDbSink)
            .WithTestSuite("reporting")
            .WithTestName("timescale_db_demo")
            .Run();
    }
}