using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper.Accesa.Model;
using FizzWare.NBuilder;
using Xunit;
using Xunit.Should;

namespace Dapper.Accesa
{
    public class DapperTests
    {
        private const string CustomerName = "Apple";
        private const string ProjectName = "Simple project";
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["Jinx"].ConnectionString;

        [Fact]
        public void Multiple_insert_with_query()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                var users = Builder<User>.CreateListOfSize(1000).All().Do(user => user.Id = 0).Build();
                sqlConnection.Execute("insert into [users] (UserName, FirstName, LastName) values (@userName, @firstName, @lastName)", users);

                List<User> fetchUsers = sqlConnection.Query<User>("select * from [users]").ToList();

                fetchUsers.ShouldNotBeEmpty();

                Assert.NotEmpty(fetchUsers);

                CleanupDatabase(sqlConnection);
            }
        }

        [Fact]
        public void Multiple_query()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                var customerId = InsertCustomerWithProjectData(sqlConnection);

                const string selectMultiple = "select * from customers where Id = @customerId;" +
                                               "select * from projects where CustomerId = @customerId;";

                using (var queryMultiple = sqlConnection.QueryMultiple(selectMultiple, new { customerId }))
                {
                    Customer foundCustomer = queryMultiple.Read<Customer>().First();
                    var projects = queryMultiple.Read<Project>().ToList();

                    foundCustomer.Name.ShouldBe("Apple");
                    projects.First().Name.ShouldBe(ProjectName);
                }

                CleanupDatabase(sqlConnection);
            }
        }

        [Fact]
        public void Multiple_mapping()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();

                var customerId = InsertCustomerWithProjectData(sqlConnection);

                const string sql = "select * from Customers c join Projects p on c.Id = p.CustomerId where c.Id = @customerId";
                var projects = sqlConnection.Query<Customer, Project, Project>(sql, (cust, prj) =>
                {
                    prj.Customer = cust;
                    return prj;
                }, new { customerId });

                Project first = projects.First();
                first.Name.ShouldBe(ProjectName);

                CleanupDatabase(sqlConnection);
            }
        }


        [Fact]
        public void Query_with_non_default_constructor()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                var users = new List<UserNonDefaultConstructor>
                {
                    new UserNonDefaultConstructor("firstUser") { FirstName = "FirstName1", LastName = "LastName1" },
                    new UserNonDefaultConstructor("secondUser") {FirstName = "FirstName2", LastName = "LastName2" },
                };
                sqlConnection.Execute("insert into [users] (UserName, FirstName, LastName) values (@userName, @firstName, @lastName)", users);

                var fetchUsers = sqlConnection.Query("select * from [users]")
                                              .Select(row => new UserNonDefaultConstructor(row.UserName) { Id = row.Id, FirstName = row.FirstName, LastName = row.LastName })
                                              .ToList();
                fetchUsers.ShouldNotBeEmpty();
                CleanupDatabase(sqlConnection);
            }
        }
        private static int InsertCustomerWithProjectData(SqlConnection sqlConnection)
        {
            var customer = new Customer { Name = CustomerName };
            int customerId = sqlConnection.Query<int>("insert into [customers] (Name) values (@name); SELECT CAST(SCOPE_IDENTITY() as int)", customer).Last();

            var project = new Project
            {
                Name = ProjectName,
                Description = "This is quite simple",
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(10),
                Price = 100000,
                Type = ProjectType.External,
                CustomerId = customerId
            };
            sqlConnection.Execute("insert into [projects] (Name, Description, StartDate, EndDate, Price, Type, CustomerId) " +
                                  "values " +
                                  "(@name, @description, @startDate, @endDate, @price, @type, @customerId); " +
                                  "SELECT CAST(SCOPE_IDENTITY() as int)", project);

            return customerId;
        }

        static void CleanupDatabase(SqlConnection sqlConnection)
        {
            sqlConnection.Execute("delete [projects]");
            sqlConnection.Execute("delete [customers]");
            sqlConnection.Execute("delete [users]");
        }

    }
}