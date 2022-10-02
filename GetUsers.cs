using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using toKinitoC_Api.Models;

namespace a2L.FunctionToKinito
{
    public static class GetUsers
    {
        [FunctionName("GetUsers")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getusers/{id?}")] HttpRequest req,
            ILogger log, int? id)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get Users");
            log.LogInformation($"id = {id ?? -1}");
            string username = req.Query["username"];
            log.LogInformation($"username = {username ?? "null"}");

            List<User> users = new List<User>();


            string options = "";
            if (id != null)
            {
                options = $" where id = {id}";
            }
            if (!string.IsNullOrEmpty(username))
            {
                if (string.IsNullOrEmpty(options))
                {
                    options = $" where username = '{username}'";
                }
                else
                {
                    options += $" and username = '{username}'";
                }
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = $"Select * from users{options}";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        User user = new User();
                        user.id = (int)reader["id"];
                        user.name = reader["name"].ToString();
                        user.username = reader["username"].ToString();
                        user.password = reader["password"].ToString();
                        user.adminLevel = (int)reader["adminLevel"];
                        user.remarks = reader["remarks"].ToString();
                        log.LogInformation(reader["name"].ToString());
                        users.Add(user);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return new OkObjectResult(users);
         
        }


        [FunctionName("CreateUser")]
        public static async Task<IActionResult> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Create User");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<User>(requestBody);
            List<int> insertedUsers = new List<int>();

            try
            {

                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    if (!String.IsNullOrEmpty(input.name))
                    {
                        var query = $"IF NOT EXISTS (Select * from users where name = '{input.name}' or username = '{input.username}' )BEGIN INSERT INTO [users] (name,username,password,adminLevel,remarks) VALUES('{input.name}', '{input.username}', '{input.password}', '{input.adminLevel}', '{input.remarks}') END";
                        log.LogInformation($"Query = <{query}>");
                        SqlCommand command = new SqlCommand(query, connection);
                        int result = command.ExecuteNonQuery();
                        insertedUsers.Add(result);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }
            return new OkObjectResult(insertedUsers);
        }

        [FunctionName("SerialExist")]
        public static async Task<IActionResult> SerialExists(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "serialexist")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Serial Exist?");

            string serials = req.Query["serials"];
            log.LogInformation($"username = {serials ?? "null"}");

            List<serialExist> serials1 = new List<serialExist>();

            var query = $"select src.serial from (values {serials}) as src(serial) WHERE EXISTS (select 1 from transactions where transactions.serial = src.serial)";
            log.LogInformation($"query = '{query}'");
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        string tempSerial = reader["serial"].ToString();
                        string result = "";
                        var query1 = $"Select CONVERT(varchar,transactiondate,103) parastDate,ArParast from Transactions where invType = 3 and serial = '{tempSerial}'";
                        using (SqlConnection connection1 = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                        {
                            log.LogInformation(query1);
                            connection1.Open();
                            var command1 = new SqlCommand(query1, connection1);
                            var reader1 = await command1.ExecuteReaderAsync();
                            while (reader1.Read())
                            {
                                string date = reader1["parastDate"].ToString();
                                string arParast = reader1["ArParast"].ToString();
                                result += $" {date}, {arParast}";
                            }
                            connection1.Close();
                        }

                        serialExist serialExist = new serialExist();
                        serialExist.serial = tempSerial;
                        serialExist.Description = result.Trim();
                        serials1.Add(serialExist);
                    }
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }
            
