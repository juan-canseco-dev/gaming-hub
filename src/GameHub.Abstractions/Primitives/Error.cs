namespace GameHub.Abstractions.Primitives;

public record Error(string Code, string Description)
{
    public static Error None = new(string.Empty, string.Empty);
    public static Error NullValue = new("Error.NullValue", "A null value was entered.");
    public static Error InternalServerError = new(
     "Error.InternalServerError",
     "An unexpected server error occurred."
    );
}