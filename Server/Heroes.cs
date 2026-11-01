using Core.Objects.Frogs;

namespace Server;

public interface IHeroCatalog
{
    HeroDto[] GetAll();
}

public class HeroCatalog : IHeroCatalog
{
    public HeroDto[] GetAll()
    {
        var commonFrogInfo = FrogPresets.Common;
        var heavyFrogInfo = FrogPresets.Heavy;
        var stickyFrogInfo = FrogPresets.Sticky;
        List<FrogConfig> frogs = [commonFrogInfo, heavyFrogInfo, stickyFrogInfo];
        return frogs
            .Select(f => 
                new HeroDto(f.Name, f.Name, f.MaxJumpDistance, f.TongueLength, f.Radius, f.Mass, f.SwimSpeed))
            .ToArray();
    }
}

public record HeroDto(
    string Id,
    string Name,
    float JumpDistance,
    float TongueLength,
    float Size,
    float Mass,
    float Speed
    // string Ability
);