            return new OkObjectResult(serials1);
        }

        [FunctionName("People")]
        public static async Task<IActionResult> People(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "people/{id?}")] HttpRequest req,
           ILogger log, int? id)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get People");
            log.LogInformation($"id = {id ?? -1}");
            string name = req.Query["name"];
            log.LogInformation($"Name = {name ?? "null"}");

            List<People> peoples = new List<People>();


            string options = "";
            if (id != null)
            {
                options = $" where id = {id}";
            }
            if (!string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrEmpty(options))
                {
                    options = $" where name = '{name}'";
                }
                else
                {
                    options += $" and name = '{name}'";
                }
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = $"Select * from people{options} order by name";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        People people = new People();
                        people.id = (int)reader["id"];
                        people.name = reader["name"].ToString();
                        people.remarks = reader["remarks"].ToString();
                       // log.LogInformation(reader["name"].ToString());
                        peoples.Add(people);
                    }

                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return new OkObjectResult(peoples);

        }

        [FunctionName("CreatePeople")]
        public static async Task<IActionResult> CreatePeople(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "people")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Create People");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<User>(requestBody);
            List<int> insertedUsers = new List<int>();

            try
            {

                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    if (!String.IsNullOrEmpty(input.name))
                    {
                        var query = $"IF NOT EXISTS (Select * from people where name = '{input.name}')BEGIN INSERT INTO [people] (name,remarks) VALUES('{input.name}', '{input.remarks}') END";
                        log.LogInformation($"Query = <{query}>");
                        SqlCommand command = new SqlCommand(query, connection);
                        int result = command.ExecuteNonQuery();
                        insertedUsers.Add(result);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }
            return new OkObjectResult(insertedUsers);
        }

        [FunctionName("Product")]
        public static async Task<IActionResult> Product(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = "product/{id?}")] HttpRequest req,
         ILogger log, int? id)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get Product");
            log.LogInformation($"id = {id ?? -1}");
            string name = req.Query["name"];
            log.LogInformation($"Name = {name ?? "null"}");

            List<People> products = new List<People>();


            string options = "";
            if (id != null)
            {
                options = $" where id = {id}";
            }
            if (!string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrEmpty(options))
                {
                    options = $" where name = '{name}'";
                }
                else
                {
                    options += $" and name = '{name}'";
                }
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = $"Select * from product{options} order by name";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        People product = new People();
                        product.id = (int)reader["id"];
                        product.name = reader["name"].ToString();
                        product.remarks = reader["remarks"].ToString();
                        // log.LogInformation(reader["name"].ToString());
                        products.Add(product);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return new OkObjectResult(products);

        }

        [FunctionName("CreateProduct")]
        public static async Task<IActionResult> CreateProduct(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "product")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Create Product");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<User>(requestBody);
            List<int> insertedUsers = new List<int>();

            try
            {

                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    if (!String.IsNullOrEmpty(input.name))
                    {
                        var query = $"IF NOT EXISTS (Select * from product where name = '{input.name}')BEGIN INSERT INTO [product] (name,remarks) VALUES('{input.name}', '{input.remarks}') END";
                        log.LogInformation($"Query = <{query}>");
                        SqlCommand command = new SqlCommand(query, connection);
                        int result = command.ExecuteNonQuery();
                        insertedUsers.Add(result);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }
            return new OkObjectResult(insertedUsers);
        }

        [FunctionName("SaveTransaction")]
        public static async Task<IActionResult> SaveTransaction(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Save Transaction");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<transaction>(requestBody);
            List<int> insertedTransactions = new List<int>();
            int result = 0;

            try
            {

                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    if (!String.IsNullOrEmpty(input.serials))
                    {
                        string[] serialList = input.serials.Split(",");
                        string query = "Insert Into transactions (personid,transactiondate,userid,serial,arparast,invtype,productid) VALUES ";
                        int counter = 0;
                        for (int i = 0; i < serialList.Length;i++)
                        {
                            query += $" ({input.personid},'{input.transactiondate}',{input.userid},'{serialList[i]}','{input.arparast}',{input.invtype},{input.productid}),";
                            counter++;
                            if (counter >= 999 || i >= serialList.Length-1)
                            {
                                query = $"{query.Substring(0, query.Length - 1)};";
                                log.LogInformation($"Query = <{query}>");
                                SqlCommand command = new SqlCommand(query, connection);
                                result += command.ExecuteNonQuery();                                
                                counter = 0;
                                query = "Insert Into transactions (personid,transactiondate,userid,serial,arparast,invtype,productid) VALUES ";
                            }
                        }
                        insertedTransactions.Add(result);
                        return new OkObjectResult(insertedTransactions);
                    }
                    else
                    {
                        return new BadRequestResult();
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }
            return new OkObjectResult(insertedTransactions);
        }
    }
}
