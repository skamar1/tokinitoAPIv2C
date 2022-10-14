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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getusers/{id?}")] HttpRequest req,
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
            string isSale = req.Query["issale"];
            int isSaleBit = 3;
            log.LogInformation($"isSale = {isSale ?? "false"}");
            if (isSale == "true")
            {
                isSaleBit = 2;
            }

            string whereQuery = $"";

            string productID = req.Query["productid"];
            log.LogInformation($"Product ID = '{productID}'");
            string searchAProduct = "";
            if (!string.IsNullOrEmpty(productID))
            {
                searchAProduct = $"and productID = {productID} ";// Where DT.serial in (select serial from transactions where productid = 5) ";
            }

            string serials = req.Query["serials"];
            string serialQuery = null;
            log.LogInformation($"serials = {serials ?? "null"}");
            if (!string.IsNullOrEmpty(serials))
            {
                if (string.IsNullOrEmpty(whereQuery))
                {
                    serialQuery = $" where DT.serial in ({serials}) ";
                }
                else
                {
                    serialQuery += $" and DT.serial in ({serials}) ";
                }
            }

            string checkForDuplicates = ">";
            if ((req.Query["showduplicates"].ToString() ?? "true") == "false")
            {
                checkForDuplicates = "=";
            }
            log.LogInformation($"checkForDuplicates = {req.Query["showduplicates"].ToString()}");


            List<serialExist> serials1 = new List<serialExist>();

            var query = $"select DT.serial,REPLACE(REPLACE(REPLACE(REPLACE(STUFF((select '; ',TR.arParast,CONVERT(varchar,TR.transactionDate,103) transactionDate from transactions TR where TR.serial = DT.serial FOR XML PATH('')),1,1,''),'<arParast>',''),'</arParast>',','),'<transactionDate>',''),'</transactionDate>','') [ArithParast] "+
                        $"from ("+
                        $"select serial from ("+
                        $"SELECT  serial, COUNT (serial) AS [DUBL] "+
                        $"FROM  transactions where invType = {isSaleBit} {searchAProduct} GROUP BY serial ) as a where DUBL {checkForDuplicates} 1) DT {serialQuery} GROUP BY serial order by 1";
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
                        string result = reader["ArithParast"].ToString();
                        
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
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/{id?}")] HttpRequest req,
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
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "product/{id?}")] HttpRequest req,
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

        [FunctionName("GetAllSales")]
        public static async Task<IActionResult> GetAllSales(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getallsales")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get GetAllSales");
            string productID = req.Query["productid"];
            log.LogInformation($"productID = {productID ?? "null"}");
            string serial = req.Query["serial"];
            log.LogInformation($"Serial = {serial}");

            List<getallorders> orders = new List<getallorders>();

            string productSearch = $" and productID = {productID} ";
            string serialSerch = $" and serial = '{serial}'";

            if (string.IsNullOrEmpty(productID))
            {
                productSearch = "";
            }
            if (string.IsNullOrEmpty(serial))
            {
                serialSerch = "";
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = $"select PER.serial,(CASE WHEN CONVERT(DATE, PER.transactiondate) = '1900-01-01 00:00:00.000' THEN '' ELSE CONVERT(CHAR(10), PER.transactiondate, 103) END) AS [purchaseDate],"+
                                $"[PER].arParast,PER.name,(CASE WHEN CONVERT(DATE, sell.transactionDate) = '1900-01-01' THEN '' ELSE CONVERT(CHAR(10), sell.transactionDate, 103) END) AS [SaleDate], "+
                                $"ISNULL([sell].arParast,'') [arPar],ISNULL(sell.name,'') [Customer] "+
                                $"from ("+
                                $"    select transactiondate ,arParast ,name ,serial   "+
                                $"    from transactions left join people on people.id = personid where invType = 3{productSearch}{serialSerch}) as PER "+
                                $"       Left join ("+
                                $"       select transactiondate ,arParast ,name,serial"+
                                $"       from transactions left join people on people.id = personid where invType = 2{productSearch}{serialSerch}) as sell on sell.serial = PER.serial " +
                                $" order by purchaseDate DESC";
                    log.LogInformation($"Query ==> {query} <==");
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        getallorders transactions = new getallorders();
                        transactions.serial = reader["serial"].ToString();
                        transactions.purchaseDate = reader["purchaseDate"].ToString();
                        transactions.arParast = reader["arParast"].ToString();
                        transactions.name = reader["name"].ToString();
                        transactions.SaleDate = reader["SaleDate"].ToString();
                        transactions.arPar = reader["arPar"].ToString();
                        transactions.Customer = reader["Customer"].ToString();

                        orders.Add(transactions);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return new OkObjectResult(orders);

        }

        [FunctionName("GetDoubleSerial")]
        public static async Task<IActionResult> GetDoubleSerial(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getdoubleserial")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get GetAllSales");
            string productID = req.Query["productid"];
            log.LogInformation($"productID = {productID ?? "null"}");

            List<getallorders> orders = new List<getallorders>();

            if (string.IsNullOrEmpty(productID))
            {
                return new OkObjectResult(orders);
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = $"select PER.serial,(CASE WHEN CONVERT(DATE, PER.transactiondate) = '1900-01-01 00:00:00.000' THEN '' ELSE CONVERT(CHAR(10), PER.transactiondate, 103) END) AS [purchaseDate]," +
                                $"[PER].arParast,PER.name,(CASE WHEN CONVERT(DATE, sell.transactionDate) = '1900-01-01' THEN '' ELSE CONVERT(CHAR(10), sell.transactionDate, 103) END) AS [SaleDate], " +
                                $"ISNULL([sell].arParast,'') [arPar],ISNULL(sell.name,'') [Customer] " +
                                $"from (" +
                                $"    select transactiondate ,arParast ,name ,serial   " +
                                $"    from transactions left join people on people.id = personid where invType = 3 and productID = {productID} ) as PER " +
                                $"       Left join (" +
                                $"       select transactiondate ,arParast ,name,serial" +
                                $"       from transactions left join people on people.id = personid where invType = 2 and productID = {productID}) as sell on sell.serial = PER.serial " +
                                $" order by purchaseDate DESC";
                    log.LogInformation($"Query ==> {query} <==");
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        getallorders user = new getallorders();
                        user.serial = reader["serial"].ToString();
                        user.purchaseDate = reader["purchaseDate"].ToString();
                        user.arParast = reader["arParast"].ToString();
                        user.name = reader["name"].ToString();
                        user.SaleDate = reader["SaleDate"].ToString();
                        user.arPar = reader["arPar"].ToString();
                        user.Customer = reader["Customer"].ToString();

                        orders.Add(user);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return new OkObjectResult(orders);

        }
    }
}
