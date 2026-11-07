using System.Collections.Generic;

namespace SAtS_MVC_Client.Models
{
    public class CourseViewModel
    {
        public Course Course { get; set; }
        public List<CourseSession> Sessions { get; set; }
    }
}
