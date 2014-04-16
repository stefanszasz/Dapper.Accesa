using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using FizzWare.NBuilder;
using Xunit;
using Assert = Xunit.Assert;

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
                var users = Builder<User>.CreateListOfSize(100).All().Do(user => user.Id = 0).Build();
                sqlConnection.Execute("insert into [users] (UserName, FirstName, LastName) values (@userName, @firstName, @lastName)", users);

                List<User> fetchUsers = sqlConnection.Query<User>("select * from [users]").ToList();

                Assert.NotEmpty(fetchUsers);

                sqlConnection.Execute("delete [users]");
            }
        }

        [Fact]
        public void Multiple_query()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var tx = sqlConnection.BeginTransaction())
                {
                    var customerId = InsertCustomerWithProjectData(sqlConnection, tx);

                    const string selectMultiple = "select * from customers where Id = @customerId;" +
                                                   "select * from projects where CustomerId = @customerId;";

                    using (var queryMultiple = sqlConnection.QueryMultiple(selectMultiple, new { customerId }, tx))
                    {
                        Customer foundCustomer = queryMultiple.Read<Customer>().First();
                        var projects = queryMultiple.Read<Project>().ToList();

                        Assert.True(foundCustomer.Name == "Apple");
                        Assert.True(projects.First().Name == ProjectName);
                    }

                    sqlConnection.Execute("delete [projects]", transaction: tx);
                    sqlConnection.Execute("delete [customers]", transaction: tx);
                }
            }
        }

        [Fact]
        public void Multiple_mapping()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var tx = sqlConnection.BeginTransaction())
                {
                    var customerId = InsertCustomerWithProjectData(sqlConnection, tx);

                    const string sql = "select * from Customers c join Projects p on c.Id = p.CustomerId where c.Id = @customerId";
                    var projects = sqlConnection.Query<Customer, Project, Project>(sql, (cust, prj) =>
                    {
                        prj.Customer = cust;
                        return prj;
                    }, new { customerId }, tx);

                    Project first = projects.First();

                    Assert.True(first.Name == ProjectName);
                    Assert.True(first.Customer.Name == CustomerName);

                    sqlConnection.Execute("delete [projects]", transaction: tx);
                    sqlConnection.Execute("delete [customers]", transaction: tx);
                }
            }
        }

        private static int InsertCustomerWithProjectData(SqlConnection sqlConnection, SqlTransaction tx)
        {
            var customer = new Customer { Name = CustomerName };
            int customerId = sqlConnection.Query<int>("insert into [customers] (Name) values (@name); SELECT CAST(SCOPE_IDENTITY() as int)", customer, tx).Last();

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
                                  "SELECT CAST(SCOPE_IDENTITY() as int)", project, tx);

            tx.Commit();
            return customerId;
        }
    }

    public enum ProjectType
    {
        Internal = 1,
        External = 2
    }
}