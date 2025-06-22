using Microsoft.Extensions.Configuration;
using panelOrmo.Models;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace panelOrmo.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // User Management
        public async Task<User> ValidateUser(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Users WHERE Username = @username AND IsActive = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString("Password");
                var storedSalt = reader.GetString("PasswordSalt");

                if (VerifyPasswordHash(password, storedHash, storedSalt))
                {
                    return new User
                    {
                        Id = reader.GetInt32("Id"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        IsSuperAdmin = reader.GetBoolean("IsSuperAdmin"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }
            }
            return null;
        }

        public async Task<List<User>> GetAllUsers()
        {
            var users = new List<User>();
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Users ORDER BY Username";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    IsSuperAdmin = reader.GetBoolean("IsSuperAdmin"),
                    IsActive = reader.GetBoolean("IsActive"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }
            return users;
        }

        public async Task<bool> CreateUser(UserCreateViewModel model, int createdBy)
        {
            try
            {
                _logger.LogInformation("Attempting to create user: {Username}", model.Username);
                
                CreatePasswordHash(model.Password, out string passwordHash, out string passwordSalt);
                _logger.LogDebug("Password hash and salt generated for user: {Username}", model.Username);

                using var connection = new SqlConnection(_connectionString);
                var query = @"INSERT INTO Users (Username, Password, Email, IsSuperAdmin, IsActive, CreatedDate, CreatedBy, PasswordSalt) 
                             VALUES (@username, @password, @email, @isSuperAdmin, 1, @createdDate, @createdBy, @passwordSalt)";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", model.Username);
                command.Parameters.AddWithValue("@password", passwordHash);
                command.Parameters.AddWithValue("@email", model.Email);
                command.Parameters.AddWithValue("@isSuperAdmin", model.IsSuperAdmin);
                command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                command.Parameters.AddWithValue("@createdBy", createdBy);
                command.Parameters.AddWithValue("@passwordSalt", passwordSalt);

                await connection.OpenAsync();
                _logger.LogDebug("Executing user creation query for: {Username}", model.Username);
                var result = await command.ExecuteNonQueryAsync();
                
                if (result > 0)
                {
                    _logger.LogInformation("Successfully created user: {Username}", model.Username);
                    return true;
                }
                else
                {
                    _logger.LogWarning("User creation failed for {Username}. No rows affected.", model.Username);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during user creation for {Username}", model.Username);
                return false;
            }
        }

        // News Management
        public async Task<List<News>> GetAllNews()
        {
            var newsList = new List<News>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT CID, CName, CTitle, CContent, CLanguageID, CImage, CIsValid, 
                         CDate, CDate2, CCreatedDate, CCreatedUserID, COrder 
                         FROM CMSContent WHERE CTypeID = 4 ORDER BY COrder DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                newsList.Add(new News
                {
                    CID = reader.GetInt32("CID"),
                    CName = reader.GetString("CName"),
                    CTitle = reader.GetString("CTitle"),
                    CContent = reader.IsDBNull("CContent") ? "" : reader.GetString("CContent"),
                    CLanguageID = reader.GetInt32("CLanguageID"),
                    CImage = reader.IsDBNull("CImage") ? "" : reader.GetString("CImage"),
                    CIsValid = reader.GetBoolean("CIsValid"),
                    CDate = reader.GetDateTime("CDate"),
                    CDate2 = reader.GetDateTime("CDate2"),
                    CCreatedDate = reader.GetDateTime("CCreatedDate"),
                    CCreatedUserID = reader.IsDBNull("CCreatedUserID") ? null : reader.GetInt32("CCreatedUserID"),
                    COrder = reader.GetInt32("COrder")
                });
            }
            return newsList;
        }

        public async Task<News> GetNewsById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT CID, CName, CTitle, CContent, CLanguageID, CImage, CIsValid, 
                         CDate, CDate2, CCreatedDate, CCreatedUserID, COrder 
                         FROM CMSContent WHERE CID = @id AND CTypeID = 4";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new News
                {
                    CID = reader.GetInt32("CID"),
                    CName = reader.GetString("CName"),
                    CTitle = reader.GetString("CTitle"),
                    CContent = reader.IsDBNull("CContent") ? "" : reader.GetString("CContent"),
                    CLanguageID = reader.GetInt32("CLanguageID"),
                    CImage = reader.IsDBNull("CImage") ? "" : reader.GetString("CImage"),
                    CIsValid = reader.GetBoolean("CIsValid"),
                    CDate = reader.GetDateTime("CDate"),
                    CDate2 = reader.GetDateTime("CDate2"),
                    CCreatedDate = reader.GetDateTime("CCreatedDate"),
                    CCreatedUserID = reader.IsDBNull("CCreatedUserID") ? null : reader.GetInt32("CCreatedUserID"),
                    COrder = reader.GetInt32("COrder")
                };
            }
            return null;
        }

        public async Task<bool> UpdateNews(int id, NewsViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var publishDate = model.PublishDate ?? DateTime.Now;

            var query = @"UPDATE CMSContent SET 
                         CName = @cname, 
                         CTitle = @ctitle, 
                         CLanguageID = @languageId, 
                         CContent = @content, 
                         CIsValid = @isValid, 
                         CDate = @date, 
                         CDate2 = @date2";

            if (imagePath != null)
            {
                query += ", CImage = @image";
            }

            query += " WHERE CID = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@cname", model.Title);
            command.Parameters.AddWithValue("@ctitle", model.Title);
            command.Parameters.AddWithValue("@languageId", model.LanguageID);
            command.Parameters.AddWithValue("@content", model.Content);
            command.Parameters.AddWithValue("@isValid", model.IsActive);
            command.Parameters.AddWithValue("@date", publishDate);
            command.Parameters.AddWithValue("@date2", publishDate);

            if (imagePath != null)
            {
                command.Parameters.AddWithValue("@image", imagePath);
            }

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> CreateNews(NewsViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get next CID and COrder
            var nextCID = await GetNextId(connection, "CMSContent", "CID");
            var nextOrder = await GetNextOrder(connection, "CMSContent", "COrder");

            var publishDate = model.PublishDate ?? DateTime.Now;

            var query = @"INSERT INTO CMSContent (CID, CName, CTitle, CParentCID, CWriterID, CTypeID, 
                         CLanguageID, CMemberID, CMenuID, CSurveyID, CSummary, CContent, COrder, 
                         CImage, CSecondImage, CThirdImage, CIsValid, CDate, CDate2, CURL, 
                         CIsCaption, CIsMainPage, CIsCommentEnable, CExternalURL, CObjectID, 
                         CObjectID2, CCreatedDate, CCreatedUserID)
                         VALUES (@cid, @cname, @ctitle, NULL, NULL, 4, @languageId, NULL, NULL, 
                         NULL, '', @content, @order, @image, NULL, NULL, @isValid, @date, 
                         @date2, '', 0, 0, 0, '', NULL, NULL, @createdDate, @createdUserId)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@cid", nextCID);
            command.Parameters.AddWithValue("@cname", model.Title);
            command.Parameters.AddWithValue("@ctitle", model.Title);
            command.Parameters.AddWithValue("@languageId", model.LanguageID);
            command.Parameters.AddWithValue("@content", model.Content);
            command.Parameters.AddWithValue("@order", nextOrder);
            command.Parameters.AddWithValue("@image", imagePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isValid", model.IsActive);
            command.Parameters.AddWithValue("@date", publishDate);
            command.Parameters.AddWithValue("@date2", publishDate);
            command.Parameters.AddWithValue("@createdDate", publishDate);
            command.Parameters.AddWithValue("@createdUserId", userId);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Collection Management
        public async Task<List<Collection>> GetAllCollections()
        {
            var collections = new List<Collection>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT DID, DName, DSummary, DPicture, DLanguageID, DIsValid, DCreatedDate, DCreatedUserID, DOrder FROM CMSDepartment WHERE DParentID = -1 ORDER BY DOrder DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                collections.Add(new Collection
                {
                    DID = reader.GetInt32("DID"),
                    DName = reader.GetString("DName"),
                    DSummary = reader.IsDBNull("DSummary") ? "" : reader.GetString("DSummary"),
                    DPicture = reader.IsDBNull("DPicture") ? "" : reader.GetString("DPicture"),
                    DLanguageID = reader.GetInt32("DLanguageID"),
                    DIsValid = reader.GetBoolean("DIsValid"),
                    DCreatedDate = reader.GetDateTime("DCreatedDate"),
                    DCreatedUserID = reader.IsDBNull("DCreatedUserID") ? null : reader.GetInt32("DCreatedUserID"),
                    DOrder = reader.GetInt32("DOrder")
                });
            }
            return collections;
        }

        public async Task<Collection> GetCollectionById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT DID, DName, DSummary, DPicture, DLanguageID, DIsValid, DCreatedDate, DCreatedUserID, DOrder FROM CMSDepartment WHERE DID = @id AND DParentID = -1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Collection
                {
                    DID = reader.GetInt32("DID"),
                    DName = reader.GetString("DName"),
                    DSummary = reader.IsDBNull("DSummary") ? "" : reader.GetString("DSummary"),
                    DPicture = reader.IsDBNull("DPicture") ? "" : reader.GetString("DPicture"),
                    DLanguageID = reader.GetInt32("DLanguageID"),
                    DIsValid = reader.GetBoolean("DIsValid"),
                    DCreatedDate = reader.GetDateTime("DCreatedDate"),
                    DCreatedUserID = reader.IsDBNull("DCreatedUserID") ? null : reader.GetInt32("DCreatedUserID"),
                    DOrder = reader.GetInt32("DOrder")
                };
            }
            return null;
        }

        public async Task<bool> UpdateCollection(int id, CollectionViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update CMSDepartment
                var deptQuery = @"UPDATE CMSDepartment SET 
                                 DLanguageID = @languageId, 
                                 DName = @dname, 
                                 DSummary = @dsummary, 
                                 DIsValid = @isValid";
                if (imagePath != null)
                {
                    deptQuery += ", DPicture = @dpicture";
                }
                deptQuery += " WHERE DID = @id AND DParentID = -1";

                using (var deptCommand = new SqlCommand(deptQuery, connection, transaction))
                {
                    deptCommand.Parameters.AddWithValue("@id", id);
                    deptCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                    deptCommand.Parameters.AddWithValue("@dname", model.Name);
                    deptCommand.Parameters.AddWithValue("@dsummary", model.Summary ?? "");
                    deptCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                    if (imagePath != null)
                    {
                        deptCommand.Parameters.AddWithValue("@dpicture", imagePath);
                    }
                    await deptCommand.ExecuteNonQueryAsync();
                }

                // Update CMSBanner
                var bannerQuery = @"UPDATE CMSBanner SET 
                                   BName = @bname, 
                                   BTitle = @btitle, 
                                   BSummary = @bsummary, 
                                   BLanguageID = @languageId, 
                                   BIsValid = @isValid";
                if (imagePath != null)
                {
                    bannerQuery += ", BImage = @bimage";
                }
                bannerQuery += " WHERE BObjectID = @id"; // Assuming BObjectID stores the collection DID

                using (var bannerCommand = new SqlCommand(bannerQuery, connection, transaction))
                {
                    bannerCommand.Parameters.AddWithValue("@id", id);
                    bannerCommand.Parameters.AddWithValue("@bname", model.Name);
                    bannerCommand.Parameters.AddWithValue("@btitle", model.Name);
                    bannerCommand.Parameters.AddWithValue("@bsummary", model.Summary ?? "");
                    bannerCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                    bannerCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                    if (imagePath != null)
                    {
                        bannerCommand.Parameters.AddWithValue("@bimage", imagePath);
                    }
                    await bannerCommand.ExecuteNonQueryAsync();
                }

                // Update CMSURIMapping
                var uriQuery = "UPDATE CMSURIMapping SET UMURI = @umuri WHERE UMTable = 'CMSDepartment' AND UMObjectID = @id";
                using (var uriCommand = new SqlCommand(uriQuery, connection, transaction))
                {
                    uriCommand.Parameters.AddWithValue("@id", id);
                    uriCommand.Parameters.AddWithValue("@umuri", model.Name.ToLower().Replace(" ", "-"));
                    await uriCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateCollection for ID: {ID}", id);
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> CreateCollection(CollectionViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Get next IDs
                var nextDID = await GetNextId(connection, "CMSDepartment", "DID", transaction);
                var nextUMID = await GetNextId(connection, "CMSURIMapping", "UMID", transaction);
                var nextDOrder = await GetNextOrder(connection, "CMSDepartment", "DOrder", transaction);
                var nextBID = await GetNextId(connection, "CMSBanner", "BID", transaction);
                var nextBOrder = await GetNextOrder(connection, "CMSBanner", "BOrder", transaction);

                var createdDate = DateTime.Now;

                // Insert into CMSDepartment (main collection)
                var deptQuery = @"INSERT INTO CMSDepartment (DID, DLanguageID, DParentID, DName, DListType, DSummary, DExplanation, DPicture, DSecondPicture, DThirdPicture, DFlash, DOrder, DIsValid, DMappedURL, DProductsMappedURL, DURL, DTarget, DFirstImageWidth, DFirstImageHeight, DSecondImageWidth, DSecondImageHeight, DThirdImageWidth, DThirdImageHeight, DCreatedDate, DCreatedUserID)
                                 VALUES (@did, @languageId, -1, @dname, 'List', @dsummary, '', @dpicture, '', '', '', @dorder, @isValid, '', '', '', '', 0, 0, 0, 0, 0, 0, @createdDate, @userId)";
                using var deptCommand = new SqlCommand(deptQuery, connection, transaction);
                deptCommand.Parameters.AddWithValue("@did", nextDID);
                deptCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                deptCommand.Parameters.AddWithValue("@dname", model.Name);
                deptCommand.Parameters.AddWithValue("@dsummary", model.Summary ?? "");
                deptCommand.Parameters.AddWithValue("@dpicture", imagePath ?? "");
                deptCommand.Parameters.AddWithValue("@dorder", nextDOrder);
                deptCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                deptCommand.Parameters.AddWithValue("@createdDate", createdDate);
                deptCommand.Parameters.AddWithValue("@userId", userId);
                await deptCommand.ExecuteNonQueryAsync();

                // Insert into CMSBanner (for banner info)
                var bannerQuery = @"INSERT INTO CMSBanner (BID, BName, BTitle, BSummary, BContent, BImage, BSecondImage, BThirdImage, BColor, BTypeID, BLanguageID, BMemberID, BMenuID, BMemberGroupID, BDate, BDate2, BStartTime, BEndTime, BDuration, BExternalURL, BOrder, BObjectID, BObjectID2, BObjectID3, BObjectID4, BObjectID5, BIsValid, BCreatedDate, BCreatedUserID)
                                   VALUES (@bid, @bname, @btitle, @bsummary, '', @bimage, NULL, NULL, '', 2, @languageId, NULL, NULL, NULL, @bdate, @bdate2, NULL, NULL, NULL, '/login', @border, @did, NULL, NULL, NULL, NULL, @isValid, @createdDate, @userId)";
                using var bannerCommand = new SqlCommand(bannerQuery, connection, transaction);
                bannerCommand.Parameters.AddWithValue("@bid", nextBID);
                bannerCommand.Parameters.AddWithValue("@bname", model.Name);
                bannerCommand.Parameters.AddWithValue("@btitle", model.Name);
                bannerCommand.Parameters.AddWithValue("@bsummary", model.Summary ?? "");
                bannerCommand.Parameters.AddWithValue("@bimage", imagePath ?? (object)DBNull.Value);
                bannerCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                bannerCommand.Parameters.AddWithValue("@bdate", createdDate);
                bannerCommand.Parameters.AddWithValue("@bdate2", createdDate.AddYears(4));
                bannerCommand.Parameters.AddWithValue("@border", nextBOrder);
                bannerCommand.Parameters.AddWithValue("@did", nextDID);
                bannerCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                bannerCommand.Parameters.AddWithValue("@createdDate", createdDate);
                bannerCommand.Parameters.AddWithValue("@userId", userId);
                await bannerCommand.ExecuteNonQueryAsync();

                // Insert into CMSURIMapping
                var uriQuery = @"INSERT INTO CMSURIMapping (UMID, UMTable, UMGroup, UMURI, UMObjectID, UMObjectID2, UMObjectID3, UMOrder, UMCreatedDate)
                                VALUES (@umid, 'CMSDepartment', NULL, @umuri, @objectId, NULL, NULL, 1, @createdDate)";
                using var uriCommand = new SqlCommand(uriQuery, connection, transaction);
                uriCommand.Parameters.AddWithValue("@umid", nextUMID);
                uriCommand.Parameters.AddWithValue("@umuri", model.Name.ToLower().Replace(" ", "-"));
                uriCommand.Parameters.AddWithValue("@objectId", nextDID);
                uriCommand.Parameters.AddWithValue("@createdDate", createdDate);
                await uriCommand.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return false;
            }
        }

        // Audit Log
        public async Task LogActivity(int userId, string username, string action, string tableName, int? recordId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"INSERT INTO AuditLog (UserId, Username, Action, TableName, RecordId, CreatedDate)
                         VALUES (@userId, @username, @action, @tableName, @recordId, @createdDate)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@action", action);
            command.Parameters.AddWithValue("@tableName", tableName);
            command.Parameters.AddWithValue("@recordId", recordId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdDate", DateTime.Now);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // Helper Methods
        private async Task<int> GetNextId(SqlConnection connection, string tableName, string idColumn, SqlTransaction transaction = null)
        {
            var query = $"SELECT ISNULL(MAX({idColumn}), 0) + 1 FROM {tableName}";
            using var command = new SqlCommand(query, connection, transaction);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task<int> GetNextOrder(SqlConnection connection, string tableName, string orderColumn, SqlTransaction transaction = null)
        {
            var query = $"SELECT ISNULL(MAX({orderColumn}), 0) + 1 FROM {tableName}";
            using var command = new SqlCommand(query, connection, transaction);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Collection Groups
        public async Task<List<CollectionGroup>> GetAllCollectionGroups()
        {
            var groups = new List<CollectionGroup>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT d.DID, d.DLanguageID, d.DParentID, d.DName, d.DPicture, d.DIsValid, 
                     d.DCreatedDate, d.DCreatedUserID, d.DOrder 
                     FROM CMSDepartment d 
                     WHERE d.DParentID != -1 
                     ORDER BY d.DOrder DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                groups.Add(new CollectionGroup
                {
                    DID = reader.GetInt32("DID"),
                    DLanguageID = reader.GetInt32("DLanguageID"),
                    DParentID = reader.GetInt32("DParentID"),
                    DName = reader.GetString("DName"),
                    DPicture = reader.IsDBNull("DPicture") ? "" : reader.GetString("DPicture"),
                    DIsValid = reader.GetBoolean("DIsValid"),
                    DCreatedDate = reader.GetDateTime("DCreatedDate"),
                    DCreatedUserID = reader.IsDBNull("DCreatedUserID") ? null : reader.GetInt32("DCreatedUserID"),
                    DOrder = reader.GetInt32("DOrder")
                });
            }
            return groups;
        }

        public async Task<CollectionGroup> GetCollectionGroupById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT d.DID, d.DLanguageID, d.DParentID, d.DName, d.DPicture, d.DIsValid, 
                     d.DCreatedDate, d.DCreatedUserID, d.DOrder 
                     FROM CMSDepartment d 
                     WHERE d.DID = @id AND d.DParentID != -1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new CollectionGroup
                {
                    DID = reader.GetInt32("DID"),
                    DLanguageID = reader.GetInt32("DLanguageID"),
                    DParentID = reader.GetInt32("DParentID"),
                    DName = reader.GetString("DName"),
                    DPicture = reader.IsDBNull("DPicture") ? "" : reader.GetString("DPicture"),
                    DIsValid = reader.GetBoolean("DIsValid"),
                    DCreatedDate = reader.GetDateTime("DCreatedDate"),
                    DCreatedUserID = reader.IsDBNull("DCreatedUserID") ? null : reader.GetInt32("DCreatedUserID"),
                    DOrder = reader.GetInt32("DOrder")
                };
            }
            return null;
        }

        public async Task<bool> UpdateCollectionGroup(int id, CollectionGroupViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Update CMSDepartment
                var deptQuery = @"UPDATE CMSDepartment SET 
                                 DLanguageID = @languageId, 
                                 DParentID = @parentId, 
                                 DName = @dname, 
                                 DIsValid = @isValid";
                if (imagePath != null)
                {
                    deptQuery += ", DPicture = @dpicture";
                }
                deptQuery += " WHERE DID = @id";

                using (var deptCommand = new SqlCommand(deptQuery, connection, transaction))
                {
                    deptCommand.Parameters.AddWithValue("@id", id);
                    deptCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                    deptCommand.Parameters.AddWithValue("@parentId", model.ParentCollectionID);
                    deptCommand.Parameters.AddWithValue("@dname", model.Name);
                    deptCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                    if (imagePath != null)
                    {
                        deptCommand.Parameters.AddWithValue("@dpicture", imagePath);
                    }
                    await deptCommand.ExecuteNonQueryAsync();
                }

                // Update CMSURIMapping
                var uriQuery = "UPDATE CMSURIMapping SET UMURI = @umuri WHERE UMTable = 'CMSDepartment' AND UMObjectID = @id";
                using (var uriCommand = new SqlCommand(uriQuery, connection, transaction))
                {
                    uriCommand.Parameters.AddWithValue("@id", id);
                    uriCommand.Parameters.AddWithValue("@umuri", model.Name.ToLower().Replace(" ", "-"));
                    await uriCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateCollectionGroup for ID: {ID}", id);
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> CreateCollectionGroup(CollectionGroupViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Get next IDs
                var nextDID = await GetNextId(connection, "CMSDepartment", "DID", transaction);
                var nextUMID = await GetNextId(connection, "CMSURIMapping", "UMID", transaction);
                var nextOrder = await GetNextOrder(connection, "CMSDepartment", "DOrder", transaction);
                var createdDate = DateTime.Now;

                // Insert into CMSDepartment
                var deptQuery = @"INSERT INTO CMSDepartment (DID, DLanguageID, DParentID, DName, DListType, 
                     DSummary, DExplanation, DPicture, DSecondPicture, DThirdPicture, DFlash, 
                     DOrder, DIsValid, DMappedURL, DProductsMappedURL, DURL, DTarget, 
                     DFirstImageWidth, DFirstImageHeight, DSecondImageWidth, DSecondImageHeight, 
                     DThirdImageWidth, DThirdImageHeight, DCreatedDate, DCreatedUserID)
                     VALUES (@did, @languageId, @parentId, @dname, 'List', '', '', @dpicture, '', '', 
                     '', @dorder, @isValid, '', '', '', '', 0, 0, 0, 0, 0, 0, @createdDate, @userId)";

                using var deptCommand = new SqlCommand(deptQuery, connection, transaction);
                deptCommand.Parameters.AddWithValue("@did", nextDID);
                deptCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                deptCommand.Parameters.AddWithValue("@parentId", model.ParentCollectionID);
                deptCommand.Parameters.AddWithValue("@dname", model.Name);
                deptCommand.Parameters.AddWithValue("@dpicture", imagePath ?? "");
                deptCommand.Parameters.AddWithValue("@dorder", nextOrder);
                deptCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                deptCommand.Parameters.AddWithValue("@createdDate", createdDate);
                deptCommand.Parameters.AddWithValue("@userId", userId);
                await deptCommand.ExecuteNonQueryAsync();

                // Insert into CMSURIMapping
                var uriQuery = @"INSERT INTO CMSURIMapping (UMID, UMTable, UMGroup, UMURI, UMObjectID, UMObjectID2, UMObjectID3, UMOrder, UMCreatedDate)
                                VALUES (@umid, 'CMSDepartment', NULL, @umuri, @objectId, NULL, NULL, 1, @createdDate)";
                using var uriCommand = new SqlCommand(uriQuery, connection, transaction);
                uriCommand.Parameters.AddWithValue("@umid", nextUMID);
                uriCommand.Parameters.AddWithValue("@umuri", model.Name.ToLower().Replace(" ", "-"));
                uriCommand.Parameters.AddWithValue("@objectId", nextDID);
                uriCommand.Parameters.AddWithValue("@createdDate", createdDate);
                await uriCommand.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateCollectionGroup");
                transaction.Rollback();
                return false;
            }
        }

        // Collection Products
        public async Task<List<CollectionProduct>> GetAllCollectionProducts()
        {
            var products = new List<CollectionProduct>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT P.PID, P.PCode, P.PName, P.PInfoPreview, P.PContent, P.PIsValid, 
                         P.PCreatedDate, P.PCreatedUserID
                         FROM CMSProduct P 
                         ORDER BY P.PCreatedDate DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new CollectionProduct
                {
                    PID = reader.GetInt32("PID"),
                    PCode = reader.GetString("PCode"),
                    PName = reader.GetString("PName"),
                    PInfoPreview = reader.IsDBNull("PInfoPreview") ? "" : reader.GetString("PInfoPreview"),
                    PContent = reader.IsDBNull("PContent") ? "" : reader.GetString("PContent"),
                    PIsValid = reader.GetBoolean("PIsValid"),
                    PCreatedDate = reader.GetDateTime("PCreatedDate"),
                    PCreatedUserID = reader.IsDBNull("PCreatedUserID") ? null : reader.GetInt32("PCreatedUserID")
                });
            }
            return products;
        }

        public async Task<List<CollectionProductIndexViewModel>> GetAllCollectionProductsWithGroups()
        {
            var products = new List<CollectionProductIndexViewModel>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT P.PID, P.PCode, P.PName, P.PIsValid, P.PCreatedDate, G.ParentCollectionGroupName
                            FROM CMSProduct P
                            OUTER APPLY (
                                SELECT TOP 1 D.DName AS ParentCollectionGroupName
                                FROM CMSProductDepartment PD
                                JOIN CMSDepartment D ON PD.PDDepartmentID = D.DID
                                WHERE PD.PDProductID = P.PID
                                ORDER BY PD.PDDepartmentID
                            ) G
                            ORDER BY P.PCreatedDate DESC";

            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new CollectionProductIndexViewModel
                {
                    PID = reader.GetInt32("PID"),
                    PCode = reader.GetString("PCode"),
                    PName = reader.GetString("PName"),
                    ParentCollectionGroupName = reader.IsDBNull("ParentCollectionGroupName") ? "No Parent Group" : reader.GetString("ParentCollectionGroupName"),
                    PIsValid = reader.GetBoolean("PIsValid"),
                    PCreatedDate = reader.GetDateTime("PCreatedDate")
                });
            }
            return products;
        }

        public async Task<CollectionProduct> GetCollectionProductById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT PID, PCode, PName, PInfoPreview, PContent, PIsValid, 
                     PCreatedDate, PCreatedUserID 
                     FROM CMSProduct 
                     WHERE PID = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new CollectionProduct
                {
                    PID = reader.GetInt32("PID"),
                    PCode = reader.IsDBNull("PCode") ? "" : reader.GetString("PCode"),
                    PName = reader.GetString("PName"),
                    PInfoPreview = reader.IsDBNull("PInfoPreview") ? null : reader.GetString("PInfoPreview"),
                    PContent = reader.IsDBNull("PContent") ? null : reader.GetString("PContent"),
                    PIsValid = reader.IsDBNull("PIsValid") ? true : reader.GetBoolean("PIsValid"),
                    PCreatedDate = reader.GetDateTime("PCreatedDate"),
                    PCreatedUserID = reader.IsDBNull("PCreatedUserID") ? null : reader.GetInt32("PCreatedUserID")
                };
            }
            return null;
        }

        private async Task<(int GroupId, string SmallImage, string MediumImage)> GetProductDetailsForEdit(int productId, SqlConnection connection, SqlTransaction transaction = null)
        {
            int groupId = 0;
            string smallImage = null;
            string mediumImage = null;

            // Get Group ID
            var groupQuery = "SELECT PDDepartmentID FROM CMSProductDepartment WHERE PDProductID = @productId";
            using (var groupCommand = new SqlCommand(groupQuery, connection, transaction))
            {
                groupCommand.Parameters.AddWithValue("@productId", productId);
                var result = await groupCommand.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    groupId = Convert.ToInt32(result);
                }
            }

            // Get Images
            var imageQuery = "SELECT PISmallImage, PIMediumImage FROM CMSProductImage WHERE PIProductID = @productId";
            using (var imageCommand = new SqlCommand(imageQuery, connection, transaction))
            {
                imageCommand.Parameters.AddWithValue("@productId", productId);
                using (var reader = await imageCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        smallImage = reader.IsDBNull("PISmallImage") ? null : reader.GetString("PISmallImage");
                        mediumImage = reader.IsDBNull("PIMediumImage") ? null : reader.GetString("PIMediumImage");
                    }
                }
            }

            return (groupId, smallImage, mediumImage);
        }

        public async Task<List<ProductImage>> GetProductImages(int productId)
        {
            var images = new List<ProductImage>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT PIID, PIProductID, PISmallImage, PIMediumImage, PIDescription, 
                 PIIsValid, PICreatedDate, PICreatedUserID 
                 FROM CMSProductImage 
                 WHERE PIProductID = @productId AND PIIsValid = 1
                 ORDER BY PICreatedDate";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@productId", productId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                images.Add(new ProductImage
                {
                    PIID = reader.GetInt32("PIID"),
                    PIProductID = reader.GetInt32("PIProductID"),
                    PISmallImage = reader.IsDBNull("PISmallImage") ? null : reader.GetString("PISmallImage"),
                    PIMediumImage = reader.IsDBNull("PIMediumImage") ? null : reader.GetString("PIMediumImage"),
                    PIDescription = reader.IsDBNull("PIDescription") ? null : reader.GetString("PIDescription"),
                    PIIsValid = reader.GetBoolean("PIIsValid"),
                    PICreatedDate = reader.GetDateTime("PICreatedDate"),
                    PICreatedUserID = reader.IsDBNull("PICreatedUserID") ? null : reader.GetInt32("PICreatedUserID")
                });
            }
            return images;
        }

        public async Task<CollectionProductEditViewModel> GetCollectionProductForEdit(int id)
        {
            var product = await GetCollectionProductById(id);
            if (product == null) return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var details = await GetProductDetailsForEdit(id, connection);

            // Get existing images
            var existingImages = await GetProductImages(id);
            var existingImagePairs = existingImages.Select((img, index) => new ExistingImagePair
            {
                PIID = img.PIID,
                NewSmallImage = null,
                NewMediumImage = null
            }).ToList();

            var viewModel = new CollectionProductEditViewModel
            {
                Product = new CollectionProductViewModel
                {
                    ProductCode = product.PCode,
                    Content = product.PContent,
                    IsActive = product.PIsValid,
                    CollectionGroupID = details.GroupId
                },
                ExistingImages = existingImagePairs,
                NewImagePairs = new List<ImagePair>(),
                // Keep for backward compatibility
                CurrentSmallImage = details.SmallImage,
                CurrentMediumImage = details.MediumImage
            };

            return viewModel;
        }

        public async Task<bool> UpdateCollectionProduct(int id, CollectionProductEditViewModel model, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var groupInfoResult = await GetCollectionGroupInfo(model.Product.CollectionGroupID, connection, transaction);
                if (!groupInfoResult.HasValue)
                {
                    transaction.Rollback();
                    return false;
                }
                var groupInfo = groupInfoResult.Value;
                var productName = $"{groupInfo.CollectionName} - {model.Product.ProductCode}";
                var formattedContent = $"<p>{model.Product.Content}</p>";

                // Update CMSProduct
                var productQuery = @"UPDATE CMSProduct SET 
                           PCode = @pcode, 
                           PStockCode = @pcode, 
                           PName = @pname, 
                           PInfoPreview = @pname, 
                           PContent = @content, 
                           PIsValid = @isValid
                           WHERE PID = @id";
                using (var productCommand = new SqlCommand(productQuery, connection, transaction))
                {
                    productCommand.Parameters.AddWithValue("@id", id);
                    productCommand.Parameters.AddWithValue("@pcode", model.Product.ProductCode);
                    productCommand.Parameters.AddWithValue("@pname", productName);
                    productCommand.Parameters.AddWithValue("@content", formattedContent);
                    productCommand.Parameters.AddWithValue("@isValid", model.Product.IsActive);
                    await productCommand.ExecuteNonQueryAsync();
                }

                // Update CMSProductDepartment
                var deptQuery = "UPDATE CMSProductDepartment SET PDDepartmentID = @deptId WHERE PDProductID = @id";
                using (var deptCommand = new SqlCommand(deptQuery, connection, transaction))
                {
                    deptCommand.Parameters.AddWithValue("@id", id);
                    deptCommand.Parameters.AddWithValue("@deptId", model.Product.CollectionGroupID);
                    await deptCommand.ExecuteNonQueryAsync();
                }

                var ftpService = new FTPService(_configuration);

                // Handle deleted images
                if (!string.IsNullOrEmpty(model.DeletedImageIds))
                {
                    var deletedIds = model.DeletedImageIds.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse);
                    foreach (var deletedId in deletedIds)
                    {
                        var deleteQuery = "DELETE FROM CMSProductImage WHERE PIID = @piid";
                        using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                        deleteCommand.Parameters.AddWithValue("@piid", deletedId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }

                // Update existing images
                if (model.ExistingImages != null)
                {
                    foreach (var existingImage in model.ExistingImages)
                    {
                        if (existingImage.NewSmallImage != null || existingImage.NewMediumImage != null)
                        {
                            string smallImagePath = null, mediumImagePath = null;

                            if (existingImage.NewSmallImage != null)
                            {
                                smallImagePath = await ftpService.UploadFile(existingImage.NewSmallImage, "/httpdocs/CMSFiles/ProductImages/SmallImage");
                            }
                            if (existingImage.NewMediumImage != null)
                            {
                                mediumImagePath = await ftpService.UploadFile(existingImage.NewMediumImage, "/httpdocs/CMSFiles/ProductImages/MediumImage");
                            }

                            var updateImageQuery = "UPDATE CMSProductImage SET ";
                            var updateParams = new List<string>();

                            if (smallImagePath != null)
                                updateParams.Add("PISmallImage = @smallImage");
                            if (mediumImagePath != null)
                                updateParams.Add("PIMediumImage = @mediumImage");

                            updateImageQuery += string.Join(", ", updateParams);
                            updateImageQuery += " WHERE PIID = @piid";

                            using var updateCommand = new SqlCommand(updateImageQuery, connection, transaction);
                            updateCommand.Parameters.AddWithValue("@piid", existingImage.PIID);
                            if (smallImagePath != null)
                                updateCommand.Parameters.AddWithValue("@smallImage", smallImagePath);
                            if (mediumImagePath != null)
                                updateCommand.Parameters.AddWithValue("@mediumImage", mediumImagePath);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Add new images
                if (model.NewImagePairs != null)
                {
                    foreach (var newImagePair in model.NewImagePairs)
                    {
                        if (newImagePair.SmallImage != null || newImagePair.MediumImage != null)
                        {
                            var nextPIID = await GetNextId(connection, "CMSProductImage", "PIID", transaction);
                            string smallImagePath = null, mediumImagePath = null;

                            if (newImagePair.SmallImage != null)
                            {
                                smallImagePath = await ftpService.UploadFile(newImagePair.SmallImage, "/httpdocs/CMSFiles/ProductImages/SmallImage");
                            }
                            if (newImagePair.MediumImage != null)
                            {
                                mediumImagePath = await ftpService.UploadFile(newImagePair.MediumImage, "/httpdocs/CMSFiles/ProductImages/MediumImage");
                            }

                            var insertImageQuery = @"INSERT INTO CMSProductImage (PIID, PIProductID, PIProductVariationID, 
                                           PIPositionX, PIPositionY, PISmallImage, PIMediumImage, PIBigImage, 
                                           PIDescription, PIGroup, PIOrder, PIIsValid, PICreatedDate, PICreatedUserID)
                                           VALUES (@piid, @productId, NULL, NULL, NULL, @smallImage, @mediumImage, '', 
                                           @description, '', 0, @isValid, @createdDate, @userId)";

                            using var insertCommand = new SqlCommand(insertImageQuery, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@piid", nextPIID);
                            insertCommand.Parameters.AddWithValue("@productId", id);
                            insertCommand.Parameters.AddWithValue("@smallImage", smallImagePath ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@mediumImage", mediumImagePath ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@description", $"{groupInfo.GroupName} - {model.Product.ProductCode}");
                            insertCommand.Parameters.AddWithValue("@isValid", model.Product.IsActive);
                            insertCommand.Parameters.AddWithValue("@createdDate", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@userId", userId);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateCollectionProduct for ID: {ID}", id);
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> CreateCollectionProduct(CollectionProductViewModel model, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Get collection group info
                var groupInfoResult = await GetCollectionGroupInfo(model.CollectionGroupID, connection, transaction);
                if (!groupInfoResult.HasValue)
                {
                    transaction.Rollback();
                    return false;
                }

                var groupInfo = groupInfoResult.Value;

                // Get next IDs
                var nextPID = await GetNextId(connection, "CMSProduct", "PID", transaction);
                var nextPDID = await GetNextId(connection, "CMSProductDepartment", "PDID", transaction);

                var createdDate = DateTime.Now;
                var productName = $"{groupInfo.CollectionName} - {model.ProductCode}";
                var formattedContent = $"<p>{model.Content}</p>";

                // Insert into CMSProduct
                var productQuery = @"INSERT INTO CMSProduct (PID, PDepartmentID, PListType, PCode, PStockCode, 
                           PName, PBrand, PInfoPreview, PInfo, PLittlePicture, PMiddlePicture, PBigPicture, 
                           PRawPrice, PPrice, PStockAmount, PMinStockAmount, PRealStockAmount, PIsNew, 
                           PVatRatio, PDiscountRatio, PVatIncluded, PMoneyTypeID, POrder, PDesi, PSeason, 
                           PSeasonDescription, PColorFirst, PColorSecond, PContent, PTechnical, P3D, 
                           PSaleStartDate, PSaleFinishDate, PIsNebimIntegration, PVirtualPrice, 
                           PIsVirtualPriceEnable, PIsValid, PCreatedDate, PCreatedUserID)
                           VALUES (@pid, NULL, '', @pcode, @pcode, @pname, '', @pname, NULL, NULL, NULL, NULL, 
                           0.00, 0.00, NULL, NULL, NULL, NULL, 0.00, 0.00, NULL, NULL, NULL, NULL, NULL, 
                           NULL, NULL, NULL, @content, NULL, NULL, NULL, NULL, 0, NULL, NULL, @isValid, 
                           @createdDate, @userId)";

                using var productCommand = new SqlCommand(productQuery, connection, transaction);
                productCommand.Parameters.AddWithValue("@pid", nextPID);
                productCommand.Parameters.AddWithValue("@pcode", model.ProductCode);
                productCommand.Parameters.AddWithValue("@pname", productName);
                productCommand.Parameters.AddWithValue("@content", formattedContent);
                productCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                productCommand.Parameters.AddWithValue("@createdDate", createdDate);
                productCommand.Parameters.AddWithValue("@userId", userId);
                await productCommand.ExecuteNonQueryAsync();

                // Insert into CMSProductDepartment
                var deptQuery = @"INSERT INTO CMSProductDepartment (PDID, PDDepartmentID, PDProductID, 
                         PDOrder, PDCreatedDate, PDCreatedUserID)
                         VALUES (@pdid, @deptId, @productId, 1, @createdDate, @userId)";

                using var deptCommand = new SqlCommand(deptQuery, connection, transaction);
                deptCommand.Parameters.AddWithValue("@pdid", nextPDID);
                deptCommand.Parameters.AddWithValue("@deptId", model.CollectionGroupID);
                deptCommand.Parameters.AddWithValue("@productId", nextPID);
                deptCommand.Parameters.AddWithValue("@createdDate", createdDate);
                deptCommand.Parameters.AddWithValue("@userId", userId);
                await deptCommand.ExecuteNonQueryAsync();

                // Handle multiple image uploads
                var ftpService = new FTPService(_configuration);

                // Process ImagePairs (new way)
                if (model.ImagePairs != null && model.ImagePairs.Any())
                {
                    foreach (var imagePair in model.ImagePairs)
                    {
                        if (imagePair.SmallImage != null || imagePair.MediumImage != null)
                        {
                            var nextPIID = await GetNextId(connection, "CMSProductImage", "PIID", transaction);

                            string smallImagePath = null, mediumImagePath = null;

                            if (imagePair.SmallImage != null)
                            {
                                smallImagePath = await ftpService.UploadFile(imagePair.SmallImage, "/httpdocs/CMSFiles/ProductImages/SmallImage");
                            }

                            if (imagePair.MediumImage != null)
                            {
                                mediumImagePath = await ftpService.UploadFile(imagePair.MediumImage, "/httpdocs/CMSFiles/ProductImages/MediumImage");
                            }

                            var imageQuery = @"INSERT INTO CMSProductImage (PIID, PIProductID, PIProductVariationID, 
                                     PIPositionX, PIPositionY, PISmallImage, PIMediumImage, PIBigImage, 
                                     PIDescription, PIGroup, PIOrder, PIIsValid, PICreatedDate, PICreatedUserID)
                                     VALUES (@piid, @productId, NULL, NULL, NULL, @smallImage, @mediumImage, '', 
                                     @description, '', 0, @isValid, @createdDate, @userId)";

                            using var imageCommand = new SqlCommand(imageQuery, connection, transaction);
                            imageCommand.Parameters.AddWithValue("@piid", nextPIID);
                            imageCommand.Parameters.AddWithValue("@productId", nextPID);
                            imageCommand.Parameters.AddWithValue("@smallImage", smallImagePath ?? (object)DBNull.Value);
                            imageCommand.Parameters.AddWithValue("@mediumImage", mediumImagePath ?? (object)DBNull.Value);
                            imageCommand.Parameters.AddWithValue("@description", $"{groupInfo.GroupName} - {model.ProductCode}");
                            imageCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                            imageCommand.Parameters.AddWithValue("@createdDate", createdDate);
                            imageCommand.Parameters.AddWithValue("@userId", userId);
                            await imageCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
                // Backward compatibility - handle old single image upload
                else if (model.SmallImage != null || model.MediumImage != null)
                {
                    var nextPIID = await GetNextId(connection, "CMSProductImage", "PIID", transaction);
                    string smallImagePath = null, mediumImagePath = null;

                    if (model.SmallImage != null)
                    {
                        smallImagePath = await ftpService.UploadFile(model.SmallImage, "/httpdocs/CMSFiles/ProductImages/SmallImage");
                    }

                    if (model.MediumImage != null)
                    {
                        mediumImagePath = await ftpService.UploadFile(model.MediumImage, "/httpdocs/CMSFiles/ProductImages/MediumImage");
                    }

                    var imageQuery = @"INSERT INTO CMSProductImage (PIID, PIProductID, PIProductVariationID, 
                             PIPositionX, PIPositionY, PISmallImage, PIMediumImage, PIBigImage, 
                             PIDescription, PIGroup, PIOrder, PIIsValid, PICreatedDate, PICreatedUserID)
                             VALUES (@piid, @productId, NULL, NULL, NULL, @smallImage, @mediumImage, '', 
                             @description, '', 0, @isValid, @createdDate, @userId)";

                    using var imageCommand = new SqlCommand(imageQuery, connection, transaction);
                    imageCommand.Parameters.AddWithValue("@piid", nextPIID);
                    imageCommand.Parameters.AddWithValue("@productId", nextPID);
                    imageCommand.Parameters.AddWithValue("@smallImage", smallImagePath ?? (object)DBNull.Value);
                    imageCommand.Parameters.AddWithValue("@mediumImage", mediumImagePath ?? (object)DBNull.Value);
                    imageCommand.Parameters.AddWithValue("@description", $"{groupInfo.GroupName} - {model.ProductCode}");
                    imageCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                    imageCommand.Parameters.AddWithValue("@createdDate", createdDate);
                    imageCommand.Parameters.AddWithValue("@userId", userId);
                    await imageCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateCollectionProduct");
                transaction.Rollback();
                return false;
            }
        }

        private async Task<(string CollectionName, string GroupName)?> GetCollectionGroupInfo(int groupId, SqlConnection connection, SqlTransaction transaction)
        {
            var query = @"SELECT cg.DName as GroupName, c.DName as CollectionName 
                         FROM CMSDepartment cg 
                         JOIN CMSDepartment c ON cg.DParentID = c.DID 
                         WHERE cg.DID = @groupId";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@groupId", groupId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetString("CollectionName"), reader.GetString("GroupName"));
            }
            return null; // Return null if no data found
        }

        // User Management
        public async Task<bool> ChangeUserPassword(int userId, string newPassword)
        {
            CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);

            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE Users SET Password = @password, PasswordSalt = @passwordSalt WHERE Id = @userId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@password", passwordHash);
            command.Parameters.AddWithValue("@passwordSalt", passwordSalt);
            command.Parameters.AddWithValue("@userId", userId);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Status Toggle Methods
        public async Task<bool> ToggleNewsStatus(int newsId, bool status)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE CMSContent SET CIsValid = @status WHERE CID = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@id", newsId);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> ToggleCollectionStatus(int collectionId, bool status)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE CMSBanner SET BIsValid = @status WHERE BID = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@id", collectionId);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Audit Log Methods
        public async Task<List<AuditLog>> GetAuditLogs(int pageSize = 100)
        {
            var logs = new List<AuditLog>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT TOP (@pageSize) Id, UserId, Username, Action, TableName, 
                     RecordId, CreatedDate FROM AuditLog ORDER BY CreatedDate DESC";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32("Id"),
                    UserId = reader.GetInt32("UserId"),
                    Username = reader.GetString("Username"),
                    Action = reader.GetString("Action"),
                    TableName = reader.GetString("TableName"),
                    RecordId = reader.IsDBNull("RecordId") ? null : reader.GetInt32("RecordId"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }
            return logs;
        }

        public async Task<List<AuditLog>> GetRecentAuditLogs(int count = 10)
        {
            return await GetAuditLogs(count);
        }

        // Dashboard Statistics
        public async Task<Dictionary<string, int>> GetDashboardStats()
        {
            var stats = new Dictionary<string, int>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // News count
            var newsQuery = "SELECT COUNT(*) FROM CMSContent WHERE CTypeID = 4 AND CIsValid = 1";
            using var newsCmd = new SqlCommand(newsQuery, connection);
            stats["News"] = (int)await newsCmd.ExecuteScalarAsync();

            // Collections count
            var collectionsQuery = "SELECT COUNT(*) FROM CMSBanner WHERE BTypeID = 2 AND BIsValid = 1";
            using var collectionsCmd = new SqlCommand(collectionsQuery, connection);
            stats["Collections"] = (int)await collectionsCmd.ExecuteScalarAsync();

            // Collection Groups count
            var groupsQuery = "SELECT COUNT(*) FROM CMSDepartment WHERE DParentID != -1 AND DIsValid = 1";
            using var groupsCmd = new SqlCommand(groupsQuery, connection);
            stats["CollectionGroups"] = (int)await groupsCmd.ExecuteScalarAsync();

            // Products count
            var productsQuery = "SELECT COUNT(*) FROM CMSProduct WHERE PCode IS NOT NULL AND PCode != '' AND PIsValid = 1";
            using var productsCmd = new SqlCommand(productsQuery, connection);
            stats["Products"] = (int)await productsCmd.ExecuteScalarAsync();

            return stats;
        }

        // DELETE METHODS
        public async Task<bool> DeleteNews(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "DELETE FROM CMSContent WHERE CID = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        public async Task<bool> DeleteCollection(int id)
        {
            _logger.LogDebug("DeleteCollection called for ID: {ID}", id);
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Delete from CMSURIMapping
                var uriQuery = "DELETE FROM CMSURIMapping WHERE UMTable = 'CMSDepartment' AND UMObjectID = @id";
                using (var uriCommand = new SqlCommand(uriQuery, connection, transaction))
                {
                    uriCommand.Parameters.AddWithValue("@id", id);
                    var uriResult = await uriCommand.ExecuteNonQueryAsync();
                    _logger.LogDebug("DeleteCollection CMSURIMapping result for UMObjectID {ID}: {Result}", id, uriResult);
                }

                // Delete from CMSDepartment (main collection)
                var deptQuery = "DELETE FROM CMSDepartment WHERE DID = @id AND DParentID = -1";
                using (var deptCommand = new SqlCommand(deptQuery, connection, transaction))
                {
                    deptCommand.Parameters.AddWithValue("@id", id);
                    var deptResult = await deptCommand.ExecuteNonQueryAsync();
                    _logger.LogDebug("DeleteCollection CMSDepartment result for DID {DID}: {Result}", id, deptResult);
                }

                // Delete from CMSBanner (banner info)
                var bannerQuery = "DELETE FROM CMSBanner WHERE BID = @id";
                using (var bannerCommand = new SqlCommand(bannerQuery, connection, transaction))
                {
                    bannerCommand.Parameters.AddWithValue("@id", id);
                    var bannerResult = await bannerCommand.ExecuteNonQueryAsync();
                    _logger.LogDebug("DeleteCollection CMSBanner result for BID {BID}: {Result}", id, bannerResult);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Exception in DeleteCollection for ID: {ID}", id);
                return false;
            }
        }
        public async Task<bool> DeleteCollectionGroup(int id)
        {
            _logger.LogDebug("DeleteCollectionGroup called for ID: {ID}", id);
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Delete from CMSURIMapping
                var uriQuery = "DELETE FROM CMSURIMapping WHERE UMTable = 'CMSDepartment' AND UMObjectID = @id";
                using (var uriCommand = new SqlCommand(uriQuery, connection, transaction))
                {
                    uriCommand.Parameters.AddWithValue("@id", id);
                    await uriCommand.ExecuteNonQueryAsync();
                }

                // Delete from CMSDepartment
                var deptQuery = "DELETE FROM CMSDepartment WHERE DID = @id";
                using (var deptCommand = new SqlCommand(deptQuery, connection, transaction))
                {
                    deptCommand.Parameters.AddWithValue("@id", id);
                    await deptCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Exception in DeleteCollectionGroup for ID: {ID}", id);
                return false;
            }
        }
        public async Task<bool> DeleteCollectionProduct(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "DELETE FROM CMSProduct WHERE PID = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Password Hashing
        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            passwordHash = Convert.ToBase64String(hash);
            passwordSalt = Convert.ToBase64String(salt);
        }

        private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            return Convert.ToBase64String(hash) == storedHash;
        }

        public (string, string) GetHashAndSalt(string password)
        {
            CreatePasswordHash(password, out string hash, out string salt);
            return (hash, salt);
        }
    }
}
