namespace HeaplitLauncher
{
    public class ChatTurn
    {
        public string Role { get; set; } = "user"; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }
}
