 using NBomber.CSharp;
 using NBomber.Sinks.Timescale;

 new TimescaleDBReportingExample().Run();

 public class TimescaleDBReportingExample
 {
     private readonly TimescaleDbSink _timescaleDbSink = 
         new("Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
     
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
                     return Response.Ok(sizeBytes: 20, statusCode: "200");
                 });

                 var step3 = await Step.Run("buy_product", context, async () =>
                 {
                     await Task.Delay(2000);
                     return Response.Ok(sizeBytes: 30, statusCode: "200");
                 });

                 return Response.Ok(statusCode: "201");
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
             ).WithoutWarmUp();

         NBomberRunner
             .RegisterScenarios(scenario)
             //.LoadInfraConfig("infra-config.json")
             .WithReportingInterval(TimeSpan.FromSeconds(5))
             .WithReportingSinks(_timescaleDbSink)
             .WithTestSuite("reporting")
             .WithTestName("timescale_db_demo")
             .Run();
     }
 }