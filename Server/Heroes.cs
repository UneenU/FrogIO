namespace Server;

public interface IHeroCatalog
{
    HeroDto[] GetAll();
}

public class HeroCatalog : IHeroCatalog
{
    public HeroDto[] GetAll()
    {
        return
        [
            new HeroDto("wren", "wren", "Scout", 100, 100, 10, "NONE"),
            new HeroDto("vex", "vex", "____", 100, 100, 10, "NONE"),
            new HeroDto("rook", "rook", "____", 100, 100, 10, "NONE")
        ];
    }
}

public record HeroDto(
    string Id,
    string Name,
    string Role,
    int Hp,
    int Attack,
    int Speed,
    string Ability
);