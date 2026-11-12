using System.Runtime.CompilerServices;
using Core.Events;
using Core.Match;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class LevelingSystem(Random? random = null) : IEventHandler<FrogEatFlyEvent>
{
    private readonly LevelListGenerator _levelListGenerator = new(random);
    private readonly Dictionary<Frog, Queue<LevelUpgrade[]>> _levelLists = new();

    void IEventHandler<FrogEatFlyEvent>.Handle(FrogEatFlyEvent e, SimulationContext simulationContext)
    {
        var fly = e.Fly;
        var frog = e.Frog;

        frog.AddXp(fly.XpForEating);
        var newLevel = GetLevel(frog.Xp);
        Console.WriteLine($"frog {e.Frog.Name}  has level {newLevel}");
        for (int i = 0; i < newLevel - frog.Level; i++)
        {
            var list = _levelListGenerator.GenerateList(3);
            _levelLists.TryAdd(e.Frog, new Queue<LevelUpgrade[]>());
            _levelLists[e.Frog].Enqueue(list);

            var player = simulationContext.Players.First(p => p.Controllable == e.Frog);
            simulationContext.ClientEvents.Add(
                new PlayerGetNewLevelEvent(player.PlayerId, list.Select(u => u.ToLevelListPosition()).ToArray()));
        }

        frog.Level = newLevel;
    }

    public void Handle(ContextCommand[] commands, SimulationContext ctx)
    {
        foreach (var command in commands)
        {
            var controllable = ctx.Players.Single(p => p.PlayerId == command.PlayerId).Controllable;
            if (controllable is not Frog frog) continue;

            var levelList = _levelLists[frog].Dequeue();
            var upgradeNumber = Math.Min(command.ChoiceNumber, levelList.Length - 1);
            var chosenPosition = levelList[upgradeNumber];
            chosenPosition.ApplyToFrog(frog);
        }
    }

    private int GetLevel(int xp)
    {
        var level = 0;
        do
        {
            level++;
        } while (xp > LevelsConfig.LevelPoints[level]);

        return level;
    }
}

public static class LevelsConfig
{
    public static readonly Dictionary<int, int> LevelPoints = new()
    {
        [1] = 0,
        [2] = 500,
        [3] = 1000,
        [4] = 2000,
        [5] = 5000,
        [6] = 10000,
        [7] = 20000,
        [8] = 30000,
        [9] = 40000,
        [10] = 50000,
        [11] = int.MaxValue
    };
}

public record LevelListPosition(string Name, float Value);

public class LevelUpgrade(string name, float value, Action<Frog, float> apply)
{
    public float Value { get; } = value;

    public void ApplyToFrog(Frog frog)
    {
        apply(frog, Value);
    }

    public LevelListPosition ToLevelListPosition()
    {
        return new(name, Value);
    }
}

public class LevelListGenerator(Random? random = null)
{
    private readonly LevelUpgrade[] _levelListPosition =
    [
        new("Mass", 0.5f, (frog, value) => frog.Mass += value),
        new("Radius", 0.5f, (frog, value) => frog.Radius += value),
        new("JumpSpeed", 0.1f, (frog, value) => frog.JumpSpeed += value),
        new("MaxJumpDistance", 5, (frog, value) => frog.MaxJumpDistance += value),
        new("TongueLength", 2, (frog, value) => frog.Tongue.MaxLength += value),
        new("TongueWidth", 0.5f, (frog, value) => frog.Tongue.Width += value),
        new("TongueSpeed", 0.15f, (frog, value) => frog.Tongue.Speed += value),
        new("JumpCooldownTicks", 5, (frog, value) => frog.JumpCooldownTicks -= (int)value),
    ];

    private readonly Random _random = random ?? new Random();

    public LevelUpgrade GeneratePosition()
    {
        var upgrade = _levelListPosition[_random.Next(0, _levelListPosition.Length)];
        return upgrade;
    }

    public LevelUpgrade[] GenerateList(int count = 3)
    {
        var position = new LevelUpgrade[3];
        for (int i = 0; i < count; i++)
        {
            position[i] = (GeneratePosition());
        }

        return position;
    }
}
