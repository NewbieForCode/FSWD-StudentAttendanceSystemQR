namespace SAtS_MVC_Client.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentFName { get; set; }
        public string StudentLName { get; set; }
        public DateOnly StudentDob { get; set; }
        public string StudentStreetAddress { get; set; }
        public string StudentSuburb { get; set; }
        public string StudentCity { get; set; }
        public int StudentPostalCode { get; set; }
        public string StudentEmail { get; set; }
        public string? StudentPhone { get; set; }
    }
}
