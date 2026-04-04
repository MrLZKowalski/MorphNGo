namespace MorphNGo.UnitTests;

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using MorphNGo.Mapping.Configuration;

/// <summary>
/// Compares convention-based mapping throughput to hand-written property copies for simple DTOs.
/// </summary>
public class MapperPerformanceTests
{
    private const int MappingCount = 10_000;

    /// <summary>
    /// After warmup, the mapper should stay within this factor of manual assignment time.
    /// Debug builds and CI hosts vary; the compiled fast path keeps this comfortably low on Release.
    /// </summary>
    private const double MaxMapperToManualRatio = 18.0;

    [Fact]
    [Trait("Category", "Performance")]
    public void SimpleConventionMapping_IsCloseToManualPropertyCopy()
    {
        var config = new MapperConfiguration(NullLogger.Instance, cfg =>
        {
            cfg.CreateMap<PerfSource, PerfDto>();
        });
        var mapper = config.CreateMapper();

        var sources = new PerfSource[MappingCount];
        for (var i = 0; i < MappingCount; i++)
        {
            sources[i] = new PerfSource
            {
                Id = i,
                Name = $"Name{i}",
                Code = $"C{i % 1000}"
            };
        }

        var manualResults = new PerfDto[MappingCount];
        var mapperResults = new PerfDto[MappingCount];

        const int warmup = 500;
        for (var w = 0; w < warmup; w++)
        {
            _ = MapOneManual(sources[w % MappingCount]);
            _ = mapper.Map<PerfDto>(sources[w % MappingCount]);
        }

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < MappingCount; i++)
        {
            manualResults[i] = MapOneManual(sources[i]);
        }

        sw.Stop();
        var manualTicks = sw.ElapsedTicks;

        sw.Restart();
        for (var i = 0; i < MappingCount; i++)
        {
            mapperResults[i] = mapper.Map<PerfDto>(sources[i]);
        }

        sw.Stop();
        var mapperTicks = sw.ElapsedTicks;

        for (var i = 0; i < MappingCount; i++)
        {
            Assert.Equal(manualResults[i].Id, mapperResults[i].Id);
            Assert.Equal(manualResults[i].Name, mapperResults[i].Name);
            Assert.Equal(manualResults[i].Code, mapperResults[i].Code);
        }

        var ratio = manualTicks == 0
            ? double.PositiveInfinity
            : (double)mapperTicks / manualTicks;

        Assert.True(
            ratio <= MaxMapperToManualRatio,
            $"Expected mapper within {MaxMapperToManualRatio}x of manual mapping; manual={manualTicks} ticks, mapper={mapperTicks} ticks, ratio={ratio:0.###}.");
    }

    private static PerfDto MapOneManual(PerfSource s) =>
        new()
        {
            Id = s.Id,
            Name = s.Name,
            Code = s.Code
        };

    private sealed class PerfSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
    }

    private sealed class PerfDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
    }
}
