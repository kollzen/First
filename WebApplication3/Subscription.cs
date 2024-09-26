namespace WebApplication3
{
    public class Subscription
    {
        public int Id { get; set; }
        public string ApartmentUrl { get; set; }
        public string Email { get; set; }
        public List<string> CurrentPrice { get; set; }
    }
}
