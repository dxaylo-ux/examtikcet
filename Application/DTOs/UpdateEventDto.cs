namespace Application.DTOs;

public class UpdateEventDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartsAt { get; set; }
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
    public int CategoryId { get; set; }
}
