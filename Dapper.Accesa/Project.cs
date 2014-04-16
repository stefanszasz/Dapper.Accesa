using System;

namespace Dapper.Accesa
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public ProjectType Type { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
    }
}