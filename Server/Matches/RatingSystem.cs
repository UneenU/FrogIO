using Core.Match;

namespace Server.Matches;

public interface IRatingSystem
{
    public Task<Dictionary<int, int>> GetRatingChanges(MatchResult result, Dictionary<int, int> ratings);
}

public class RatingSystem : IRatingSystem
{
    private const int RaffledRating = 30;

    public async Task<Dictionary<int, int>> GetRatingChanges(MatchResult result, Dictionary<int, int> ratings)
    {
        var scores = result.Scores;
        var playerIds = scores.Keys.ToArray();
        
        // Средний игровой результат.
        double averageScore = scores.Values.Average();
        // Средний рейтинг.
        double averageRating = ratings.Values.Average();

        var newUsersRating = new Dictionary<int, int>();
        foreach (var playerId in playerIds)
        {
            var score = scores[playerId];
            double performance = (score - averageScore) / averageScore;

            var rating = ratings[playerId];
            double ratingScale = GetRatingScale((rating - averageRating) /  averageRating);
            
            double change = performance * ratingScale;

            newUsersRating[playerId] = rating + (int)(RaffledRating * change);
        }

        return newUsersRating;
    }

    private double GetRatingScale(double relativeDiff, float k = 100)
    {
        return 1 / (1 + k * relativeDiff*relativeDiff);
    }
}