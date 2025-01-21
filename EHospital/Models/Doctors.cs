namespace EHospital.Models
{
    public class Doctors
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int SpecializationID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateOnly Birthdate { get; set; }

    }
}
