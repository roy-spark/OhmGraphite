﻿using System;
using NLog;
using OpenHardwareMonitor.Hardware;
using Topshelf;

namespace OhmGraphite
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<MetricTimer>(s =>
                {
                    // We need to know where the graphite server lives and how often
                    // to poll the hardware
                    var config = Logger.LogFunction("parse config", MetricConfig.ParseAppSettings);
                    double seconds = config.Interval.TotalSeconds;
                    Logger.Info($"Host: {config.Host} port: {config.Port} interval: {seconds} tags: {config.Tags}");

                    // We'll want to capture all available hardware metrics
                    // to send to graphite
                    var computer = new Computer
                    {
                        GPUEnabled = true,
                        MainboardEnabled = true,
                        CPUEnabled = true,
                        RAMEnabled = true,
                        FanControllerEnabled = true,
                        HDDEnabled = true
                    };

                    var collector = new SensorCollector(computer);
                    var writer = new GraphiteWriter(config.Host, config.Port, Environment.MachineName, config.Tags);

                    s.ConstructUsing(name =>
                        Logger.LogFunction("creating timer",
                            () => new MetricTimer(config.Interval, collector, writer)));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.UseNLog();
                x.RunAsLocalSystem();
                x.SetDescription(
                    "Extract hardware sensor data and exports it to a given host and port in a graphite compatible format");
                x.SetDisplayName("Ohm Graphite");
                x.SetServiceName("OhmGraphite");
                x.OnException(ex => Logger.Error(ex, "OhmGraphite TopShelf encountered an error"));
            });
        }
    }
}