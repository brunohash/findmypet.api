using FindMyPet.Models;
using Dapper;
using MySqlConnector;
using FindMyPet.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace FindMyPet.Repository
{
	public class AccoutRepository : IAccoutRepository
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private readonly IDbConnection _mySql;

        public AccoutRepository(IConfiguration config)
        {
            _mySql = new MySqlConnection(config.GetConnectionString("mySqlGeneral"));
        }

        public async Task<UserAuthenticate> ViewAuthenticate(string user, string pass)
        {
            try
            {
                string passOn = generateMD5(pass);

                return await _mySql.QueryFirstOrDefaultAsync<UserAuthenticate>(@"
                        SELECT Id, User, Role FROM `FindMyPet`.`User.Api`
                        WHERE Status = 1 AND `User` = @User AND `Password` = @Pass
                      ",
                    new
                    {
                        User = user,
                        Pass = passOn
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<object> CreateUser(CreatedUser user)
        {
            int selectExistsEmail = await GetUser(user.Email);

            if (selectExistsEmail <= 0)
            {
                string passOn = generateMD5(user.Password);
                Guid recovery = Guid.NewGuid();

                try
                {
                    CreatedUserWithoutPassword save = await _mySql.QueryFirstAsync<CreatedUserWithoutPassword>(@"
                        Insert into `FindMyPet`.`User.Account`
                        (`name`, `birthday`, `email`, `password`, `recoveryPassword`, `created_at`)
                        VALUES
                        (@Name, @Birthday, @Email, @Password, @Recovery, NOW());
                        SELECT LAST_INSERT_ID() AS Id, @Name AS Name, @Birthday AS Birthday, @Email AS Email, @Recovery AS Recovery;
                      ",
                        new
                        {
                            Name = user.Name,
                            Birthday = user.Birthday,
                            Email = user.Email,
                            Password = passOn,
                            Recovery = recovery
                        });

                    if (save == null || !await createUserApi(user.Email, passOn))
                    {

                        MessageError message = new MessageError()
                        {
                            message = "Erro ao criar usuário, por favor tente novamente mais tarde!",
                            Date = DateTime.Now,
                            Reference = "Account 1"
                        };

                        return message;
                    }

                    return save;
                }
                catch (Exception ex)
                {
                    MessageError message = new MessageError()
                    {
                        message = "Erro ao criar usuário, por favor tente novamente mais tarde!",
                        Date = DateTime.Now,
                        Reference = "Account 2"
                    };

                    return message;
                }
            }
            else
            {
                MessageError message = new MessageError()
                {
                    message = "Troque seu e-mail e tente novamente!",
                    Date = DateTime.Now,
                    Reference = "Account 3"
                };

                return message;
            }
        }

        public Task<int> GetUser(string email)
        {
            return _mySql.ExecuteScalarAsync<int>(@"
                        SELECT * FROM `FindMyPet`.`User.Account`
                        WHERE `email` = @Email
                      ",
                    new
                    {
                        Email = email
                    });
        }

        public string generateMD5(string password)
        {
            var md5 = MD5.Create();
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(password);
            byte[] hash = md5.ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        public async Task<bool> createUserApi(string user, string password)
        {
            try
            {
                int save = await _mySql.ExecuteAsync(@"
                        Insert into `FindMyPet`.`User.Api`
                        (`User`, `Password`, `Role`, `Status`, `Created_at`)
                        VALUES
                        (@User, @Password, @Role, @Status, NOW())
                      ",
                        new
                        {
                            User = user,
                            Password = password,
                            Role = "admin",
                            Status = 1
                        });

                if (save < 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

