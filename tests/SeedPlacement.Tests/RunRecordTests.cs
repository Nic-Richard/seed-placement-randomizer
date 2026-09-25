using SeedPlacement.Core;

namespace SeedPlacement.Tests;

public class RunRecordTests
{
    private static Rack FilledRack()
    {
        var rack = new Rack();
        var types = new[] { "Wheat", "Barnyard grass", null, "Radish" };
        for (uint i = 0; i < 4; i++)
        {
            rack.Shelve(SeedSampler.Generate(new LayoutCode(i * 977, new PlacementSettings(3 + (int)i, 15 + i))), types[i]);
        }
        var first = Enumerable.Range(0, Rack.SlotCount).First(s => rack.Slots[s] is not null);
        rack.Relabel(first, "Control, \"A\"");
        rack.Remove(Enumerable.Range(0, Rack.SlotCount).Last(s => rack.Slots[s] is not null));
        return rack;
    }

    [Fact]
    public void Round_trips_through_json_with_identical_layouts()
    {
        var rack = FilledRack();

        var restored = new Rack();
        RunRecord.FromJson(RunRecord.From(rack).ToJson())!.RestoreInto(restored);

        Assert.Equal(rack.NextNumber, restored.NextNumber);
        for (var slot = 0; slot < Rack.SlotCount; slot++)
        {
            var a = rack.Slots[slot];
            var b = restored.Slots[slot];
            Assert.Equal(a is null, b is null);
            if (a is null || b is null) continue;
            Assert.Equal(a.Number, b.Number);
            Assert.Equal(a.Label, b.Label);
            Assert.Equal(a.SeedType, b.SeedType);
            Assert.Equal(a.Layout.Seeds, b.Layout.Seeds);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"version\":99,\"nextNumber\":1,\"dishes\":[]}")]
    public void Unreadable_runs_are_ignored(string json) => Assert.Null(RunRecord.FromJson(json));

    [Fact]
    public void Restoring_rejects_a_slot_used_twice()
    {
        var code = new LayoutCode(1, PlacementSettings.Default).ToString();
        var record = new RunRecord(3, [new RunRecord.Dish(2, 1, code, null), new RunRecord.Dish(2, 2, code, null)]);

        Assert.Throws<ArgumentException>(() => record.RestoreInto(new Rack()));
    }

    [Fact]
    public void Csv_has_one_row_per_seed_and_escapes_labels()
    {
        var rack = FilledRack();
        var lines = RunCsv.Write(rack).TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        Assert.Equal(RunCsv.Header, lines[0]);
        Assert.Equal(rack.Slots.Sum(d => d?.Layout.Seeds.Count ?? 0), lines.Length - 1);
        Assert.Contains(lines, l => l.Contains("\"Control, \"\"A\"\"\""));
        Assert.Contains(lines, l => l.Contains(",Barnyard grass,"));
    }
}
