namespace Domain.Models;

public enum MediaType : byte
{
    Audio = 1,
    Video = 2,
    Both = 3
}

public enum GuestClipStatus : byte
{
    Pending = 0,
    Published = 1,
    NoClipAvailable = 2
}
