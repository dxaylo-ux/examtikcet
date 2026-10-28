using Application.DTOs;

namespace Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(BookingMessage message);
}
