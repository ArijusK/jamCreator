using System.ComponentModel.DataAnnotations;

namespace JamCreator.Shared.Models;

public class LeaveJamModel
{
    public string SessionId { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
