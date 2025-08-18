using RouderSky;

Console.WriteLine("Hello, World!");

// --- Some Warmup
DebugMgr.EnableLogging = false;
for (int n = 0; n < 10; n++) {
    TestFrifloECSPerformance.RunAllPerformanceTests();
}

// --- Some Warmup
DebugMgr.EnableLogging = true;
TestFrifloECSPerformance.RunAllPerformanceTests();

