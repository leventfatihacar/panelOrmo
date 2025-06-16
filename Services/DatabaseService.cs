using Microsoft.Extensions.Configuration;
using panelOrmo.Models;
using System.Data;
using System.Data.SqlClient;

namespace panelOrmo.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // User Management
        public async Task<User> ValidateUser(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Users WHERE Username = @username AND Password = @password AND IsActive = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password); // In production, use hashed passwords

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
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
            using var connection = new SqlConnection(_connectionString);
            var query = @"INSERT INTO Users (Username, Password, Email, IsSuperAdmin, IsActive, CreatedDate, CreatedBy) 
                         VALUES (@username, @password, @email, @isSuperAdmin, 1, @createdDate, @createdBy)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", model.Username);
            command.Parameters.AddWithValue("@password", model.Password); // In production, hash the password
            command.Parameters.AddWithValue("@email", model.Email);
            command.Parameters.AddWithValue("@isSuperAdmin", model.IsSuperAdmin);
            command.Parameters.AddWithValue("@createdDate", DateTime.Now);
            command.Parameters.AddWithValue("@createdBy", createdBy);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
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

        public async Task<bool> CreateNews(NewsViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);

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

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Collection Management
        public async Task<List<Collection>> GetAllCollections()
        {
            var collections = new List<Collection>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT BID, BName, BTitle, BSummary, BImage, BLanguageID, BIsValid, 
                         BDate, BDate2, BCreatedDate, BCreatedUserID, BOrder 
                         FROM CMSBanner WHERE BTypeID = 2 ORDER BY BOrder DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                collections.Add(new Collection
                {
                    BID = reader.GetInt32("BID"),
                    BName = reader.GetString("BName"),
                    BTitle = reader.GetString("BTitle"),
                    BSummary = reader.IsDBNull("BSummary") ? "" : reader.GetString("BSummary"),
                    BImage = reader.IsDBNull("BImage") ? "" : reader.GetString("BImage"),
                    BLanguageID = reader.GetInt32("BLanguageID"),
                    BIsValid = reader.GetBoolean("BIsValid"),
                    BDate = reader.GetDateTime("BDate"),
                    BDate2 = reader.GetDateTime("BDate2"),
                    BCreatedDate = reader.GetDateTime("BCreatedDate"),
                    BCreatedUserID = reader.IsDBNull("BCreatedUserID") ? null : reader.GetInt32("BCreatedUserID"),
                    BOrder = reader.GetInt32("BOrder")
                });
            }
            return collections;
        }

        public async Task<bool> CreateCollection(CollectionViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Get next IDs
                var nextBID = await GetNextId(connection, "CMSBanner", "BID", transaction);
                var nextDID = await GetNextId(connection, "CMSDepartment", "DID", transaction);
                var nextUMID = await GetNextId(connection, "CMSURIMapping", "UMID", transaction);
                var nextBOrder = await GetNextOrder(connection, "CMSBanner", "BOrder", transaction);
                var nextDOrder = await GetNextOrder(connection, "CMSDepartment", "DOrder", transaction);

                var startDate = model.StartDate ?? DateTime.Now;
                var endDate = startDate.AddYears(4);

                // Insert into CMSBanner
                var bannerQuery = @"INSERT INTO CMSBanner (BID, BName, BTitle, BSummary, BContent, BImage, 
                                   BSecondImage, BThirdImage, BColor, BTypeID, BLanguageID, BMemberID, 
                                   BMenuID, BMemberGroupID, BDate, BDate2, BStartTime, BEndTime, BDuration, 
                                   BExternalURL, BOrder, BObjectID, BObjectID2, BObjectID3, BObjectID4, 
                                   BObjectID5, BIsValid, BCreatedDate, BCreatedUserID)
                                   VALUES (@bid, @bname, @btitle, @bsummary, '', @bimage, NULL, NULL, '', 
                                   2, @languageId, NULL, NULL, NULL, @bdate, @bdate2, NULL, NULL, NULL, 
                                   '/login', @border, NULL, NULL, NULL, NULL, NULL, @isValid, @createdDate, @userId)";

                using var bannerCommand = new SqlCommand(bannerQuery, connection, transaction);
                bannerCommand.Parameters.AddWithValue("@bid", nextBID);
                bannerCommand.Parameters.AddWithValue("@bname", model.Name);
                bannerCommand.Parameters.AddWithValue("@btitle", model.Name);
                bannerCommand.Parameters.AddWithValue("@bsummary", model.Summary ?? "");
                bannerCommand.Parameters.AddWithValue("@bimage", imagePath ?? (object)DBNull.Value);
                bannerCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                bannerCommand.Parameters.AddWithValue("@bdate", startDate);
                bannerCommand.Parameters.AddWithValue("@bdate2", endDate);
                bannerCommand.Parameters.AddWithValue("@border", nextBOrder);
                bannerCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                bannerCommand.Parameters.AddWithValue("@createdDate", startDate);
                bannerCommand.Parameters.AddWithValue("@userId", userId);
                await bannerCommand.ExecuteNonQueryAsync();

                // Insert into CMSDepartment
                var deptQuery = @"INSERT INTO CMSDepartment (DID, DLanguageID, DParentID, DName, DListType, 
                                 DSummary, DExplanation, DPicture, DSecondPicture, DThirdPicture, DFlash, 
                                 DOrder, DIsValid, DMappedURL, DProductsMappedURL, DURL, DTarget, 
                                 DFirstImageWidth, DFirstImageHeight, DSecondImageWidth, DSecondImageHeight, 
                                 DThirdImageWidth, DThirdImageHeight, DCreatedDate, DCreatedUserID)
                                 VALUES (@did, @languageId, -1, @dname, 'List', '', '', @dpicture, '', '', 
                                 '', @dorder, @isValid, '', '', '', '', 0, 0, 0, 0, 0, 0, @createdDate, @userId)";

                using var deptCommand = new SqlCommand(deptQuery, connection, transaction);
                deptCommand.Parameters.AddWithValue("@did", nextDID);
                deptCommand.Parameters.AddWithValue("@languageId", model.LanguageID);
                deptCommand.Parameters.AddWithValue("@dname", model.Name);
                deptCommand.Parameters.AddWithValue("@dpicture", imagePath ?? "");
                deptCommand.Parameters.AddWithValue("@dorder", nextDOrder);
                deptCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                deptCommand.Parameters.AddWithValue("@createdDate", startDate);
                deptCommand.Parameters.AddWithValue("@userId", userId);
                await deptCommand.ExecuteNonQueryAsync();

                // Insert into CMSURIMapping
                var uriQuery = @"INSERT INTO CMSURIMapping (UMID, UMTable, UMGroup, UMURI, UMObjectID, 
                                UMObjectID2, UMObjectID3, UMOrder, UMCreatedDate)
                                VALUES (@umid, 'CMSDepartment', NULL, @umuri, @objectId, NULL, NULL, 1, @createdDate)";

                using var uriCommand = new SqlCommand(uriQuery, connection, transaction);
                uriCommand.Parameters.AddWithValue("@umid", nextUMID);
                uriCommand.Parameters.AddWithValue("@umuri", model.Name.ToLower().Replace(" ", "-"));
                uriCommand.Parameters.AddWithValue("@objectId", nextDID);
                uriCommand.Parameters.AddWithValue("@createdDate", startDate);
                await uriCommand.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch
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

        public async Task<bool> CreateCollectionGroup(CollectionGroupViewModel model, int userId, string imagePath = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var nextDID = await GetNextId(connection, "CMSDepartment", "DID");
            var nextOrder = await GetNextOrder(connection, "CMSDepartment", "DOrder");
            var createdDate = DateTime.Now;

            var query = @"INSERT INTO CMSDepartment (DID, DLanguageID, DParentID, DName, DListType, 
                     DSummary, DExplanation, DPicture, DSecondPicture, DThirdPicture, DFlash, 
                     DOrder, DIsValid, DMappedURL, DProductsMappedURL, DURL, DTarget, 
                     DFirstImageWidth, DFirstImageHeight, DSecondImageWidth, DSecondImageHeight, 
                     DThirdImageWidth, DThirdImageHeight, DCreatedDate, DCreatedUserID)
                     VALUES (@did, @languageId, @parentId, @dname, 'List', '', '', @dpicture, '', '', 
                     '', @dorder, @isValid, '', '', '', '', 0, 0, 0, 0, 0, 0, @createdDate, @userId)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@did", nextDID);
            command.Parameters.AddWithValue("@languageId", model.LanguageID);
            command.Parameters.AddWithValue("@parentId", model.ParentCollectionID);
            command.Parameters.AddWithValue("@dname", model.Name);
            command.Parameters.AddWithValue("@dpicture", imagePath ?? "");
            command.Parameters.AddWithValue("@dorder", nextOrder);
            command.Parameters.AddWithValue("@isValid", model.IsActive);
            command.Parameters.AddWithValue("@createdDate", createdDate);
            command.Parameters.AddWithValue("@userId", userId);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        // Collection Products
        public async Task<List<CollectionProduct>> GetAllCollectionProducts()
        {
            var products = new List<CollectionProduct>();
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT PID, PCode, PName, PInfoPreview, PContent, PIsValid, 
                     PCreatedDate, PCreatedUserID 
                     FROM CMSProduct 
                     WHERE PCode IS NOT NULL AND PCode != '' 
                     ORDER BY PCreatedDate DESC";
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new CollectionProduct
                {
                    PID = reader.GetInt32("PID"),
                    PCode = reader.IsDBNull("PCode") ? "" : reader.GetString("PCode"),
                    PName = reader.GetString("PName"),
                    PInfoPreview = reader.IsDBNull("PInfoPreview") ? "" : reader.GetString("PInfoPreview"),
                    PContent = reader.IsDBNull("PContent") ? "" : reader.GetString("PContent"),
                    PIsValid = reader.IsDBNull("PIsValid") ? true : reader.GetBoolean("PIsValid"),
                    PCreatedDate = reader.GetDateTime("PCreatedDate"),
                    PCreatedUserID = reader.IsDBNull("PCreatedUserID") ? null : reader.GetInt32("PCreatedUserID")
                });
            }
            return products;
        }

        public async Task<bool> CreateCollectionProduct(CollectionProductViewModel model, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Get collection group info - FIXED: Proper nullable tuple handling
                var groupInfoResult = await GetCollectionGroupInfo(model.CollectionGroupID, connection, transaction);
                if (!groupInfoResult.HasValue)
                {
                    transaction.Rollback();
                    return false;
                }

                var groupInfo = groupInfoResult.Value; // Extract the value from nullable tuple

                // Get next IDs
                var nextPID = await GetNextId(connection, "CMSProduct", "PID", transaction);
                var nextPDID = await GetNextId(connection, "CMSProductDepartment", "PDID", transaction);
                var nextPIID = await GetNextId(connection, "CMSProductImage", "PIID", transaction);

                var createdDate = DateTime.Now;
                var productName = $"{groupInfo.CollectionName} - {model.ProductCode}"; // FIXED: Proper access
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

                // Handle image uploads if provided
                if (model.SmallImage != null || model.MediumImage != null)
                {
                    var ftpService = new FTPService(_configuration); // FIXED: Now _configuration is available
                    string smallImagePath = null, mediumImagePath = null;

                    if (model.SmallImage != null)
                    {
                        smallImagePath = await ftpService.UploadFile(model.SmallImage, "/httpdocs/CMSFiles/ProductImages/SmallImage");
                    }

                    if (model.MediumImage != null)
                    {
                        mediumImagePath = await ftpService.UploadFile(model.MediumImage, "/httpdocs/CMSFiles/ProductImages/MediumImage");
                    }

                    // Insert into CMSProductImage
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
                    imageCommand.Parameters.AddWithValue("@description", $"{groupInfo.GroupName} - {model.ProductCode}"); // FIXED: Proper access
                    imageCommand.Parameters.AddWithValue("@isValid", model.IsActive);
                    imageCommand.Parameters.AddWithValue("@createdDate", createdDate);
                    imageCommand.Parameters.AddWithValue("@userId", userId);
                    await imageCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        private async Task<(string CollectionName, string GroupName)?> GetCollectionGroupInfo(int groupId, SqlConnection connection, SqlTransaction transaction)
        {
            var query = @"SELECT cg.DName as GroupName, c.BName as CollectionName 
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
            using var connection = new SqlConnection(_connectionString);
            var query = "UPDATE Users SET Password = @password WHERE Id = @userId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@password", newPassword); // In production, hash the password
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
    }
}
