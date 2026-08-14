namespace EventManagement.Api.Services;

public static class EventCategories
{
    public static readonly string[] All =
    [
        "Art & Exhibition", "Awards Event", "Comedy Shows", "Concerts & Music",
        "Conferences", "Cultural Events", "Education & Learning", "Fashion & Beauty",
        "Festivals", "Food & Drink", "Gaming & Esports", "Hackathons",
        "Health & Wellness", "Movies & Film", "Other", "Pageant",
        "Parties & Nightlife", "Startup & Tech", "Workshops & Training"
    ];

    public static string? Normalize(string value) => All.FirstOrDefault(category =>
        string.Equals(category, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
